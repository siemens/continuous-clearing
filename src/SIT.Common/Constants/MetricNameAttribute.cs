// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System;

namespace SIT.Common.Constants
{
    /// <summary>
    /// Declares the Grafana-friendly, low-cardinality metric name to be emitted for a KPI property
    /// (e.g. "sit.upload.packages_uploaded"). Used in place of the human-readable DisplayName when
    /// splitting KPI data into individual numeric metrics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MetricNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the metric name to use when tracking this property as a telemetry metric.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The metric name to use when tracking this property as a telemetry metric.</param>
        public MetricNameAttribute(string name)
        {
            Name = name;
        }
    }
}
