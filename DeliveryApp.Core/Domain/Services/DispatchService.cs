using CSharpFunctionalExtensions;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using Primitives;

namespace DeliveryApp.Core.Domain.Services.DispatchService;

public class DispatchService : IDispatchService
{
    public Result<Courier, Error> Dispatch(Order order, Courier[] couriers)
    {
        // Validate input
        if (order.Status != OrderStatus.Created) return GeneralErrors.ValueIsInvalid("Provide Order in Created status.");
        if (couriers.Length <= 0) return GeneralErrors.ValueIsRequired("Provide one or more available couriers.");

        // Find the best courier (if any)
        Courier bestCourier = couriers.FirstOrDefault(c => c.CanTakeOrder(order).Value is true);
        if (bestCourier == null) return GeneralErrors.ValueIsInvalid("No courier available with sufficient space.");

        double bestTime = double.PositiveInfinity;
        foreach (var courier in couriers)
        {

            if (courier.CanTakeOrder(order).Value is true && courier.CalculateTimeToLocation(order.Location).Value < bestTime)
            {
                bestCourier = courier;
                bestTime = courier.CalculateTimeToLocation(order.Location).Value;
            }
        }
        
        // Assign an order to the best courier           
        var takeOrderResult = bestCourier.TakeOrder(order);
        if (takeOrderResult.IsFailure) return takeOrderResult.Error;

        return bestCourier;
    }
}