using System;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using FluentAssertions;
using Xunit;

namespace DeliveryApp.UnitTests.Domain.Model.CourierAggregate;

public class CourierAggregateShall
{
    [Theory]
    [InlineData("bag", 10)]
    [InlineData("backpack", 5)]
    public void CreateStorageWhenCorrectParameters(string name, int volume)
    {
        // Arrange

        // Act
        var storagePlace = StoragePlace.Create(name, volume);

        // Assert
        storagePlace.IsSuccess.Should().BeTrue();
        storagePlace.Value.Name.Should().Be(name);
        storagePlace.Value.TotalVolume.Should().Be(volume);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData(" ", 10)]
    [InlineData(null, 10)]
    [InlineData("bag", 0)]
    [InlineData("bag", -1)]
    public void ReturnErrorWhenIncorrectParameters(string name, int volume)
    {
        // Arrange

        // Act
        var storagePlace = StoragePlace.Create(name, volume);

        // Assert
        storagePlace.IsSuccess.Should().BeFalse();
        storagePlace.Error.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void CanStoreWhenItemSmallerStorage(int volume)
    {
        // Arrange
        var storagePlaceResult = StoragePlace.Create("bag", 5);
        storagePlaceResult.IsSuccess.Should().BeTrue();
        var storagePlace = storagePlaceResult.Value;

        // Act
        var result = storagePlace.CanStore(volume);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    public void CannotStoreWhenItemLargerStorage(int volume)
    {
        // Arrange
        var storagePlaceResult = StoragePlace.Create("bag", 5);
        storagePlaceResult.IsSuccess.Should().BeTrue();
        var storagePlace = storagePlaceResult.Value;

        // Act
        var result = storagePlace.CanStore(volume);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReturnErrorWhenIncorrectVolume(int volume)
    {
        // Arrange
        // Arrange
        var storagePlaceResult = StoragePlace.Create("bag", 5);
        storagePlaceResult.IsSuccess.Should().BeTrue();
        var storagePlace = storagePlaceResult.Value;

        // Act
        var result = storagePlace.CanStore(volume);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
}