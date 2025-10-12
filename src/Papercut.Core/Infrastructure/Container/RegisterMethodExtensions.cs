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


using System.Reflection;

using Autofac;

using Papercut.Common.Extensions;

namespace Papercut.Core.Infrastructure.Container;

public static class RegisterMethodExtensions
{
    static IsRegisterContainerBuilderMethodSpecification MethodIsRegisterContainerBuilderSpecification =>
        new();

    /// <summary>
    ///     Gets the register methods.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns></returns>
    public static IReadOnlyList<Action<ContainerBuilder>> GetStaticRegisterMethods(this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        var instanceBasedRegisterMethods = assembly.GetTypes()
            .SelectMany(_ => _.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => MethodIsRegisterContainerBuilderSpecification.IsSatisfiedBy(m))
            .ToList();

        if (instanceBasedRegisterMethods.Any())
            throw new AggregateException(
                instanceBasedRegisterMethods.Select(
                    s =>
                        new MissingMethodException($"Registration Method: {s.DeclaringType}.{s} must be static!")));

        var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        return
            assembly.GetTypes()
                .SelectMany(_ => _.GetMethods(bindingFlags))
                .Where(m => MethodIsRegisterContainerBuilderSpecification.IsSatisfiedBy(m))
                .Select(
                    z => new Action<ContainerBuilder>(
                        c =>
                        {
                            Log.Verbose(
                                "Invoking Registration Method {MethodType} {MethodName}",
                                z.DeclaringType?.ToString(),
                                z.ToString());
                            z.Invoke(null, new object[] {c});
                        }))
                .ToList();
    }

    /// <summary>
    ///     Invokes all register methods
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="assembly"></param>
    /// <param name="additionalAssemblies"></param>
    public static void RegisterStaticMethods(
        this ContainerBuilder builder,
        Assembly assembly,
        params Assembly[] additionalAssemblies)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        foreach (var registerMethod in assembly.ToEnumerable().Concat(additionalAssemblies)
                     .SelectMany(s => s.GetStaticRegisterMethods())) registerMethod(builder);
    }

    internal class IsRegisterContainerBuilderMethodSpecification
    {
        public bool IsSatisfiedBy(MethodInfo method)
        {
            return !this.IsNotSatisfiedBecause(method).Any();
        }

        private IEnumerable<string> IsNotSatisfiedBecause(MethodInfo method)
        {
            if (method?.Name != "Register")
            {
                yield return "Method NewName must be 'Register'";
                yield break;
            }

            var @params = method.GetParameters().IfNullEmpty().ToList();

            if (@params.Count != 1 || @params[0].ParameterType != typeof(ContainerBuilder))
                yield return "Method must have one parameter of type 'ContainerBuilder'";
        }
    }
}