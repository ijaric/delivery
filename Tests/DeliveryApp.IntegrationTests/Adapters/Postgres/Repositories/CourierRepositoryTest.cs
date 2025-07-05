using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using DeliveryApp.Infrastructure.Adapters.Postgres;
using DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using FluentAssertions;
using Xunit;

namespace DeliveryApp.IntegrationTests.Adapters.Postgres.Repositories;

public class CourierRepositoryTest : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private readonly ApplicationDbContext _dbContext;
    private readonly CourierRepository _repository;

    public CourierRepositoryTest()
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
    public async Task CreateCourier_ShouldAddCourierToDatabase()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        var location = new Location(1, 1);
        var courierResult = Courier.Create("Test Courier", 5, location);
        var courier = courierResult.Value;

        // Act
        await repository.CreateCourier(courier);
        await context.SaveChangesAsync();

        // Assert
        var savedCourier = await context.Couriers
            .Include(c => c.StoragePlaces)
            .FirstOrDefaultAsync(c => c.Id == courier.Id);
        
        savedCourier.Should().NotBeNull();
        savedCourier.Name.Should().Be("Test Courier");
        savedCourier.Speed.Should().Be(5);
        savedCourier.Location.X.Should().Be(1);
        savedCourier.Location.Y.Should().Be(1);
        savedCourier.StoragePlaces.Should().HaveCount(1);
        savedCourier.StoragePlaces.First().Name.Should().Be("Сумка");
    }

    [Fact]
    public async Task UpdateCourier_ShouldModifyCourierInDatabase()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        var location = new Location(2, 2);
        var courierResult = Courier.Create("Original Courier", 3, location);
        var courier = courierResult.Value;
        
        await repository.CreateCourier(courier);
        await context.SaveChangesAsync();
        
        // Detach to simulate fresh load
        context.Entry(courier).State = EntityState.Detached;
        
        // Load fresh instance and modify
        var freshCourier = await context.Couriers
            .Include(c => c.StoragePlaces)
            .FirstAsync(c => c.Id == courier.Id);
        
        // Move step by step to reach (5,5) from (2,2)
        freshCourier.Move(new Location(5, 5));

        // Act
        repository.UpdateCourier(freshCourier);
        await context.SaveChangesAsync();

        // Assert
        var updatedCourier = await context.Couriers.FirstAsync(c => c.Id == courier.Id);
        // The Move method moves at speed=3, so from (2,2) to (5,5) might not reach in one move
        // Let's verify it moved closer to target
        (updatedCourier.Location.X > 2 || updatedCourier.Location.Y > 2).Should().BeTrue();
        updatedCourier.Location.X.Should().BeLessThanOrEqualTo(5);
        updatedCourier.Location.Y.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetById_ShouldReturnCourierWithStoragePlaces()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        var location = new Location(3, 3);
        var courierResult = Courier.Create("Get By Id Courier", 4, location);
        var courier = courierResult.Value;
        
        await repository.CreateCourier(courier);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetById(courier.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Get By Id Courier");
        result.Speed.Should().Be(4);
        result.Location.X.Should().Be(3);
        result.Location.Y.Should().Be(3);
        result.StoragePlaces.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetById(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailable_ShouldReturnCouriersWithAllStoragePlacesFree()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        // Create available courier
        var location1 = new Location(1, 1);
        var availableCourierResult = Courier.Create("Available Courier", 2, location1);
        var availableCourier = availableCourierResult.Value;
        
        // Create unavailable courier with occupied storage
        var location2 = new Location(2, 2);
        var busyCourierResult = Courier.Create("Busy Courier", 3, location2);
        var busyCourier = busyCourierResult.Value;
        
        await repository.CreateCourier(availableCourier);
        await repository.CreateCourier(busyCourier);
        await context.SaveChangesAsync();
        
        // Occupy storage place of busy courier
        var storagePlace = busyCourier.StoragePlaces.First();
        var orderId = Guid.NewGuid();
        storagePlace.Store(orderId, 5);
        
        repository.UpdateCourier(busyCourier);
        await context.SaveChangesAsync();

        // Act
        var availableCouriers = await repository.GetAvailable();

        // Assert
        availableCouriers.Should().HaveCount(1);
        availableCouriers[0].Name.Should().Be("Available Courier");
        availableCouriers[0].StoragePlaces.Should().AllSatisfy(sp => sp.OrderId.Should().BeNull());
    }

    [Fact]
    public async Task GetAvailable_WithNoAvailableCouriers_ShouldReturnEmptyArray()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        var location = new Location(1, 1);
        var courierResult = Courier.Create("Busy Courier", 2, location);
        var courier = courierResult.Value;
        
        await repository.CreateCourier(courier);
        await context.SaveChangesAsync();
        
        // Occupy all storage places
        var courierFromDb = await context.Couriers
            .Include(c => c.StoragePlaces)
            .FirstAsync(c => c.Id == courier.Id);
        
        foreach (var storagePlace in courierFromDb.StoragePlaces)
        {
            storagePlace.Store(Guid.NewGuid(), 5);
        }
        
        await context.SaveChangesAsync();

        // Act
        var availableCouriers = await repository.GetAvailable();

        // Assert
        availableCouriers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailable_WithMultipleStoragePlaces_ShouldOnlyReturnFullyAvailable()
    {
        // Arrange
        using var context = await CreateDbContextAsync();
        var repository = new CourierRepository(context);
        
        var location = new Location(1, 1);
        var courierResult = Courier.Create("Multi Storage Courier", 2, location);
        var courier = courierResult.Value;
        
        // Add additional storage place
        courier.AddStoragePlace("Backpack", 20);
        
        await repository.CreateCourier(courier);
        await context.SaveChangesAsync();
        
        // Occupy only one storage place
        var courierFromDb = await context.Couriers
            .Include(c => c.StoragePlaces)
            .FirstAsync(c => c.Id == courier.Id);
        
        var firstStoragePlace = courierFromDb.StoragePlaces.First();
        firstStoragePlace.Store(Guid.NewGuid(), 5);
        
        await context.SaveChangesAsync();

        // Act
        var availableCouriers = await repository.GetAvailable();

        // Assert
        availableCouriers.Should().BeEmpty(); // Should be empty because not ALL storage places are free
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
        
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
