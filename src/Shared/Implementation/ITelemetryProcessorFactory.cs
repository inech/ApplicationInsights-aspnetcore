﻿#if AI_ASPNETCORE_WEB
    namespace Microsoft.ApplicationInsights.AspNetCore
#else
namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Represents factory used to create <see cref="ITelemetryProcessor"/> with dependency injection support.
    /// </summary>
    public interface ITelemetryProcessorFactory
    {
        /// <summary>
        /// Returns a <see cref="ITelemetryProcessor"/>,
        /// given the next <see cref="ITelemetryProcessor"/> in the call chain.
        /// </summary>
        ITelemetryProcessor Create(ITelemetryProcessor nextProcessor);
    }
}