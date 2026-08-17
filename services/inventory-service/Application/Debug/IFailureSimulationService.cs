namespace Inventory.Api.Application.Debug;

public interface IFailureSimulationService
{
    void Arm(FailureSimulationMode mode);

    bool TryConsume(out FailureSimulationMode mode);
}
