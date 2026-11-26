using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class WheelSpinMinigame(Player owner, int result, int actIndex)
{
    private readonly TaskCompletionSource _completionSource = new();

    /// <summary>
    ///     Visual landing angle in degrees, includes a small cosmetic offset.
    /// </summary>
    public float ResultAngle { get; } = result * 60f + Rng.Chaotic.NextInt(-10, 11);

    /// <summary>
    ///     Current act index, used to select the event background.
    /// </summary>
    public int ActIndex { get; } = actIndex;

    public event Action? Finished;

    public void Complete()
    {
        if (_completionSource.Task.IsCompleted) return;
        _completionSource.SetResult();
        Finished?.Invoke();
    }

    public void ForceEnd()
    {
        _completionSource.TrySetCanceled();
    }

    public async Task PlayMinigame()
    {
        if (!LocalContext.IsMe(owner))
            return;

        NWheelSpinScreen.ShowScreen(this);
        await _completionSource.Task;
    }
}