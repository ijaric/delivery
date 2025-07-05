using CSharpFunctionalExtensions;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using Primitives;

namespace DeliveryApp.Core.Ports;

public interface IOrderRepository
{
    public Task CreateOrder(Order order);

    public void UpdateOrder(Order order);

    public Task<Order> GetById(Guid id);

    public Task<Order> GetAnyCreated();

    public Task<Order[]> GetByStatus(OrderStatus status);    
}