using CSharpFunctionalExtensions;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using Primitives;

namespace DeliveryApp.Core.Domain.Services.DispatchService;

public interface IDispatchService
{
    public Result<Courier, Error> Dispatch(Order order, Courier[] couriers);
}