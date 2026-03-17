namespace DomainModelPOC.Domain;

public sealed class Order(Guid id, Customer customer, DateTimeOffset placedAt)
{
    public Guid Id { get; } = id;
    public Customer Customer { get; } = customer;
    public DateTimeOffset PlacedAt { get; } = placedAt;
    public OrderStatus Status
    {
        get;
        set
        {
            if (field == value) return;
            if (!field.CanTransitionTo(value))
                throw new InvalidOperationException(
                    $"Cannot transition order from {field} to {value}.");
            field = value;
        }
    } = OrderStatus.Draft;

    // C# 14 field keyword in a computed property with validation
    public string? Notes
    {
        get => field;
        set => field = value?.Trim() is { Length: > 0 } trimmed ? trimmed : null;
    }

    private readonly List<OrderLine> _lines = [];

    public IReadOnlyList<OrderLine> Lines => _lines;

    // C# 14: params ReadOnlySpan<T> — zero-alloc for small call-sites
    public void AddLines(params ReadOnlySpan<OrderLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(lines));
            _lines.Add(line);
        }
    }

    public Money TotalAmount =>
        _lines.Aggregate(Money.Zero, (acc, l) => acc + l.LineTotal);

    public static Order New(Customer customer) =>
        new(Guid.CreateVersion7(), customer, DateTimeOffset.UtcNow);
}

// -----------------------------------------------------------
// C# 14: Primary constructor — all state via constructor params
// 'readonly' fields are inlined; no boilerplate assignments.
// -----------------------------------------------------------
public sealed class Customer(
    Guid id,
    string firstName,
    string lastName,
    Email email)
{
    public Guid Id { get; } = id;
    public Email Email { get; } = email;

    // field keyword: trim + validate in setter, no backing field
    public string FirstName
    {
        get;
        init => field = value.Trim().Length > 0
            ? value.Trim()
            : throw new ArgumentException("First name cannot be empty.");
    } = firstName;

    public string LastName
    {
        get;
        init => field = value.Trim().Length > 0
            ? value.Trim()
            : throw new ArgumentException("Last name cannot be empty.");
    } = lastName;

    public string FullName => $"{FirstName} {LastName}";

    public static Customer New(string firstName, string lastName, string email) =>
        new(Guid.CreateVersion7(), firstName, lastName, new Email(email));
}

// -----------------------------------------------------------
// Value objects: clean with records + C# 14 features
// -----------------------------------------------------------
public readonly record struct Money(decimal Amount, string Currency = "USD")
{
    public static readonly Money Zero = new(0m);

    // field keyword works in record structs too
    public decimal Amount
    {
        get;
        init => field = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Amount), "Amount cannot be negative.");
    } = Amount;

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        return a with { Amount = a.Amount + b.Amount };
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}

public readonly record struct Email
{
    // field keyword: validate once on construction, expose clean value
    public string Value
    {
        get;
        init => field = value.Contains('@')
            ? value.ToLowerInvariant()
            : throw new FormatException($"'{value}' is not a valid email.");
    }

    public Email(string value) => Value = value; // triggers the init above
}

public readonly record struct OrderLine(
    Guid ProductId,
    string ProductName,
    int Quantity,
    Money UnitPrice)
{
    public Money LineTotal => UnitPrice with { Amount = UnitPrice.Amount * Quantity };
}

public enum OrderStatus { Draft, Submitted, Processing, Shipped, Delivered, Cancelled }

// -----------------------------------------------------------
// Extension methods for OrderStatus
// -----------------------------------------------------------
public static class OrderStatusExtensions
{
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next) => (current, next) switch
    {
        (OrderStatus.Draft,     OrderStatus.Submitted)  => true,
        (OrderStatus.Submitted, OrderStatus.Processing) => true,
        (OrderStatus.Processing,OrderStatus.Shipped)    => true,
        (OrderStatus.Shipped,   OrderStatus.Delivered)  => true,
        (_, OrderStatus.Cancelled) when current is not OrderStatus.Delivered => true,
        _ => false
    };

    public static bool IsTerminal(this OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled;

    public static string DisplayLabel(this OrderStatus status) => status switch
    {
        OrderStatus.Draft      => "Draft",
        OrderStatus.Submitted  => "Submitted",
        OrderStatus.Processing => "Processing",
        OrderStatus.Shipped    => "Shipped",
        OrderStatus.Delivered  => "Delivered",
        OrderStatus.Cancelled  => "Cancelled",
        _ => "Unknown"
    };
}
