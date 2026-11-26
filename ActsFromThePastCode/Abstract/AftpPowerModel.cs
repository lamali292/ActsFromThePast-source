using BaseLib.Abstracts;

namespace ActsFromThePast;

public abstract class AftpPowerModel : CustomPowerModel
{
    public sealed override string? CustomPackedIconPath => Id.Entry.PowerAtlasPath();
    public sealed override string? CustomBigIconPath => Id.Entry.BigPowerImagePath();
}
