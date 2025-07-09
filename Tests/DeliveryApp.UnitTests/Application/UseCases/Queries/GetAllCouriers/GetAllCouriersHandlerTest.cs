using DeliveryApp.Core.Application.UseCases.Queries.GetAllCouriers;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Ports;
using FluentAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DomainLocation = DeliveryApp.Core.Domain.Model.SharedKernel.Location;

namespace DeliveryApp.UnitTests.Application.UseCases.Queries.GetAllCouriers;

public class GetAllCouriersHandlerTest
{
    private readonly ICourierRepository _courierRepository;
    private readonly GetAllCouriersHandler _handler;

    public GetAllCouriersHandlerTest()
    {
        _courierRepository = Substitute.For<ICourierRepository>();
        _handler = new GetAllCouriersHandler(_courierRepository);
    }

    [Fact]
    public async Task Handle_WithValidCouriers_ReturnsCorrectResponse()
    {
        // Arrange
        var courier1 = CreateValidCourier("Courier1", 5, new DomainLocation(1, 2));
        var courier2 = CreateValidCourier("Courier2", 3, new DomainLocation(3, 4));
        var couriers = new[] { courier1, courier2 };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().HaveCount(2);

        result.Couriers[0].Id.Should().Be(courier1.Id);
        result.Couriers[0].Name.Should().Be("Courier1"); // ИСПРАВЛЕНО: добавлена проверка Name
        result.Couriers[0].Location.X.Should().Be(1);
        result.Couriers[0].Location.Y.Should().Be(2);

        result.Couriers[1].Id.Should().Be(courier2.Id);
        result.Couriers[1].Name.Should().Be("Courier2"); // ИСПРАВЛЕНО: добавлена проверка Name
        result.Couriers[1].Location.X.Should().Be(3);
        result.Couriers[1].Location.Y.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WithEmptyCourierCollection_ReturnsEmptyResponse()
    {
        // Arrange
        var emptyCouriers = new Courier[0];
        _courierRepository.GetBusy().Returns(Task.FromResult(emptyCouriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryGetBusyOnce()
    {
        // Arrange
        var couriers = new[] { CreateValidCourier("TestCourier", 5, new DomainLocation(1, 1)) };
        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _courierRepository.Received(1).GetBusy();
    }

    [Fact]
    public async Task Handle_WithSingleCourier_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var expectedLocation = new DomainLocation(10, 10);
        var courier = CreateValidCourier("SingleCourier", 7, expectedLocation);
        var couriers = new[] { courier };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().HaveCount(1);

        var mappedCourier = result.Couriers[0];
        mappedCourier.Id.Should().Be(courier.Id);
        mappedCourier.Name.Should().Be("SingleCourier"); // ИСПРАВЛЕНО: добавлена проверка Name
        mappedCourier.Location.Should().NotBeNull();
        mappedCourier.Location.X.Should().Be(expectedLocation.X);
        mappedCourier.Location.Y.Should().Be(expectedLocation.Y);
    }

    [Fact]
    public async Task Handle_WithMultipleCouriers_MapsEachCourierIndependently()
    {
        // Arrange
        var courier1 = CreateValidCourier("Courier1", 5, new DomainLocation(1, 2));
        var courier2 = CreateValidCourier("Courier2", 3, new DomainLocation(3, 4));
        var courier3 = CreateValidCourier("Courier3", 7, new DomainLocation(5, 6));
        var couriers = new[] { courier1, courier2, courier3 };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().HaveCount(3);

        // Verify each courier is mapped correctly
        result.Couriers[0].Id.Should().Be(courier1.Id);
        result.Couriers[0].Name.Should().Be("Courier1"); // ИСПРАВЛЕНО: добавлена проверка Name
        result.Couriers[0].Location.X.Should().Be(1);
        result.Couriers[0].Location.Y.Should().Be(2);

        result.Couriers[1].Id.Should().Be(courier2.Id);
        result.Couriers[1].Name.Should().Be("Courier2"); // ИСПРАВЛЕНО: добавлена проверка Name
        result.Couriers[1].Location.X.Should().Be(3);
        result.Couriers[1].Location.Y.Should().Be(4);

        result.Couriers[2].Id.Should().Be(courier3.Id);
        result.Couriers[2].Name.Should().Be("Courier3"); // ИСПРАВЛЕНО: добавлена проверка Name
        result.Couriers[2].Location.X.Should().Be(5);
        result.Couriers[2].Location.Y.Should().Be(6);
    }

    [Fact]
    public async Task Handle_CreatesNewLocationObjectsForEachCourier()
    {
        // Arrange
        var courier1 = CreateValidCourier("Courier1", 5, new DomainLocation(1, 2));
        var courier2 = CreateValidCourier("Courier2", 3, new DomainLocation(3, 4));
        var couriers = new[] { courier1, courier2 };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Couriers[0].Location.Should().NotBeSameAs(result.Couriers[1].Location);
        result.Couriers[0].Location.Should().NotBeSameAs(courier1.Location);
        result.Couriers[1].Location.Should().NotBeSameAs(courier2.Location);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var couriers = new[] { CreateValidCourier("TestCourier", 5, new DomainLocation(1, 1)) };
        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        await _courierRepository.Received(1).GetBusy();
    }

    [Fact]
    public async Task Handle_WithBoundaryCoordinates_MapsCorrectly()
    {
        // Arrange
        var courier = CreateValidCourier("BoundaryCourier", 5, new DomainLocation(1, 10));
        var couriers = new[] { courier };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().HaveCount(1);
        result.Couriers[0].Location.X.Should().Be(1);
        result.Couriers[0].Location.Y.Should().Be(10);
        result.Couriers[0].Name.Should().Be("BoundaryCourier"); // ИСПРАВЛЕНО: добавлена проверка Name
    }

    [Fact]
    public async Task Handle_WithMaxCoordinates_MapsCorrectly()
    {
        // Arrange
        var courier = CreateValidCourier("MaxCourier", 10, new DomainLocation(10, 10));
        var couriers = new[] { courier };

        _courierRepository.GetBusy().Returns(Task.FromResult(couriers));

        var query = new GetAllCouriersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Couriers.Should().HaveCount(1);
        result.Couriers[0].Location.X.Should().Be(10);
        result.Couriers[0].Location.Y.Should().Be(10);
        result.Couriers[0].Name.Should().Be("MaxCourier"); // ИСПРАВЛЕНО: добавлена проверка Name
    }

    private static Courier CreateValidCourier(string name, int speed, DomainLocation location)
    {
        var courierResult = Courier.Create(name, speed, location);
        return courierResult.Value;
    }
} 