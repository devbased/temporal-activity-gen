# Activity Proxy Generator for Temporal .NET SDK

**NOTE: This project is just a prototype and is not supported by the Temporal team.**

Source generator for the Temporal .NET SDK that generates proxy classes for interfaces containing activity methods.
The proxy classes enable dependency injection for your activities, making it easier to manage dependencies and services.


## Usage

### 1. Add the source generator to your project

Add the `Temporal.ActivityProxyGenerator` package to your project for example using local nuget feed.

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

### 3. Register the generated proxy classes with the TemporalWorker

Use the `ActivityProxyHelper` class to discover and register the generated proxy classes with the TemporalWorker.

```csharp
IServiceProvider serviceProvider = ...
using var worker = new TemporalWorker(
    client,
    new TemporalWorkerOptions
    {
        Activities = ActivityProxyHelper.DiscoverAndRegisterActivityProxies(serviceProvider).ToList(),
    });
```