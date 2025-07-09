using DeliveryApp.Core.Application.UseCases.Commands.CreateCourier;
using DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;
using DeliveryApp.Core.Application.UseCases.Queries.GetAllCouriers;
using DeliveryApp.Core.Application.UseCases.Queries.GetNotCompletedOrders;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenApi.Controllers;
using Courier = OpenApi.Models.Courier;
using Location = OpenApi.Models.Location;
using Order = OpenApi.Models.Order;
using Error = OpenApi.Models.Error;
using NewCourier = OpenApi.Models.NewCourier;

namespace DeliveryApp.Api.Adapters.Http;

[ApiController]
public class DeliveryController : DefaultApiController
{
    private readonly IMediator _mediator;

    public DeliveryController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Создать курьера
    /// </summary>
    public override async Task<IActionResult> CreateCourier([FromBody] NewCourier newCourier)
    {
        var commandResult = CreateCourierCommand.Create(newCourier.Name, newCourier.Speed);
        if (commandResult.IsFailure)
        {
            return BadRequest(new Error 
            { 
                Code = 400, 
                Message = commandResult.Error.Message 
            });
        }

        var response = await _mediator.Send(commandResult.Value);
        if (response.IsSuccess)
        {
            return Created($"/api/v1/couriers", new { success = true });
        }

        return Conflict(new Error 
        { 
            Code = 409, 
            Message = response.Error.Message 
        });
    }

    /// <summary>
    /// Создать заказ (тестовый метод)
    /// </summary>
    public override async Task<IActionResult> CreateOrder()
    {
        var random = new Random();
        var x = random.Next(1, 11); // Случайная координата X от 1 до 10
        var y = random.Next(1, 11); // Случайная координата Y от 1 до 10
        var volume = random.Next(1, 11); // Случайный объем от 1 до 10
        
        var commandResult = CreateOrderCommand.Create(x, y, volume);
        if (commandResult.IsFailure)
        {
            return BadRequest(new Error 
            { 
                Code = 400, 
                Message = commandResult.Error.Message 
            });
        }

        var response = await _mediator.Send(commandResult.Value);
        if (response.IsSuccess)
        {
            return Created($"/api/v1/orders", new { success = true });
        }

        return Conflict(new Error 
        { 
            Code = 409, 
            Message = response.Error.Message 
        });
    }

    /// <summary>
    /// Получить всех курьеров
    /// </summary>
    public override async Task<IActionResult> GetCouriers()
    {
        var query = new GetAllCouriersQuery();
        var result = await _mediator.Send(query);
        
        if (result == null) return NotFound();
        
        var couriers = result.Couriers.Select(c => new Courier
        {
            Id = c.Id,
            Name = c.Name,
            Location = new Location
            {
                X = c.Location.X,
                Y = c.Location.Y
            }
        }).ToList();
        
        return Ok(couriers);
    }

    /// <summary>
    /// Получить все незавершенные заказы
    /// </summary>
    public override async Task<IActionResult> GetOrders()
    {
        var query = new GetNotCompletedOrdersQuery();
        var result = await _mediator.Send(query);
        
        if (result == null) return NotFound();
        
        var orders = result.Orders.Select(o => new Order
        {
            Id = o.Id,
            Location = new Location
            {
                X = o.Location.X,
                Y = o.Location.Y
            }
        }).ToList();
        
        return Ok(orders);
    }
}