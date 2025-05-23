namespace Chainer.Tests.TestUtilities;

/// <summary>
/// A simple test context for basic chain execution tests.
/// </summary>
public sealed class SimpleTestContext : ICloneable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsProcessed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }

    public object Clone()
    {
        return new SimpleTestContext
        {
            Id = Id,
            Name = Name,
            Value = Value,
            IsProcessed = IsProcessed,
            ProcessedAt = ProcessedAt
        };
    }
}

/// <summary>
/// A test context with nested objects for testing complex scenarios.
/// </summary>
public sealed class ComplexTestContext : ICloneable
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public OrderInfo Order { get; set; } = new();
    public CustomerInfo Customer { get; set; } = new();
    public List<string> ProcessingSteps { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    public object Clone()
    {
        return new ComplexTestContext
        {
            TransactionId = TransactionId,
            Order = (OrderInfo)Order.Clone(),
            Customer = (CustomerInfo)Customer.Clone(),
            ProcessingSteps = new List<string>(ProcessingSteps),
            Metadata = new Dictionary<string, object>(Metadata),
            Status = Status
        };
    }

    public sealed class OrderInfo : ICloneable
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        public object Clone()
        {
            return new OrderInfo
            {
                OrderId = OrderId,
                TotalAmount = TotalAmount,
                DiscountAmount = DiscountAmount,
                FinalAmount = FinalAmount,
                Items = Items.Select(i => (OrderItem)i.Clone()).ToList()
            };
        }
    }

    public sealed class OrderItem : ICloneable
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public object Clone()
        {
            return new OrderItem
            {
                ProductId = ProductId,
                ProductName = ProductName,
                Quantity = Quantity,
                UnitPrice = UnitPrice
            };
        }
    }

    public sealed class CustomerInfo : ICloneable
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CustomerType Type { get; set; } = CustomerType.Regular;
        public bool IsVip { get; set; }

        public object Clone()
        {
            return new CustomerInfo
            {
                CustomerId = CustomerId,
                Name = Name,
                Email = Email,
                Type = Type,
                IsVip = IsVip
            };
        }
    }

    public enum ProcessingStatus
    {
        Pending,
        Validating,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum CustomerType
    {
        Regular,
        Premium,
        Enterprise
    }
}

/// <summary>
/// A test context specifically for testing handler configuration scenarios.
/// </summary>
public sealed class ConfigurableTestContext : ICloneable
{
    public string InputValue { get; set; } = string.Empty;
    public string OutputValue { get; set; } = string.Empty;
    public decimal Multiplier { get; set; } = 1.0m;
    public bool EnableFeatureA { get; set; }
    public bool EnableFeatureB { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public List<string> AllowedOperations { get; set; } = new();

    public object Clone()
    {
        return new ConfigurableTestContext
        {
            InputValue = InputValue,
            OutputValue = OutputValue,
            Multiplier = Multiplier,
            EnableFeatureA = EnableFeatureA,
            EnableFeatureB = EnableFeatureB,
            MaxRetries = MaxRetries,
            Timeout = Timeout,
            AllowedOperations = new List<string>(AllowedOperations)
        };
    }
}

/// <summary>
/// A test context that tracks modifications for testing handler behavior.
/// </summary>
public sealed class TrackingTestContext : ICloneable
{
    private readonly List<string> _modifications = new();

    public int Counter { get; set; }
    public string CurrentHandler { get; set; } = string.Empty;
    public IReadOnlyList<string> Modifications => _modifications.AsReadOnly();
    public Dictionary<string, int> HandlerExecutionCount { get; set; } = new();
    public bool ShouldFailAtHandler { get; set; }
    public string FailAtHandlerName { get; set; } = string.Empty;

    public void AddModification(string modification)
    {
        _modifications.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {modification}");
    }

    public void IncrementHandlerCount(string handlerName)
    {
        if (HandlerExecutionCount.ContainsKey(handlerName))
            HandlerExecutionCount[handlerName]++;
        else
            HandlerExecutionCount[handlerName] = 1;
    }

    public object Clone()
    {
        var clone = new TrackingTestContext
        {
            Counter = Counter,
            CurrentHandler = CurrentHandler,
            ShouldFailAtHandler = ShouldFailAtHandler,
            FailAtHandlerName = FailAtHandlerName,
            HandlerExecutionCount = new Dictionary<string, int>(HandlerExecutionCount)
        };

        // Deep copy modifications
        foreach (var modification in _modifications)
        {
            clone._modifications.Add(modification);
        }

        return clone;
    }
}

/// <summary>
/// A minimal test context for performance and basic tests.
/// </summary>
public sealed class MinimalTestContext : ICloneable
{
    public int Value { get; set; }

    public object Clone() => new MinimalTestContext { Value = Value };
}

/// <summary>
/// A test context for testing validation scenarios.
/// </summary>
public sealed class ValidationTestContext : ICloneable
{
    public string RequiredField { get; set; } = string.Empty;
    public string? OptionalField { get; set; }
    public int Age { get; set; }
    public decimal Amount { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsValid { get; set; } = true;

    public object Clone()
    {
        return new ValidationTestContext
        {
            RequiredField = RequiredField,
            OptionalField = OptionalField,
            Age = Age,
            Amount = Amount,
            Email = Email,
            PhoneNumber = PhoneNumber,
            ValidationErrors = new List<string>(ValidationErrors),
            IsValid = IsValid
        };
    }
}