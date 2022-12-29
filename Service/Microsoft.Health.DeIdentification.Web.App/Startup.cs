// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Local;
using Microsoft.Health.DeIdentification.Web.Async;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.JobManagement;

namespace Microsoft.Health.DeIdentification.Web.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DeIdConfigurationSection>(options => Configuration.GetSection(ConfigurationConstants.DeIdConfigurationSectionKey).Bind(options));

            services.AddControllersWithViews().AddNewtonsoftJson();
            //services.AddScoped<InMemoryQueueClient>();

            // add artifact store
            services.AddSingleton<IArtifactStore, LocalArtifactStore>();

            services.AddSingleton<JobHosting, JobHosting>();

            services.AddSingleton<IJobFactory, LocalJobFactory>();

            services.AddHostedService<HostingBackgroundService>();

            services.AddSingleton<IDeIdConfigurationRegistration, DeIdConfigurationRegistration>();

            services.AddSingleton<FhirDeIdHandler, FhirDeIdHandler>();

            services.AddSingleton<LocalFhirBatchHandler, LocalFhirBatchHandler>();

            services.AddSingleton<LocalFhirDataLoader, LocalFhirDataLoader>();

            services.AddSingleton<LocalFhirDataWriter, LocalFhirDataWriter>();

            services.AddSingleton<IQueueClient, InMemoryQueueClient>();

            AnonymizerEngine.InitializeFhirPathExtensionSymbols();

            services.AddFactory<IScoped<IDeIdOperation<ResourceList, ResourceList>>>();

            services.Add<FhirDeIdOperationProvider>()
                    .Transient()
                    .AsService<IDeIdOperationProvider>();
            //services.AddSingleton<IDeIdOperationProvider, FhirDeIdOperationProvider>();

        }
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
