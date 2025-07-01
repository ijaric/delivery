using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using Xunit;
using System;

namespace DeliveryApp.Core.Domain.Services.DispatchService;

public class DispatchServiceTest
{
    private readonly DispatchService _dispatchService;

    public DispatchServiceTest()
    {
        _dispatchService = new DispatchService();
    }

    [Fact]
    public void Dispatch_WithEmptyArrayOfCouriers_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();
        var couriers = new Courier[0];

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Provide one or more available couriers", result.Error.Message);
    }

    [Fact]
    public void Dispatch_WithNullOrder_ReturnsError()
    {
        // Arrange
        Order order = null;
        var couriers = new[] { CreateValidCourier("Courier1", 5, new Location(1, 1)) };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Dispatch_WithNullCouriersArray_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();
        Courier[] couriers = null;

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Dispatch_WithOrderNotInCreatedStatus_ReturnsError()
    {
        // Arrange
        var order = CreateValidOrder();
        var courier = CreateValidCourier("Courier1", 5, new Location(1, 1));
        
        // First assign the order to change its status
        order.Assign(courier);
        var couriers = new[] { courier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Provide Order in Created status", result.Error.Message);
    }

    [Fact]
    public void Dispatch_WithCouriersWithoutSufficientSpace_ReturnsError()
    {
        // Arrange
        var order = CreateOrderWithVolume(15); // Large volume
        var courier = CreateValidCourier("SmallCourier", 5, new Location(1, 1)); // Default storage is 10
        var couriers = new[] { courier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("No courier available with sufficient space", result.Error.Message);
    }

    [Fact]
    public void Dispatch_WithSingleValidCourier_ReturnsSuccessfully()
    {
        // Arrange
        var order = CreateValidOrder();
        var courier = CreateValidCourier("Courier1", 5, new Location(1, 1));
        var couriers = new[] { courier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(courier, result.Value);
        Assert.Equal(OrderStatus.Assigned, order.Status);
        Assert.Equal(courier.Id, order.CourierId);
    }

    [Fact]
    public void Dispatch_WithMultipleCouriersWithDifferentTimes_SelectsFastest()
    {
        // Arrange
        var orderLocation = new Location(5, 5);
        var order = CreateOrderAtLocation(orderLocation);
        
        var slowCourier = CreateValidCourier("SlowCourier", 1, new Location(1, 1)); // Distance: 8, Time: 8
        var fastCourier = CreateValidCourier("FastCourier", 10, new Location(3, 3)); // Distance: 4, Time: 0.4
        var mediumCourier = CreateValidCourier("MediumCourier", 2, new Location(2, 2)); // Distance: 6, Time: 3
        
        var couriers = new[] { slowCourier, fastCourier, mediumCourier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fastCourier, result.Value);
    }

    [Fact]
    public void Dispatch_WithCouriersWithEqualTime_SelectsFirstEligible()
    {
        // Arrange
        var orderLocation = new Location(3, 3);
        var order = CreateOrderAtLocation(orderLocation);
        
        var courier1 = CreateValidCourier("Courier1", 2, new Location(1, 1)); // Distance: 4, Time: 2
        var courier2 = CreateValidCourier("Courier2", 2, new Location(5, 5)); // Distance: 4, Time: 2
        
        var couriers = new[] { courier1, courier2 };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(courier1, result.Value); // First one should be selected
    }

    [Fact]
    public void Dispatch_WithMixedCouriers_SelectsOptimal()
    {
        // Arrange
        var orderLocation = new Location(5, 5);
        var order = CreateOrderWithVolumeAtLocation(15, orderLocation); // Large volume
        
        var courierNoSpace = CreateValidCourier("NoSpace", 10, new Location(1, 1)); // Fast but no space (default 10)
        var courierWithSpace = CreateValidCourier("WithSpace", 5, new Location(2, 2)); // Slower but has space
        courierWithSpace.AddStoragePlace("Big Bag", 20); // Add larger storage
        
        var couriers = new[] { courierNoSpace, courierWithSpace };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(courierWithSpace, result.Value);
    }

    [Fact]
    public void Dispatch_WithOnlyOneEligibleCourier_SelectsThatCourier()
    {
        // Arrange
        var order = CreateValidOrder();
        
        var ineligibleCourier = CreateValidCourier("Ineligible", 5, new Location(1, 1));
        var eligibleCourier = CreateValidCourier("Eligible", 3, new Location(2, 2));
        eligibleCourier.AddStoragePlace("Extra Storage", 10);
        
        // Make first courier ineligible by filling its storage
        var dummyOrder = CreateValidOrder();
        ineligibleCourier.TakeOrder(dummyOrder);
        
        var couriers = new[] { ineligibleCourier, eligibleCourier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(eligibleCourier, result.Value);
    }

    [Fact]
    public void Dispatch_VerifiesOrderStateAfterDispatch()
    {
        // Arrange
        var order = CreateValidOrder();
        var courier = CreateValidCourier("Courier1", 5, new Location(1, 1));
        var couriers = new[] { courier };

        // Act
        var result = _dispatchService.Dispatch(order, couriers);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Assigned, order.Status);
        Assert.Equal(courier.Id, order.CourierId);
        
        // Verify courier has taken the order (storage should be occupied)
        var canTakeAnotherOrder = courier.CanTakeOrder(CreateValidOrder());
        Assert.True(canTakeAnotherOrder.IsFailure); // Should fail because storage is occupied
    }

    // Helper methods
    private Order CreateValidOrder()
    {
        return Order.Create(Guid.NewGuid(), new Location(5, 5), 5).Value;
    }

    private Order CreateOrderWithVolume(int volume)
    {
        return Order.Create(Guid.NewGuid(), new Location(5, 5), volume).Value;
    }

    private Order CreateOrderAtLocation(Location location)
    {
        return Order.Create(Guid.NewGuid(), location, 5).Value;
    }

    private Order CreateOrderWithVolumeAtLocation(int volume, Location location)
    {
        return Order.Create(Guid.NewGuid(), location, volume).Value;
    }

    private Courier CreateValidCourier(string name, int speed, Location location)
    {
        return Courier.Create(name, speed, location).Value;
    }
}