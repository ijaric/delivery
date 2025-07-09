using DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using DeliveryApp.Core.Ports;
using FluentAssertions;
using NSubstitute;
using Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeliveryApp.UnitTests.Application.UseCases.Commands.CreateOrder;

public class CreateOrderHandlerTest
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTest()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateOrderHandler(_orderRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var commandResult = CreateOrderCommand.Create(1, 2, 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _orderRepository.Received(1).CreateOrder(Arg.Any<Order>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesOrderWithCorrectProperties()
    {
        // Arrange
        var commandResult = CreateOrderCommand.Create(3, 7, 8);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        
        Order capturedOrder = null;
        await _orderRepository.CreateOrder(Arg.Do<Order>(o => capturedOrder = o));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder.Location.X.Should().Be(3);
        capturedOrder.Location.Y.Should().Be(7);
        capturedOrder.Volume.Should().Be(8);
        capturedOrder.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ReturnsFailure()
    {
        // Arrange
        var commandResult = CreateOrderCommand.Create(1, 2, 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Message.Should().Contain("Failed to save order");
    }

    [Fact]
    public async Task Handle_CreatesOrderWithNewGuid()
    {
        // Arrange
        var commandResult = CreateOrderCommand.Create(1, 2, 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        
        Order capturedOrder = null;
        await _orderRepository.CreateOrder(Arg.Do<Order>(o => capturedOrder = o));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOrder.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToUnitOfWork()
    {
        // Arrange
        var commandResult = CreateOrderCommand.Create(1, 2, 5);
        var command = commandResult.Value;
        
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }
}

public class CreateOrderCommandTest
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(5, 5, 5)]
    [InlineData(10, 10, 10)]
    public void Create_WithValidParameters_ReturnsSuccess(int x, int y, int volume)
    {
        // Act
        var result = CreateOrderCommand.Create(x, y, volume);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.X.Should().Be(x);
        result.Value.Y.Should().Be(y);
        result.Value.Volume.Should().Be(volume);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_WithInvalidX_ReturnsFailure(int x)
    {
        // Act
        var result = CreateOrderCommand.Create(x, 5, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("X coordinate must be between 1 and 10");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_WithInvalidY_ReturnsFailure(int y)
    {
        // Act
        var result = CreateOrderCommand.Create(5, y, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Y coordinate must be between 1 and 10");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_WithInvalidVolume_ReturnsFailure(int volume)
    {
        // Act
        var result = CreateOrderCommand.Create(5, 5, volume);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Volume must be between 1 and 10");
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(11, 11, 11)]
    [InlineData(-1, -1, -1)]
    public void Create_WithMultipleInvalidParameters_ReturnsFailureWithAllErrors(int x, int y, int volume)
    {
        // Act
        var result = CreateOrderCommand.Create(x, y, volume);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("X coordinate must be between 1 and 10");
        result.Error.Message.Should().Contain("Y coordinate must be between 1 and 10");
        result.Error.Message.Should().Contain("Volume must be between 1 and 10");
    }
} 