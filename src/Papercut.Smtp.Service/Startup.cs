namespace Papercut.Smtp.Service
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;

    using Core.Annotations;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Services;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public ILifetimeScope AutofacContainer { get; private set; }

        public IConfiguration Configuration { get; }

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<ServiceModule>();

            builder.Register(c =>
                {
                    var papercutService = c.Resolve<PapercutServerService>();

                    papercutService.Start();

                    return papercutService;
                })
                .Named<PapercutServerService>("AutoRunMainInstance").SingleInstance()
                .AutoActivate();
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapControllerRoute("AppStatic", "{*url}",
                    defaults: new {controller = "StaticContent", action = "Get"});
            });
        }
    }
}
