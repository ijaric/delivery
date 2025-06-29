using CSharpFunctionalExtensions;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using Primitives;

namespace DeliveryApp.Core.Domain.Services.DispatchService;

public class DispatchService : IDispatchService
{
    public Result<Courier, Error> Dispatch(Order order, Courier[] couriers)
    {
        // Validate input parameters
        if (order == null) return GeneralErrors.ValueIsRequired("Order");
        if (couriers == null) return GeneralErrors.ValueIsRequired("Couriers");
        if (couriers.Length <= 0) return GeneralErrors.ValueIsRequired("Provide one or more available couriers");
        if (order.Status != OrderStatus.Created) return GeneralErrors.ValueIsInvalid("Provide Order in Created status");

        Courier bestCourier = null;
        double bestTime = double.PositiveInfinity;

        // Find the courier that can take the order with the shortest delivery time
        foreach (var courier in couriers)
        {
            var canTakeOrderResult = courier.CanTakeOrder(order);
            if (canTakeOrderResult.IsFailure) continue; // Skip courier if it can't take the order
            
            if (!canTakeOrderResult.Value) continue; // Skip if courier can't take the order
            
            var timeResult = courier.CalculateTimeToLocation(order.Location);
            if (timeResult.IsFailure) continue; // Skip courier if time calculation fails
            
            if (timeResult.Value < bestTime)
            {
                bestCourier = courier;
                bestTime = timeResult.Value;
            }
        }

        // Check if we found any eligible courier
        if (bestCourier == null) 
            return GeneralErrors.ValueIsInvalid("No courier available with sufficient space");

        // Assign the order to the best courier
        var takeOrderResult = bestCourier.TakeOrder(order);
        if (takeOrderResult.IsFailure) return takeOrderResult.Error;

        return bestCourier;
    }
}