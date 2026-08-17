using System.Threading;

namespace Inventory.Api.Application.Debug;

public sealed class FailureSimulationService : IFailureSimulationService
{
    private int _nextMode;

    public void Arm(FailureSimulationMode mode)
    {
        Interlocked.Exchange(ref _nextMode, (int)mode);
    }

    public bool TryConsume(out FailureSimulationMode mode)
    {
        var consumedMode = Interlocked.Exchange(ref _nextMode, 0);
        mode = (FailureSimulationMode)consumedMode;
        return consumedMode != 0;
    }
}
