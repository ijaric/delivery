namespace DeliveryApp.Core.Application.UseCases.Queries.GetCreatedAndAssignedOrders;

public class GetCreatedAndAssignedOrdersResponse
{
    public GetCreatedAndAssignedOrdersResponse(List<OrderLocation> orders)
    {
        Orders.AddRange(orders);
    }

    public List<OrderLocation> Orders { get; set; } = [];
}

public class OrderLocation
{
    /// <summary>
    ///     Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Геопозиция (X,Y)
    /// </summary>
    public Location Location { get; set; }
}

public class Location
{
    /// <summary>
    ///     Горизонталь
    /// </summary>
    public int X { get; set; }

    /// <summary>
    ///     Вертикаль
    /// </summary>
    public int Y { get; set; }
}