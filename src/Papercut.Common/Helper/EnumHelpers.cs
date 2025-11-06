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


namespace Papercut.Common.Helper;

public static class EnumHelpers
{
    /// <summary>
    ///     Gets an enum as a list
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns></returns>
    public static List<TEnum> GetEnumList<TEnum>()
        where TEnum : struct
    {
        Type enumType = typeof(TEnum);

        // Can't use type constraints on value types, so have to do check like this
        if (enumType.BaseType != typeof(Enum))
            throw new ArgumentException("EnumAsList does not support non-enum types");

        Array enumValArray = Enum.GetValues(enumType);

        var enumValues = new List<TEnum>(enumValArray.Length);

        enumValues.AddRange(
            enumValArray.Cast<int>().Select(val => (TEnum)Enum.Parse(enumType, val.ToString())));

        return enumValues;
    }

    /// <summary>
    /// Enum to names
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns></returns>
    public static List<string> GetNames<TEnum>()
        where TEnum : struct
    {
        Type enumType = typeof(TEnum);

        // Can't use type constraints on value types, so have to do check like this
        if (enumType.BaseType != typeof(Enum)) throw new ArgumentException("EnumToNames does not support non-enum types");

        return Enum.GetNames(enumType).ToList();
    }
}