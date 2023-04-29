using System;

namespace TemporalActivityGen;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class ActivityProxyRegistrationAttribute : Attribute
{
    public ActivityProxyRegistrationAttribute(Type activityProxyType)
    {
        this.ActivityProxyType = activityProxyType;
    }

    public Type ActivityProxyType { get; }
}
