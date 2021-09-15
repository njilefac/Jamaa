namespace Domain.Values
{
    public class MoneyAmount
    {
        public Currency Currency { get; set; }
        public decimal Value { get; set; }

        public MoneyAmount(decimal value, Currency currency)
        {
            Value = value;
            Currency = currency;
        }
    }
}
