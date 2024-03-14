using Autofac.Builder;

namespace Papercut.Core.Infrastructure.Container;

public static class AutofacRegistrationExtensions
{
    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
        InstancePerUIScope
        <TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder)
    {
        return builder.InstancePerMatchingLifetimeScope(ContainerScope.UIScopeTag);
    }
}