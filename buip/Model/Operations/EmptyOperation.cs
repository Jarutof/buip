using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{
    public class EmptyOperation : Operation
    {
        public EmptyOperation(OperationInfo info) : base(info)
        {
        }

        public override async Task ProcessAsync(CancellationToken token)
        {

        }
    }
}
