namespace Papercut.Core.Helper
{
    using Autofac.Builder;

    public static class AutofacRegistrationExtensions
    {
        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> InstancePerUIScope
            <TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder)
        {
            return builder.InstancePerMatchingLifetimeScope(PapercutContainer.UIScopeTag);
        }
    }
}