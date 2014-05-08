namespace Papercut
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    using Autofac;

    using Caliburn.Micro;

    using Papercut.ViewModels;

    public class AppBootstrapper : Bootstrapper<MainViewModel>
    {
        readonly Lazy<ILifetimeScope> _lifetimeScope = new Lazy<ILifetimeScope>(() => ((App)Application.Current).Container);

        protected IComponentContext Container
        {
            get
            {
                return _lifetimeScope.Value;
            }
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return
                Container.Resolve(
                    typeof(IEnumerable<>)
                    .MakeGenericType(new Type[] { service })) as IEnumerable<object>;
        }

        protected override object GetInstance(Type service, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                object result;
                if (Container.TryResolve(service, out result)) return result;
            }
            else
            {
                object result;
                if (Container.TryResolveNamed(key, service, out result)) return result;
            }

            throw new Exception(string.Format("Could not locate any instances of contract {0}.", key ?? service.Name));
        }

        protected override void BuildUp(object instance)
        {
            Container.InjectProperties(instance);
        }
    }
}