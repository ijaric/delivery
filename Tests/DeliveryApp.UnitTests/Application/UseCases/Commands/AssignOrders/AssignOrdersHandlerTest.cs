using DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
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
        var order = Substitute.For<Order>();
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
        _orderRepository.DidNotReceive().UpdateOrder(Arg.Any<Order>());
        _courierRepository.DidNotReceive().UpdateCourier(Arg.Any<Courier>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDispatchServiceFailure_SkipsOrderAndContinues()
    {
        // Arrange
        var order1 = Substitute.For<Order>();
        var order2 = Substitute.For<Order>();
        var courier = Substitute.For<Courier>();
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // First order fails dispatch, second succeeds
        _dispatchService.Dispatch(order1, Arg.Any<Courier[]>())
            .Returns(Result.Failure<Courier, Error>(GeneralErrors.ValueIsInvalid("No suitable courier")));
        _dispatchService.Dispatch(order2, Arg.Any<Courier[]>())
            .Returns(Result.Success<Courier, Error>(courier));
            
        order2.Assign(courier).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // First order should not be updated (dispatch failed)
        _orderRepository.DidNotReceive().UpdateOrder(order1);
        
        // Second order should be updated (dispatch succeeded)
        order2.Received(1).Assign(courier);
        _orderRepository.Received(1).UpdateOrder(order2);
        _courierRepository.Received(1).UpdateCourier(courier);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOrderAssignFailure_SkipsOrderAndContinues()
    {
        // Arrange
        var order1 = Substitute.For<Order>();
        var order2 = Substitute.For<Order>();
        var courier1 = Substitute.For<Courier>();
        var courier2 = Substitute.For<Courier>();
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier1, courier2 };

        courier1.Id.Returns(Guid.NewGuid());
        courier2.Id.Returns(Guid.NewGuid());

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // Both dispatches succeed but first assignment fails
        _dispatchService.Dispatch(order1, couriers).Returns(Result.Success<Courier, Error>(courier1));
        _dispatchService.Dispatch(order2, Arg.Is<Courier[]>(c => c.Length == 2)) // All couriers available since first assignment failed
            .Returns(Result.Success<Courier, Error>(courier2));
            
        order1.Assign(courier1).Returns(UnitResult.Failure<Error>(GeneralErrors.ValueIsInvalid("Order assignment failed")));
        order2.Assign(courier2).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // First order assignment failed, so no repository update
        order1.Received(1).Assign(courier1);
        _orderRepository.DidNotReceive().UpdateOrder(order1);
        _courierRepository.DidNotReceive().UpdateCourier(courier1);
        
        // Second order assignment succeeded
        order2.Received(1).Assign(courier2);
        _orderRepository.Received(1).UpdateOrder(order2);
        _courierRepository.Received(1).UpdateCourier(courier2);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuccessfulAssignment_UpdatesRepositoriesAndCallsUnitOfWork()
    {
        // Arrange
        var order = Substitute.For<Order>();
        var courier = Substitute.For<Courier>();
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(order, couriers).Returns(Result.Success<Courier, Error>(courier));
        order.Assign(courier).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        order.Received(1).Assign(courier);
        _orderRepository.Received(1).UpdateOrder(order);
        _courierRepository.Received(1).UpdateCourier(courier);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnitOfWorkFailure_ReturnsFalse()
    {
        // Arrange
        var order = Substitute.For<Order>();
        var courier = Substitute.For<Courier>();
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(order, couriers).Returns(Result.Success<Courier, Error>(courier));
        order.Assign(courier).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        order.Received(1).Assign(courier);
        _orderRepository.Received(1).UpdateOrder(order);
        _courierRepository.Received(1).UpdateCourier(courier);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_ProcessesEachOrderSequentially()
    {
        // Arrange
        var order1 = Substitute.For<Order>();
        var order2 = Substitute.For<Order>();
        var courier1 = Substitute.For<Courier>();
        var courier2 = Substitute.For<Courier>();
        var orders = new[] { order1, order2 };
        var couriers = new[] { courier1, courier2 };

        courier1.Id.Returns(Guid.NewGuid());
        courier2.Id.Returns(Guid.NewGuid());

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        
        // First order assigned to first courier
        _dispatchService.Dispatch(order1, couriers).Returns(Result.Success<Courier, Error>(courier1));
        order1.Assign(courier1).Returns(UnitResult.Success<Error>());
        
        // Second order assigned to second courier (first courier filtered out)
        _dispatchService.Dispatch(order2, Arg.Is<Courier[]>(c => c.Length == 1 && c[0] == courier2))
            .Returns(Result.Success<Courier, Error>(courier2));
        order2.Assign(courier2).Returns(UnitResult.Success<Error>());
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // Verify both orders were processed
        order1.Received(1).Assign(courier1);
        order2.Received(1).Assign(courier2);
        _orderRepository.Received(1).UpdateOrder(order1);
        _orderRepository.Received(1).UpdateOrder(order2);
        _courierRepository.Received(1).UpdateCourier(courier1);
        _courierRepository.Received(1).UpdateCourier(courier2);
        
        // Verify courier filtering (second dispatch gets only remaining courier)
        _dispatchService.Received(1).Dispatch(order2, Arg.Is<Courier[]>(c => c.Length == 1 && c[0] == courier2));
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsRepositoryMethodsWithCorrectParameters()
    {
        // Arrange
        var order = Substitute.For<Order>();
        var courier = Substitute.For<Courier>();
        var orders = new[] { order };
        var couriers = new[] { courier };

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(order, couriers).Returns(Result.Success<Courier, Error>(courier));
        order.Assign(courier).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        // Verify correct repository method calls
        await _orderRepository.Received(1).GetByStatuses(OrderStatus.Created);
        await _courierRepository.Received(1).GetAvailable();
        _dispatchService.Received(1).Dispatch(order, couriers);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToUnitOfWork()
    {
        // Arrange
        var order = Substitute.For<Order>();
        var courier = Substitute.For<Courier>();
        var orders = new[] { order };
        var couriers = new[] { courier };
        var cancellationToken = new CancellationToken();

        _orderRepository.GetByStatuses(OrderStatus.Created).Returns(Task.FromResult(orders));
        _courierRepository.GetAvailable().Returns(Task.FromResult(couriers));
        _dispatchService.Dispatch(order, couriers).Returns(Result.Success<Courier, Error>(courier));
        order.Assign(courier).Returns(UnitResult.Success<Error>());
        _unitOfWork.SaveChangesAsync(cancellationToken).Returns(Task.FromResult(true));

        var command = new AssignOrdersCommand();

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}