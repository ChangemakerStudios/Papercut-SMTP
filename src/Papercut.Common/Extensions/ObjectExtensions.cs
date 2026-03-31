// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.ComponentModel;
using System.Reflection;

namespace Papercut.Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Converts any object to type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    /// <returns></returns>
    public static T? ToType<T>(this object? instance)
    {
        if (instance == null) return default;

        if (Equals(instance, default(T))) return default;
        if (Equals(instance, DBNull.Value)) return default;

        if (instance is string str)
        {
            if (string.IsNullOrEmpty(str)) return default;
        }
        else if (instance is not IConvertible && !instance.GetType().IsValueType)
        {
            // just cast since it's a class....
            return (T) instance;
        }

        var conversionType = typeof (T);

        if (conversionType.IsGenericType
            && conversionType.GetGenericTypeDefinition() == typeof (Nullable<>))
            conversionType = new NullableConverter(conversionType).UnderlyingType;

        return (T) Convert.ChangeType(instance, conversionType);
    }

    public static void CopyTo<TFrom, TTo>(this TFrom obj, TTo other)
        where TFrom : class
        where TTo : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var otherProps = other.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance |
            BindingFlags.SetProperty);

        foreach (var props in obj.GetType().GetProperties(
                     BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance |
                     BindingFlags.GetProperty))
        {
            var matchingProp = otherProps.FirstOrDefault(s => s.Name.Equals(props.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingProp != null)
            {
                var value = props.GetValue(obj, null);
                matchingProp.SetValue(other, value, null);
            }
        }
    }

    public static bool IsAny<T>(this T value, params T[] items)
    {
        return items.Contains(value);
    }

    /// <summary>
    ///     Compares value to default(TIn) and return true if it's default.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsDefault<TIn>(this TIn value)
    {
        // from the master, J. Skeet:
        return EqualityComparer<TIn>.Default.Equals(value, default);
    }

    /// <summary>
    ///     Converts to an IEnumerable'T if obj is not default.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static IEnumerable<T> ToEnumerable<T>(this T obj)
    {
        if (!obj.IsDefault()) yield return obj;
    }

    /// <summary>
    /// Gets all properties on T obj as an IEnumerable of key/value pairs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<string, Lazy<object?>>> GetProperties<T>(this T obj)
        where T : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var type = typeof (T);

        var properties =
            type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance |
                               BindingFlags.GetProperty);

        foreach (var prop in properties)
        {
            var displayName =
                prop.GetCustomAttributes(typeof (DisplayNameAttribute), false)
                    .OfType<DisplayNameAttribute>()
                    .FirstOrDefault();

            var password =
                prop.GetCustomAttributes(typeof (PasswordPropertyTextAttribute), false)
                    .OfType<PasswordPropertyTextAttribute>()
                    .FirstOrDefault();

            if (password != null)
            {
                yield return KeyValuePair.Create(
                    displayName?.DisplayName ?? prop.Name,
                    new Lazy<object?>(() => "***********"));
            }
            else
            {
                yield return
                    KeyValuePair.Create(
                        displayName?.DisplayName ?? prop.Name,
                        new Lazy<object?>(() => prop.GetValue(obj, null)));
            }
        }
    }
}