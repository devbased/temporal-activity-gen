using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TemporalActivityGen;

public class ActivityProxyHelper
{
    public static IEnumerable<Delegate> DiscoverAndRegisterActivityProxies(IServiceProvider serviceProvider, params Assembly[] assembliesToLookup)
    {
        if (assembliesToLookup.Length == 0)
        {
            yield break;
        }

        foreach (var assembly in assembliesToLookup)
        {
            var activityProxyRegistrationAttributes = assembly.GetCustomAttributes<ActivityProxyRegistrationAttribute>();
            foreach (var activityProxyRegistrationAttribute in activityProxyRegistrationAttributes)
            {
                var activityProxyType = activityProxyRegistrationAttribute.ActivityProxyType;
                if (activityProxyType == null)
                {
                    throw new InvalidOperationException("ActivityProxyType is null.");
                }

                var activityProxy = Activator.CreateInstance(activityProxyType, serviceProvider);
                if (activityProxy == null)
                {
                    throw new InvalidOperationException("Cannot create ActivityProxy instance.");
                }

                foreach (var method in activityProxyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsFinal && m.IsVirtual))
                {
                    var dynamicDelegate = method.CreateDelegate(Expression.GetDelegateType(method.GetParameters().Select(x => x.ParameterType).Append(method.ReturnType).ToArray()), activityProxy);

                    yield return dynamicDelegate;
                }
            }
        }
    }
}
