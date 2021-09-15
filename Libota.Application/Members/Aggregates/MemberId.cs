using EventFlow.Core;

namespace Libota.Application.Members.Aggregates
{
    public class MemberId : Identity<MemberId>
    {
        public MemberId(string value)
            :base(value)
        {
        }
    }
}