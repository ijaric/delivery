using Microsoft.EntityFrameworkCore;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.OrderAggregate;

namespace DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;

public class OrderRepository : IOrderRepository
{
    public readonly ApplicationDbContext _dbContext;

    public OrderRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateOrder(Order order)
    {
        await _dbContext.Orders.AddAsync(order);
    }

    public void UpdateOrder(Order order)
    {
        _dbContext.Update(order);
    }

    public async Task<Order> GetById(Guid id)
    {
        return await _dbContext.Orders.FindAsync(id);
    }

    public async Task<Order> GetAnyCreated()
    {
        return await _dbContext.Orders.Where(o => o.Status.Name == OrderStatus.Created.Name).FirstAsync();
    }

    public async Task<Order[]> GetByStatus(OrderStatus status)
    {
        return await _dbContext.Orders.Where(o => o.Status.Name == status.Name).ToArrayAsync();
    }
}