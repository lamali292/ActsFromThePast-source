using ActsFromThePast.Compatibility;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MysteriousSphere : AftpEventModel
{

    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    public override EncounterModel CanonicalEncounter =>
        ModelDb.Encounter<TwoOrbWalkersEvent>();

    public override ActModel[] Acts => [ModelDb.Act<TheBeyondAct>()];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        if (ActsFromThePastConfig.RebalancedMode)
            return
            [
                Option(Open),
                Option(Distract, "INITIAL_REBALANCED")
            ];
        return
        [
            Option(Open),
            Option(Leave)
        ];
    }

    private Task Open()
    {
        MysteriousSpherePatches.SwapToOpenSphere();
        SetEventState(PageDescription("PRE_COMBAT"), [
            Option(Fight, "PRE_COMBAT")
        ]);
        return Task.CompletedTask;
    }

    private Task Fight()
    {
        if (Owner == null) return Task.CompletedTask;
        var rareRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Rare).ToMutable();
        var rewards = new List<Reward>
        {
            new GoldReward(45, 55, Owner),
            GameApiCallCompat.CreateRelicReward(rareRelic, Owner)
        };
        EnterCombatWithoutExitingEvent<TwoOrbWalkersEvent>(rewards, false);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    private async Task Distract()
    {
        if (Owner == null) return;
        var enemies = _combatSynchronizer?.CombatStateForLayout?.Enemies;
        if (enemies == null) return;
        foreach (var enemy in enemies)
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(enemy);
            if (creatureNode == null) continue;

            var scale = creatureNode.Scale;
            creatureNode.Scale = new Vector2(-scale.X, scale.Y);

            var endPos = creatureNode.Position + new Vector2(1200f, 0f);
            var tween = creatureNode.CreateTween();
            tween.TweenProperty(creatureNode, "position", endPos, 3.0f)
                .SetTrans(Tween.TransitionType.Linear);
        }

        MysteriousSpherePatches.SwapToOpenSphere();

        var commonRelic = RelicFactory.PullNextRelicFromFront(Owner, RelicRarity.Common).ToMutable();
        await RewardsCmd.OfferCustom(Owner, [GameApiCallCompat.CreateRelicReward(commonRelic, Owner)]);
        SetEventFinished(PageDescription("DISTRACT"));
    }
}