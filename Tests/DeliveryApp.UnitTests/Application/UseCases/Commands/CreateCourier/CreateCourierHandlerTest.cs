using DeliveryApp.Core.Application.UseCases.Commands.CreateCourier;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using DeliveryApp.Core.Ports;
using FluentAssertions;
using NSubstitute;
using Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeliveryApp.UnitTests.Application.UseCases.Commands.CreateCourier;

public class CreateCourierHandlerTest
{
    private readonly ICourierRepository _courierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateCourierHandler _handler;

    public CreateCourierHandlerTest()
    {
        _courierRepository = Substitute.For<ICourierRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateCourierHandler(_courierRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var commandResult = CreateCourierCommand.Create("TestCourier", 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _courierRepository.Received(1).CreateCourier(Arg.Any<Courier>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesCourierWithCorrectProperties()
    {
        // Arrange
        var commandResult = CreateCourierCommand.Create("TestCourier", 7);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        
        Courier capturedCourier = null;
        await _courierRepository.CreateCourier(Arg.Do<Courier>(c => capturedCourier = c));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedCourier.Should().NotBeNull();
        capturedCourier.Name.Should().Be("TestCourier");
        capturedCourier.Speed.Should().Be(7);
        capturedCourier.Location.Should().NotBeNull();
        capturedCourier.StoragePlaces.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ReturnsFailure()
    {
        // Arrange
        var commandResult = CreateCourierCommand.Create("TestCourier", 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().Contain("Failed to save courier");
    }

    [Fact]
    public async Task Handle_CreatesRandomLocation()
    {
        // Arrange
        var commandResult = CreateCourierCommand.Create("TestCourier", 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        
        Courier capturedCourier = null;
        await _courierRepository.CreateCourier(Arg.Do<Courier>(c => capturedCourier = c));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCourier.Location.Should().NotBeNull();
        capturedCourier.Location.X.Should().BeInRange(1, 10);
        capturedCourier.Location.Y.Should().BeInRange(1, 10);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToUnitOfWork()
    {
        // Arrange
        var commandResult = CreateCourierCommand.Create("TestCourier", 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}

public class CreateCourierCommandTest
{
    [Theory]
    [InlineData("ValidName", 1)]
    [InlineData("TestCourier", 5)]
    [InlineData("AB", 10)]
    public void Create_WithValidParameters_ReturnsSuccess(string name, int speed)
    {
        // Act
        var result = CreateCourierCommand.Create(name, speed);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Speed.Should().Be(speed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithInvalidName_ReturnsFailure(string name)
    {
        // Act
        var result = CreateCourierCommand.Create(name, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("name");
    }

    [Fact]
    public void Create_WithNameTooShort_ReturnsFailure()
    {
        // Act
        var result = CreateCourierCommand.Create("A", 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("at least 2 characters");
    }

    [Fact]
    public void Create_WithNameTooLong_ReturnsFailure()
    {
        // Arrange
        var longName = new string('A', 51);

        // Act
        var result = CreateCourierCommand.Create(longName, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("no more than 50 characters");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Create_WithInvalidSpeed_ReturnsFailure(int speed)
    {
        // Act
        var result = CreateCourierCommand.Create("ValidName", speed);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("speed");
        result.Error.Message.Should().Contain("greater than 0");
    }

    [Fact]
    public void Create_WithSpeedTooHigh_ReturnsFailure()
    {
        // Act
        var result = CreateCourierCommand.Create("ValidName", 11);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("speed");
        result.Error.Message.Should().Contain("no more than 10");
    }
} 