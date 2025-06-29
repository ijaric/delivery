using System;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using FluentAssertions;
using Xunit;
using System.Linq;

namespace DeliveryApp.UnitTests.Domain.Model.CourierAggregate;

public class CourierShall
{
    [Fact]
    public void CreateCourierWhenCorrectParameters()
    {
        // Arrange
        string name = "Bob";
        int speed = 1;
        Location location = new(4, 4);

        // Act
        var courier = Courier.Create(name, speed, location);

        // Assert
        courier.IsSuccess.Should().BeTrue();
        courier.Value.Name.Should().Be(name);
        courier.Value.Speed.Should().Be(speed);
        courier.Value.Location.Should().Be(location);
        courier.Value.StoragePlaces.Should().HaveCount(1);
        courier.Value.StoragePlaces.First().Name.Should().Be("Сумка");
        courier.Value.StoragePlaces.First().TotalVolume.Should().Be(10);
    }

    [Theory]
    [InlineData(null, 1, 4, 4)]
    [InlineData("", 1, 4, 4)]
    [InlineData(" ", 1, 4, 4)]
    [InlineData("Bob", 0, 4, 4)]
    [InlineData("Bob", -1, 4, 4)]
    public void ReturnErrorWhenCreateCourierIncorrectParameters(string name, int speed, int x, int y)
    {
        // Arrange
        var location = new Location(x, y);

        // Act
        var courier = Courier.Create(name, speed, location);

        // Assert
        courier.IsSuccess.Should().BeFalse();
        courier.Error.Should().NotBeNull();
    }
}