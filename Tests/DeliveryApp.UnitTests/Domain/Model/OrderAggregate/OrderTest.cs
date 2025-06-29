using System;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using FluentAssertions;
using Xunit;

namespace DeliveryApp.UnitTests.Domain.Model.OrderAggregate;

public class OrderShall
{
    [Fact]
    public void CreateOrderWhenCorrectParameters()
    {
        // Arrange
        Guid orderId = Guid.NewGuid();
        Location location = new(4, 4);
        int volume = 8;

        // Act
        var order = Order.Create(orderId, location, volume);

        // Assert
        order.IsSuccess.Should().BeTrue();
        order.Value.Id.Should().Be(orderId);
        order.Value.Location.Should().Be(location);
        order.Value.Volume.Should().Be(volume);
        order.Value.Status.Should().Be(OrderStatus.Created);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 5, 5, 10)]
    [InlineData("a8d80e46-6e32-4436-95f0-612348a83985", null, null, 10)]
    [InlineData("a8d80e46-6e32-4436-95f0-612348a83985", 5, 5, 0)]
    [InlineData("a8d80e46-6e32-4436-95f0-612348a83985", 5, 5, -1)]
    public void ReturnErrorWhenCreateOrderIncorrectParameters(string orderId, int? x, int? y, int volume)
    {
        // Arrange
        var location = x.HasValue && y.HasValue ? new Location(x.Value, y.Value) : null;

        // Act
        var order = Order.Create(Guid.Parse(orderId), location, volume);

        // Assert
        order.IsSuccess.Should().BeFalse();
        order.Error.Should().NotBeNull();
    }
}