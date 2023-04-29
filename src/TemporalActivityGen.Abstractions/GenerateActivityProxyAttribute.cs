using System;

namespace TemporalActivityGen;

/// <summary>
/// Generate a proxy for the interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
[System.Diagnostics.Conditional("TEMPORAL_ACTIVITY_GEN_USAGES")]
public sealed class GenerateActivityProxyAttribute : Attribute
{
}
