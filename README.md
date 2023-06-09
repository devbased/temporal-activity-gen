# Activity Proxy Generator for Temporal .NET SDK

**NOTE: This repository has been archived. The decision to archive this repository is based on prioritizing the use of the official interceptors approach provided by the Temporal team.
If you are interested in exploring the official interceptors approach, you can refer to the Temporal samples repository: https://github.com/temporalio/samples-dotnet**

Source generator for the Temporal .NET SDK that generates proxy classes for interfaces containing activity methods.
The proxy classes enable dependency injection for your activities, making it easier to manage dependencies and services.


## Usage

### 1. Add the source generator to your project

Add the `TemporalActivityGen` package to your project for example using local nuget feed.

### 2. Annotate your interfaces and activity methods

To generate proxy classes, use the `GenerateActivityProxy` attribute on interfaces that contain activity methods, and the `Activity` attribute on the methods themselves.

```csharp
[GenerateActivityProxy]
public interface IOrderProcessingActivities
{
    public static readonly IOrderProcessingActivities Ref = ActivityRefs.Create<IOrderProcessingActivities>();

    [Activity("notify-orders-processed")]
    public Task NotifyOrderProcessed(ProcessOrderInput input);

    [Activity]
    public Task<ShipItemsOutput> ShipItems(ShipItemsInput input);

    public Task NonActivityMethod();
}
```

### 3. Register Activity Interface implementation in DI container
```csharp
services.AddScoped<IOrderProcessingActivities, OrderProcessingActivities>();
```

### 4. Register the generated proxy classes with the TemporalWorker

Use the `ActivityProxyHelper` class to discover and register the generated proxy classes with the TemporalWorker.

```csharp
IServiceProvider serviceProvider = ...
using var worker = new TemporalWorker(
    client,
    new TemporalWorkerOptions
    {
        Activities = ActivityProxyHelper.DiscoverAndRegisterActivityProxies(serviceProvider, typeof(OrderProcessingActivities).Assembly).ToList(),
    });
```

## What is generated

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the TemporalActivityGen source generator
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#pragma warning disable CS1591 // publicly visible type or member must be documented


#nullable enable

using TemporalActivityGen;
using Temporalio;
using Temporalio.Activities;
using Microsoft.Extensions.DependencyInjection;

[assembly: global::TemporalActivityGen.ActivityProxyRegistration(typeof(global::TestApplication.OrderProcessingActivitiesProxy))]

namespace TestApplication;

public partial class OrderProcessingActivitiesProxy : global::TestApplication.IOrderProcessingActivities
{
    private readonly global::System.IServiceProvider _serviceProvider;

    public OrderProcessingActivitiesProxy(global::System.IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Activity("notify-orders-processed")]
    public async global::System.Threading.Tasks.Task NotifyOrderProcessed(global::TestApplication.NotifyOrderProcessedInput input)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var impl = scope.ServiceProvider.GetRequiredService<global::TestApplication.IOrderProcessingActivities>();
        await impl.NotifyOrderProcessed(input);
    }
    
    [Activity]
    public async global::System.Threading.Tasks.Task<global::TestApplication.ShipItemsOutput> ShipItems(global::TestApplication.ShipItemsInput input)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var impl = scope.ServiceProvider.GetRequiredService<global::TestApplication.Activities.IShippingActivities>();
        return await impl.ShipItems(input);
    }
}
```
