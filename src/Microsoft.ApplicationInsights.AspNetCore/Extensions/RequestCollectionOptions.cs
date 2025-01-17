﻿using System;

namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
{
    /// <summary>
    /// Request collection options define the custom behavior or non-default features of request collection.
    /// </summary>
    public class RequestCollectionOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCollectionOptions"/> class
        /// and populates default values.
        /// </summary>
        public RequestCollectionOptions()
        {
            this.InjectResponseHeaders = true;

            // In NetStandard20, ApplicationInsightsLoggerProvider is enabled by default,
            // which captures Exceptions. Disabling it in RequestCollectionModule to avoid duplication.
#if NETSTANDARD2_0
            this.TrackExceptions = false;
#else
            this.TrackExceptions = true;
#endif
            this.EnableW3CDistributedTracing = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether Request-Context header is injected into the response.
        /// </summary>
        public bool InjectResponseHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exceptions are be tracked by the RequestCOllectionModule.
        /// Exceptions could be tracked by ApplicationInsightsLoggerProvider as well which is not affected by
        /// this setting.
        /// </summary>
        public bool TrackExceptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether W3C distributed tracing standard is enabled.
        /// </summary>
        [Obsolete("This flag is obsolete and noop. Use System.Diagnostics.Activity.DefaultIdFormat (along with ForceDefaultIdFormat) flags instead.")] 
        public bool EnableW3CDistributedTracing { get; set; }
    }
}
