using System;
using System.Threading.Tasks;
using TemporalActivityGen;
using Temporalio.Activities;

namespace TestApplication.Activities
{

    [GenerateActivityProxy]
    public interface IShippingActivities
    {
        [Activity("custom-activity-name")]
        public Task<ShipItemsOutput> ShipItems(ShipItemsInput input);
    }


}


namespace TestApplication
{
    public class NotifyOrderProcessedInput
    {
        public Guid OrderId { get; set; }
    }

    [GenerateActivityProxy]
    public interface IOrderProcessingActivities
    {
        [Activity]
        public Task NotifyOrderProcessed(NotifyOrderProcessedInput input);
    }


    public class ShipItemsInput
    {
        public Guid OrderId { get; set; }
    }

    public class ShipItemsOutput
    {
        public string TrackingNumber { get; set; }
    }
}