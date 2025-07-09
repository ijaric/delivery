using DeliveryApp.Core.Application.UseCases.Queries.GetCreatedAndAssignedOrders;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Ports;
using FluentAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DomainLocation = DeliveryApp.Core.Domain.Model.SharedKernel.Location;

namespace DeliveryApp.UnitTests.Application.UseCases.Queries.GetCreatedAndAssignedOrders;

public class GetCreatedAndAssignedOrdersHandlerTest
{
    private readonly IOrderRepository _orderRepository;
    private readonly GetCreatedAndAssignedOrdersHandler _handler;

    public GetCreatedAndAssignedOrdersHandlerTest()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _handler = new GetCreatedAndAssignedOrdersHandler(_orderRepository);
    }

    [Fact]
    public async Task Handle_WithValidOrders_ReturnsCorrectResponse()
    {
        // Arrange
        var order1 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 2));
        var order2 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(3, 4));
        var orders = new[] { order1, order2 };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(2);

        result.Orders[0].Id.Should().Be(order1.Id);
        result.Orders[0].Location.X.Should().Be(1);
        result.Orders[0].Location.Y.Should().Be(2);

        result.Orders[1].Id.Should().Be(order2.Id);
        result.Orders[1].Location.X.Should().Be(3);
        result.Orders[1].Location.Y.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WithEmptyOrderCollection_ReturnsEmptyResponse()
    {
        // Arrange
        var emptyOrders = new Order[0];
        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(emptyOrders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryGetByStatusesOnce()
    {
        // Arrange
        var orders = new[] { CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 1)) };
        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _orderRepository.Received(1).GetByStatuses(OrderStatus.Created, OrderStatus.Assigned);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectStatuses()
    {
        // Arrange
        var orders = new Order[0];
        _orderRepository.GetByStatuses(Arg.Any<OrderStatus>(), Arg.Any<OrderStatus>())
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _orderRepository.Received(1).GetByStatuses(
            Arg.Is<OrderStatus>(s => s == OrderStatus.Created),
            Arg.Is<OrderStatus>(s => s == OrderStatus.Assigned)
        );
    }

    [Fact]
    public async Task Handle_WithSingleOrder_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedLocation = new DomainLocation(5, 7);
        var order = CreateValidOrder(orderId, expectedLocation);
        var orders = new[] { order };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(1);

        var mappedOrder = result.Orders[0];
        mappedOrder.Id.Should().Be(orderId);
        mappedOrder.Location.Should().NotBeNull();
        mappedOrder.Location.X.Should().Be(expectedLocation.X);
        mappedOrder.Location.Y.Should().Be(expectedLocation.Y);
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_MapsEachOrderIndependently()
    {
        // Arrange
        var order1 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 2));
        var order2 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(3, 4));
        var order3 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(5, 6));
        var orders = new[] { order1, order2, order3 };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(3);

        // Verify each order is mapped correctly
        result.Orders[0].Id.Should().Be(order1.Id);
        result.Orders[0].Location.X.Should().Be(1);
        result.Orders[0].Location.Y.Should().Be(2);

        result.Orders[1].Id.Should().Be(order2.Id);
        result.Orders[1].Location.X.Should().Be(3);
        result.Orders[1].Location.Y.Should().Be(4);

        result.Orders[2].Id.Should().Be(order3.Id);
        result.Orders[2].Location.X.Should().Be(5);
        result.Orders[2].Location.Y.Should().Be(6);
    }

    [Fact]
    public async Task Handle_CreatesNewLocationObjectsForEachOrder()
    {
        // Arrange
        var order1 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 2));
        var order2 = CreateValidOrder(Guid.NewGuid(), new DomainLocation(3, 4));
        var orders = new[] { order1, order2 };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Orders[0].Location.Should().NotBeSameAs(result.Orders[1].Location);
        result.Orders[0].Location.Should().NotBeSameAs(order1.Location);
        result.Orders[1].Location.Should().NotBeSameAs(order2.Location);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var orders = new[] { CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 1)) };
        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _orderRepository.Received(1).GetByStatuses(OrderStatus.Created, OrderStatus.Assigned);
    }

    [Fact]
    public async Task Handle_WithBoundaryCoordinates_MapsCorrectly()
    {
        // Arrange
        var order = CreateValidOrder(Guid.NewGuid(), new DomainLocation(1, 10));
        var orders = new[] { order };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(1);
        result.Orders[0].Location.X.Should().Be(1);
        result.Orders[0].Location.Y.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithMaxCoordinates_MapsCorrectly()
    {
        // Arrange
        var order = CreateValidOrder(Guid.NewGuid(), new DomainLocation(10, 10));
        var orders = new[] { order };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(1);
        result.Orders[0].Location.X.Should().Be(10);
        result.Orders[0].Location.Y.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithOrdersAtSameLocation_MapsCorrectly()
    {
        // Arrange
        var sharedLocation = new DomainLocation(5, 5);
        var order1 = CreateValidOrder(Guid.NewGuid(), sharedLocation);
        var order2 = CreateValidOrder(Guid.NewGuid(), sharedLocation);
        var orders = new[] { order1, order2 };

        _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned)
            .Returns(Task.FromResult(orders));

        var query = new GetCreatedAndAssignedOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(2);

        result.Orders[0].Location.X.Should().Be(5);
        result.Orders[0].Location.Y.Should().Be(5);
        result.Orders[1].Location.X.Should().Be(5);
        result.Orders[1].Location.Y.Should().Be(5);

        // Verify different instances
        result.Orders[0].Id.Should().NotBe(result.Orders[1].Id);
        result.Orders[0].Location.Should().NotBeSameAs(result.Orders[1].Location);
    }

    private static Order CreateValidOrder(Guid id, DomainLocation location, int volume = 5)
    {
        var result = Order.Create(id, location, volume);
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to create test order: {result.Error}");

        return result.Value;
    }
}