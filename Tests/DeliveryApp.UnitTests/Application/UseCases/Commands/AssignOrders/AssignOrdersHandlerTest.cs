using DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using DeliveryApp.Core.Domain.Services.DispatchService;
using DeliveryApp.Core.Ports;
using FluentAssertions;
using NSubstitute;
using Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using CSharpFunctionalExtensions;
using System.Collections.Generic;
using System.Linq;

namespace DeliveryApp.UnitTests.Application.UseCases.Commands.AssignOrders;

public class AssignOrdersHandlerTest
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICourierRepository _courierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDispatchService _dispatchService;
    private readonly AssignOrdersHandler _handler;

    public AssignOrdersHandlerTest()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _courierRepository = Substitute.For<ICourierRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _dispatchService = Substitute.For<IDispatchService>();
        _handler = new AssignOrdersHandler(_orderRepository, _courierRepository, _unitOfWork, _dispatchService);
    }

    [Fact]
    public async Task Handle_WithNoUnassignedOrders_ReturnsTrue()
    {
        // Arrange
        var orders = new Order[0];
        var couriers = new Courier[0];

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _orderRepository.DidNotReceive().UpdateOrder(Arg.Any<Order>());
        _courierRepository.DidNotReceive().UpdateCourier(Arg.Any<Courier>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoAvailableCouriers_ReturnsTrue()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var orders = new[] { order };
        var couriers = new Courier[0];

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Created); // Order should remain unassigned
        _orderRepository.DidNotReceive().UpdateOrder(Arg.Any<Order>());
        _courierRepository.DidNotReceive().UpdateCourier(Arg.Any<Courier>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDispatchServiceFailure_SkipsOrderAndContinues()
    {
        // Arrange
        var order1 = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var order2 = Order.Create(Guid.NewGuid(), new Location(2, 2), 3).Value;
        var courier = Courier.Create("TestCourier", 5, new Location(3, 3)).Value;
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // First order fails dispatch, second succeeds
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>())
            .Returns(callInfo =>
            {
                var order = callInfo.ArgAt<Order>(0);
                if (order.Id == order1.Id)
                    return Result.Failure<Courier, Error>(GeneralErrors.ValueIsInvalid("No suitable courier"));
                else
                    return Result.Success<Courier, Error>(courier);
            });
            
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // First order should remain unassigned (dispatch failed)
        order1.Status.Should().Be(OrderStatus.Created);
        order1.CourierId.Should().BeNull();
        _orderRepository.DidNotReceive().UpdateOrder(order1);
        
        // Second order should be assigned (dispatch succeeded)
        order2.Status.Should().Be(OrderStatus.Assigned);
        order2.CourierId.Should().Be(courier.Id);
        _orderRepository.Received(1).UpdateOrder(order2);
        _courierRepository.Received(1).UpdateCourier(courier);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOrderAssignFailure_SkipsOrderAndContinues()
    {
        // Arrange
        var order1 = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var order2 = Order.Create(Guid.NewGuid(), new Location(2, 2), 3).Value;
        var courier1 = Courier.Create("Courier1", 5, new Location(1, 1)).Value;
        var courier2 = Courier.Create("Courier2", 5, new Location(2, 2)).Value;
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier1, courier2 };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // Pre-assign order1 to another courier to simulate assignment failure
        var tempCourier = Courier.Create("TempCourier", 3, new Location(5, 5)).Value;
        order1.Assign(tempCourier); // This will make the next assign call fail
        
        // Both dispatches succeed
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>())
            .Returns(callInfo =>
            {
                var order = callInfo.ArgAt<Order>(0);
                if (order.Id == order1.Id)
                    return Result.Success<Courier, Error>(courier1);
                else
                    return Result.Success<Courier, Error>(courier2);
            });
            
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // First order assignment failed, so no repository update for courier1
        _orderRepository.DidNotReceive().UpdateOrder(order1);
        _courierRepository.DidNotReceive().UpdateCourier(courier1);
        
        // Second order assignment succeeded
        order2.Status.Should().Be(OrderStatus.Assigned);
        order2.CourierId.Should().Be(courier2.Id);
        _orderRepository.Received(1).UpdateOrder(order2);
        _courierRepository.Received(1).UpdateCourier(courier2);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuccessfulAssignment_UpdatesRepositoriesAndCallsUnitOfWork()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var courier = Courier.Create("TestCourier", 5, new Location(2, 2)).Value;
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>()).Returns(Result.Success<Courier, Error>(courier));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Assigned);
        order.CourierId.Should().Be(courier.Id);
        _orderRepository.Received(1).UpdateOrder(order);
        _courierRepository.Received(1).UpdateCourier(courier);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnitOfWorkFailure_ReturnsFalse()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var courier = Courier.Create("TestCourier", 5, new Location(2, 2)).Value;
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>()).Returns(Result.Success<Courier, Error>(courier));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Assigned);
        order.CourierId.Should().Be(courier.Id);
        _orderRepository.Received(1).UpdateOrder(order);
        _courierRepository.Received(1).UpdateCourier(courier);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_ProcessesEachOrderSequentially()
    {
        // Arrange
        var order1 = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var order2 = Order.Create(Guid.NewGuid(), new Location(2, 2), 3).Value;
        var courier1 = Courier.Create("Courier1", 5, new Location(1, 1)).Value;
        var courier2 = Courier.Create("Courier2", 5, new Location(2, 2)).Value;
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier1, courier2 };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // Track dispatch calls for debugging
        var dispatchCalls = new List<string>();
        
        // Setup dispatch service - always return success with first available courier
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>())
            .Returns(callInfo =>
            {
                var order = callInfo.ArgAt<Order>(0);
                var availableCouriers = callInfo.ArgAt<Courier[]>(1);
                var courierNames = string.Join(", ", availableCouriers.Select(c => c.Name));
                dispatchCalls.Add($"Order {order.Id} with {availableCouriers.Length} couriers: [{courierNames}]");
                
                // Always return success with first available courier
                return Result.Success<Courier, Error>(availableCouriers[0]);
            });
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // Debug: Check dispatch calls
        dispatchCalls.Count.Should().Be(2, $"Should have called dispatch for both orders. Actual calls: {string.Join("; ", dispatchCalls)}");
        
        // Both orders should be processed
        order1.Status.Should().Be(OrderStatus.Assigned);
        order1.CourierId.Should().NotBeNull();
        order2.Status.Should().Be(OrderStatus.Assigned);
        order2.CourierId.Should().NotBeNull();
        
        // Repository calls should be made for both orders
        _orderRepository.Received(1).UpdateOrder(order1);
        _orderRepository.Received(1).UpdateOrder(order2);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsRepositoryMethodsWithCorrectParameters()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var courier = Courier.Create("TestCourier", 5, new Location(2, 2)).Value;
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>()).Returns(Result.Success<Courier, Error>(courier));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // Verify correct repository method calls
        await _orderRepository.Received(1).GetByStatuses(OrderStatus.Created);
        await _courierRepository.Received(1).GetAvailable();
        _dispatchService.Received().Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>());
        
        // Verify domain object state
        order.Status.Should().Be(OrderStatus.Assigned);
        order.CourierId.Should().Be(courier.Id);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToUnitOfWork()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), new Location(1, 1), 5).Value;
        var courier = Courier.Create("TestCourier", 5, new Location(2, 2)).Value;
        var orders = new[] { order };
        var couriers = new[] { courier };
        var cancellationToken = new CancellationToken();

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(Arg.Any<Order>(), Arg.Any<Courier[]>()).Returns(Result.Success<Courier, Error>(courier));
        _unitOfWork.SaveChangesAsync(cancellationToken).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Assigned);
        order.CourierId.Should().Be(courier.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}