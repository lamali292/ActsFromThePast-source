using BaseLib.Abstracts;

namespace ActsFromThePast.Dread;

/// <summary>
///     Base of the powers that belong to a Dominion. A power with art of its own in ImageGen/images/powers (named after its id, like
///     <c>champ_dominion_power.png</c>) uses it. The others borrow a unique icon of the base game (<see cref="IconEntry" />, the id
///     entry of a base game power such as <c>GUARDED_POWER</c>). No two Dominion powers share an icon.
/// </summary>
public abstract class DreadPowerModel : CustomPowerModel
{
    protected abstract string IconEntry { get; }

    public override string? CustomPackedIconPath => Id.Entry.PowerAtlasPath() ??
                                                    $"res://images/atlases/power_atlas.sprites/{IconEntry.ToLowerInvariant()}.tres";

    public override string? CustomBigIconPath =>
        Id.Entry.BigPowerImagePath() ?? $"res://images/powers/{IconEntry.ToLowerInvariant()}.png";
}
