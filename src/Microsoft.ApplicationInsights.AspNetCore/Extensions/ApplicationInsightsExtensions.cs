﻿namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
#if NETSTANDARD2_0
    using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Shared.Implementation;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static partial class ApplicationInsightsExtensions
    {
        [SuppressMessage(category: "", checkId: "CS1591:MissingXmlComment", Justification = "Obsolete method.")]
        [Obsolete("This middleware is no longer needed. Enable Request monitoring using services.AddApplicationInsights")]
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            return app;
        }

        [SuppressMessage(category: "", checkId: "CS1591:MissingXmlComment", Justification = "Obsolete method.")]
        [Obsolete("This middleware is no longer needed to track exceptions as they are automatically tracked by RequestTrackingTelemetryModule")]
        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionTrackingMiddleware>();
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="instrumentationKey">Instrumentation key to use for telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            string instrumentationKey)
        {
            services.AddApplicationInsightsTelemetry(options => options.InstrumentationKey = instrumentationKey);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">Configuration to use for sending telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The action used to configure the options.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure(options);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The options instance used to configure with.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            ApplicationInsightsServiceOptions options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure((ApplicationInsightsServiceOptions o) => options.CopyPropertiesTo(o));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services)
        {
            try
            {
                if (!IsApplicationInsightsAdded(services))
                {
                    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    AddAspNetCoreWebTelemetryInitializers(services);
                    AddCommonInitializers(services);

                    // Request Tracking.
                    services.AddSingleton<ITelemetryModule>(provider =>
                    {
                        var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>().Value;
                        var appIdProvider = provider.GetService<IApplicationIdProvider>();

                        if (options.EnableRequestTrackingTelemetryModule)
                        {
                            return new RequestTrackingTelemetryModule(appIdProvider);
                        }
                        else
                        {
                            return new NoOpTelemetryModule();
                        }
                    });

                    services.ConfigureTelemetryModule<RequestTrackingTelemetryModule>((module, options) =>
                    {
                        if(options.EnableRequestTrackingTelemetryModule)
                        {
                            module.CollectionOptions = options.RequestCollectionOptions;
                        }                        
                    });

                    AddCommonTelemetryModules(services);
                    AddTelemetryChannel(services);

#if NETSTANDARD2_0
                    ConfigureEventCounterModuleWithSystemCounters(services);
                    ConfigureEventCounterModuleWithAspNetCounters(services);
#endif

                    services.TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>,
                            DefaultApplicationInsightsServiceConfigureOptions>();

                    AddTelemetryConfigAndClient(services);
                    AddDefaultApplicationIdProvider(services);

                    // Using startup filter instead of starting DiagnosticListeners directly because
                    // AspNetCoreHostingDiagnosticListener injects TelemetryClient that injects TelemetryConfiguration
                    // that requires IOptions infrastructure to run and initialize
                    services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();
                    services.AddSingleton<IJavaScriptSnippet, JavaScriptSnippet>();
                    // Add 'JavaScriptSnippet' "Service" for backwards compatibility. To remove in favour of 'IJavaScriptSnippet'.
                    services.AddSingleton<JavaScriptSnippet>(); 

                    // NetStandard2.0 has a package reference to Microsoft.Extensions.Logging.ApplicationInsights, and
                    // enables ApplicationInsightsLoggerProvider by default.
#if NETSTANDARD2_0
                    AddApplicationInsightsLoggerProvider(services);
#endif
                }

                return services;
            }
            catch (Exception e)
            {
                AspNetCoreEventSource.Instance.LogError(e.ToInvariantString());
                return services;
            }
        }

        private static void AddAspNetCoreWebTelemetryInitializers(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, SyntheticTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, AspNetCoreEnvironmentTelemetryInitializer>();
        }
    }
}
