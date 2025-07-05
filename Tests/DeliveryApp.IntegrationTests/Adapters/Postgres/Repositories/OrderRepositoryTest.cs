using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using DeliveryApp.Infrastructure.Adapters.Postgres;
using DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using FluentAssertions;
using Xunit;

namespace DeliveryApp.IntegrationTests.Adapters.Postgres.Repositories;

public class OrderRepositoryTest : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    public OrderRepositoryTest()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("test_delivery")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    private async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        await _container.StartAsync();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureDeletedAsync(); // Clean slate for each test
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }

    [Fact]
    public async Task CreateOrder_ShouldAddOrderToDatabase()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        var orderId = Guid.NewGuid();
        var location = new Location(5, 7);
        var orderResult = Order.Create(orderId, location, 25);
        var order = orderResult.Value;

        // Act
        await repository.CreateOrder(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        
        savedOrder.Should().NotBeNull();
        savedOrder.Id.Should().Be(orderId);
        savedOrder.Location.X.Should().Be(5);
        savedOrder.Location.Y.Should().Be(7);
        savedOrder.Volume.Should().Be(25);
        savedOrder.Status.Should().Be(OrderStatus.Created);
        savedOrder.CourierId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateOrder_ShouldModifyOrderInDatabase()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        var orderId = Guid.NewGuid();
        var location = new Location(3, 4);
        var orderResult = Order.Create(orderId, location, 15);
        var order = orderResult.Value;
        
        await repository.CreateOrder(order);
        await context.SaveChangesAsync();
        
        // Create and assign a courier
        var courierLocation = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 2, courierLocation);
        var courier = courierResult.Value;
        
        await context.Couriers.AddAsync(courier);
        await context.SaveChangesAsync();
        
        // Detach to simulate fresh load
        context.Entry(order).State = EntityState.Detached;
        
        // Load fresh instance and assign courier
        var freshOrder = await context.Orders.FirstAsync(o => o.Id == orderId);
        freshOrder.Assign(courier);

        // Act
        repository.UpdateOrder(freshOrder);
        await context.SaveChangesAsync();

        // Assert
        var updatedOrder = await context.Orders.FirstAsync(o => o.Id == orderId);
        updatedOrder.Status.Should().Be(OrderStatus.Assigned);
        updatedOrder.CourierId.Should().Be(courier.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnCorrectOrder()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        var orderId = Guid.NewGuid();
        var location = new Location(2, 3);
        var orderResult = Order.Create(orderId, location, 30);
        var order = orderResult.Value;
        
        await repository.CreateOrder(order);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetById(orderId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.Location.X.Should().Be(2);
        result.Location.Y.Should().Be(3);
        result.Volume.Should().Be(30);
        result.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetById(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAnyCreated_ShouldReturnFirstCreatedOrder()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        // Create multiple orders with different statuses
        var location1 = new Location(1, 1);
        var order1Result = Order.Create(Guid.NewGuid(), location1, 10);
        var order1 = order1Result.Value;
        
        var location2 = new Location(2, 2);
        var order2Result = Order.Create(Guid.NewGuid(), location2, 20);
        var order2 = order2Result.Value;
        
        var location3 = new Location(3, 3);
        var order3Result = Order.Create(Guid.NewGuid(), location3, 30);
        var order3 = order3Result.Value;
        
        // Assign order2 to change its status
        var courierLocation = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 2, courierLocation);
        var courier = courierResult.Value;
        order2.Assign(courier);
        
        await repository.CreateOrder(order1);
        await repository.CreateOrder(order2);
        await repository.CreateOrder(order3);
        await context.Couriers.AddAsync(courier);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAnyCreated();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Created);
        // Should be either order1 or order3 (both are created)
        var createdOrderIds = new[] { order1.Id, order3.Id };
        createdOrderIds.Should().Contain(result.Id);
    }

    [Fact]
    public async Task GetAnyCreated_WithNoCreatedOrders_ShouldThrowException()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        var location = new Location(1, 1);
        var orderResult = Order.Create(Guid.NewGuid(), location, 10);
        var order = orderResult.Value;
        
        // Create courier and assign order
        var courierLocation = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 2, courierLocation);
        var courier = courierResult.Value;
        order.Assign(courier);
        
        await repository.CreateOrder(order);
        await context.Couriers.AddAsync(courier);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.GetAnyCreated());
    }

    [Fact]
    public async Task GetByStatus_ShouldReturnOrdersWithSpecifiedStatus()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        // Create orders with different statuses
        var location1 = new Location(1, 1);
        var createdOrder1Result = Order.Create(Guid.NewGuid(), location1, 10);
        var createdOrder1 = createdOrder1Result.Value;
        
        var location2 = new Location(2, 2);
        var createdOrder2Result = Order.Create(Guid.NewGuid(), location2, 20);
        var createdOrder2 = createdOrder2Result.Value;
        
        var location3 = new Location(3, 3);
        var assignedOrderResult = Order.Create(Guid.NewGuid(), location3, 30);
        var assignedOrder = assignedOrderResult.Value;
        
        // Create courier and assign one order
        var courierLocation = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 2, courierLocation);
        var courier = courierResult.Value;
        assignedOrder.Assign(courier);
        
        await repository.CreateOrder(createdOrder1);
        await repository.CreateOrder(createdOrder2);
        await repository.CreateOrder(assignedOrder);
        await context.Couriers.AddAsync(courier);
        await context.SaveChangesAsync();

        // Act - Get created orders
        var createdOrders = await repository.GetByStatus(OrderStatus.Created);
        
        // Assert
        createdOrders.Should().HaveCount(2);
        createdOrders.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.Created));
        var createdOrderIds = createdOrders.Select(o => o.Id).ToArray();
        createdOrderIds.Should().Contain(createdOrder1.Id);
        createdOrderIds.Should().Contain(createdOrder2.Id);
        createdOrderIds.Should().NotContain(assignedOrder.Id);

        // Act - Get assigned orders
        var assignedOrders = await repository.GetByStatus(OrderStatus.Assigned);
        
        // Assert
        assignedOrders.Should().HaveCount(1);
        assignedOrders[0].Id.Should().Be(assignedOrder.Id);
        assignedOrders[0].Status.Should().Be(OrderStatus.Assigned);
    }

    [Fact]
    public async Task GetByStatus_WithNoMatchingOrders_ShouldReturnEmptyArray()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        var location = new Location(1, 1);
        var orderResult = Order.Create(Guid.NewGuid(), location, 10);
        var order = orderResult.Value;
        
        await repository.CreateOrder(order);
        await context.SaveChangesAsync();

        // Act - Look for completed orders when none exist
        var completedOrders = await repository.GetByStatus(OrderStatus.Completed);

        // Assert
        completedOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatus_Assigned_ShouldReturnOnlyAssignedOrders()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new OrderRepository(context);
        
        // Create multiple orders
        var orders = new List<Order>();
                 for (int i = 0; i < 5; i++)
         {
             var location = new Location(i + 1, i + 1); // Ensure coordinates are 1-10
             var orderResult = Order.Create(Guid.NewGuid(), location, 10 + i);
            orders.Add(orderResult.Value);
            await repository.CreateOrder(orderResult.Value);
        }
        
        // Create courier and assign 3 orders
        var courierLocation = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 2, courierLocation);
        var courier = courierResult.Value;
        
        orders[1].Assign(courier);
        orders[3].Assign(courier);
        orders[4].Assign(courier);
        
        await context.Couriers.AddAsync(courier);
        await context.SaveChangesAsync();

        // Act
        var assignedOrders = await repository.GetByStatus(OrderStatus.Assigned);

        // Assert
        assignedOrders.Should().HaveCount(3);
        assignedOrders.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.Assigned));
        assignedOrders.Should().AllSatisfy(o => o.CourierId.Should().Be(courier.Id));
        
        var assignedOrderIds = assignedOrders.Select(o => o.Id).ToArray();
        assignedOrderIds.Should().Contain(orders[1].Id);
        assignedOrderIds.Should().Contain(orders[3].Id);
        assignedOrderIds.Should().Contain(orders[4].Id);
        assignedOrderIds.Should().NotContain(orders[0].Id);
        assignedOrderIds.Should().NotContain(orders[2].Id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
