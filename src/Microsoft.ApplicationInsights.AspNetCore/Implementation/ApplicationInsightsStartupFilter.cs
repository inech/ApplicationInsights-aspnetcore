﻿namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// <see cref="IStartupFilter"/> implementation that initialized ApplicationInsights services on application startup
    /// </summary>
    internal class ApplicationInsightsStartupFilter : IStartupFilter
    {
        private readonly ILogger<ApplicationInsightsStartupFilter> logger;
        
        public ApplicationInsightsStartupFilter(ILogger<ApplicationInsightsStartupFilter> logger)
        {
            this.logger = logger;
        }
        
        /// <inheritdoc/>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                try
                {
                    // Attempting to resolve TelemetryConfiguration triggers configuration of the same
                    // via <see cref="TelemetryConfigurationOptionsSetup"/> class which triggers
                    // initialization of TelemetryModules and construction of TelemetryProcessor pipeline.
                    var tc = app.ApplicationServices.GetService<TelemetryConfiguration>();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(0, ex, "Failed to resolve TelemetryConfiguration.");
                    AspNetCoreEventSource.Instance.LogError(ex.ToInvariantString());
                }

                // Invoking next builder is not wrapped in try catch to ensure any exceptions gets propogated up.
                next(app);
            };
        }
    }
}
