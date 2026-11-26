using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public sealed class SporeCloudPower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner)
            return;

        var players = CombatState.PlayerCreatures.Where(c => c.IsAlive).ToList();

        if (!players.Any())
            return;

        Flash();
        AFTPModAudio.Play("fungi_beast", "spore_cloud_release");

        foreach (var player in players)
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), player, Amount, null, null);
    }
}