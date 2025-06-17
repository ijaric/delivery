using System;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using FluentAssertions;
using Xunit;

namespace DeliveryApp.UnitTests.Domain.Model.SharedKernel;

public class LocationShould
{
    [Theory]
    [InlineData(0, 5, "x_value")]
    [InlineData(11, 5, "x_value")]
    [InlineData(5, 0, "y_value")]
    [InlineData(5, 11, "y_value")]
    public void ThrowExecptionWhenParamsAreNotCorrectOnCreated(int x, int y, string expectedParamName)
    {
        // Arrange 

        // Act 
        Action act = () => new Location(x, y);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == expectedParamName);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public void SetWhenParamsAreCorrectOnCreated(int x, int y)
    {
        // Arrange 

        // Act 
        var location = new Location(x, y);

        // Assert
        location.X.Should().Be(x);
        location.Y.Should().Be(y);
    }

    [Theory]
    [InlineData(1, 1, 1, 1, 0)]
    [InlineData(1, 1, 3, 2, 3)]
    [InlineData(10, 5, 4, 2, 9)]
    public void CorrectDistanceWhenValidParamsOnCalcDistance(int x1, int y1, int x2, int y2, int expected)

    {
        // Assrt
        var first = new Location(x1, y1);
        var second = new Location(x2, y2);

        // Act
        int distance = first.DistanceTo(second);

        // Assert
        distance.Should().Be(expected);
    }

    [Fact]
    public void ThrowExceptionWhenOtherIsNotOnCalcDistance()
    {
        // Arrange
        var location = new Location(5, 5);

        // Act
        Action act = () => location.DistanceTo(null!);

        // Assert
        act.Should()
           .Throw<ArgumentNullException>()
           .WithParameterName("other");
    }

    [Fact]
    public void CoordsWithinRangeWhenCreatedRandomLocation()
    {
        // Arrange
        const int iters = 100;

        // Act + Assert
        for (int i = 0; i < iters; i++)
        {
            var location = Location.CreateRandom();
            location.X.Should().BeInRange(1, 10);
            location.Y.Should().BeInRange(1, 10);
        }
    }

    [Fact]
    public void AreEqualWhenIdenticalLocation()
    {
        // Arrange
        var first = new Location(1, 10);
        var second = new Location(1, 10);

        // Act

        // Assert
        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }
}