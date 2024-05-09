using System;
using Domain.Shared.Values;

namespace Domain.Organisation.Values
{
    public class FinancialPeriod : ITimePeriod
    {
        public DateTime Begin => throw new NotImplementedException();

        public DateTime End => throw new NotImplementedException();
    }
}
