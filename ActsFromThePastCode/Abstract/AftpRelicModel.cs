using BaseLib.Abstracts;

namespace ActsFromThePast;

public abstract class AftpRelicModel : CustomRelicModel
{
    public override string PackedIconPath => Id.Entry.RelicAtlasPath() ?? base.PackedIconPath;
    protected override string PackedIconOutlinePath => Id.Entry.RelicOutlineAtlasPath() ?? base.PackedIconOutlinePath;
    protected override string BigIconPath => Id.Entry.BigRelicImagePath() ?? base.BigIconPath;
}
