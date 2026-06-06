namespace Domain.Accounting.Values;

public sealed record MoneyAmount
{
    public MoneyAmount(decimal value, Currency currency)
    {
        Value = value;
        Currency = currency;
    }

    public Currency Currency { get; }
    public decimal Value { get; }
}