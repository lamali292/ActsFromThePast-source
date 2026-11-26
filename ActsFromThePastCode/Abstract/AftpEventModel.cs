using BaseLib.Abstracts;

namespace ActsFromThePast;

public abstract class AftpEventModel : CustomEventModel
{
    public override string? CustomInitialPortraitPath => Id.Entry.EventPortraitPath();
}
