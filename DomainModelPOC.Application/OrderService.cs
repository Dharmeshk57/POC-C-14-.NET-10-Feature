namespace DomainModelPOC.Application;

using DomainModelPOC.Domain;

// -----------------------------------------------------------
// C# 14: Primary constructor in a service class
// Dependencies are concise — no constructor body noise.
// ----------------------------------------------------------
public sealed class OrderService(IOrderRepository repository, TimeProvider clock)
{
    // C# 14: Collection expressions with spread operator
    private static readonly HashSet<OrderStatus> ActiveStatuses =
        [OrderStatus.Submitted, OrderStatus.Processing, OrderStatus.Shipped];

    public async Task<Order> CreateOrderAsync(
        Customer customer,
        CancellationToken ct = default,
        params OrderLine[] lines)   // params must be last in async methods
    {
        var order = Order.New(customer);
        order.AddLines(lines);                   // forwarded to domain
        await repository.SaveAsync(order, ct);
        return order;
    }

    public async Task AdvanceStatusAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await repository.GetAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        var next = order.Status switch
        {
            OrderStatus.Draft      => OrderStatus.Submitted,
            OrderStatus.Submitted  => OrderStatus.Processing,
            OrderStatus.Processing => OrderStatus.Shipped,
            OrderStatus.Shipped    => OrderStatus.Delivered,
            _                      => throw new InvalidOperationException(
                                         $"Order is already in a terminal state: {order.Status.DisplayLabel()}")
        };

        order.Status = next;
        await repository.SaveAsync(order, ct);
    }

    // Single-pass summary for lower allocations and less repeated work
    public async Task<OrderSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var orders = await repository.GetAllAsync(ct);
        var countByStatus = new Dictionary<OrderStatus, int>();
        var revenueByCustomer = new Dictionary<string, Money>(StringComparer.Ordinal);
        var recent = new List<Order>(10);
        var active = 0;

        foreach (var order in orders)
        {
            if (ActiveStatuses.Contains(order.Status))
                active++;

            countByStatus[order.Status] =
                countByStatus.TryGetValue(order.Status, out var statusCount)
                    ? statusCount + 1
                    : 1;

            revenueByCustomer[order.Customer.FullName] =
                revenueByCustomer.TryGetValue(order.Customer.FullName, out var total)
                    ? total + order.TotalAmount
                    : order.TotalAmount;

            InsertRecentOrder(order, recent);
        }

        var recentOrders = recent
            .Select((order, index) => $"#{index + 1} — {order.Id} ({order.Status.DisplayLabel()})")
            .ToList();

        return new OrderSummary(
            TotalOrders: orders.Count,
            Active: active,
            CountByStatus: countByStatus.ToDictionary(kv => kv.Key.DisplayLabel(), kv => kv.Value),
            RevenueByCustomer: revenueByCustomer,
            RecentOrders: recentOrders);
    }

    private static void InsertRecentOrder(Order order, List<Order> recent)
    {
        var index = recent.FindIndex(existing => order.PlacedAt > existing.PlacedAt);
        if (index < 0)
            index = recent.Count;

        if (index >= 10)
            return;

        recent.Insert(index, order);
        if (recent.Count > 10)
            recent.RemoveAt(10);
    }
}

public sealed record OrderSummary(
    int TotalOrders,
    int Active,
    Dictionary<string, int> CountByStatus,
    Dictionary<string, Money> RevenueByCustomer,
    List<string> RecentOrders);

// -----------------------------------------------------------
// Repository interface (domain boundary)
// -----------------------------------------------------------
public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(Order order, CancellationToken ct = default);
}

// -----------------------------------------------------------
// In-memory implementation for the POC
// -----------------------------------------------------------
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _store = [];

    public Task<Order?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.Values.ToList());

    public Task SaveAsync(Order order, CancellationToken ct = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}
