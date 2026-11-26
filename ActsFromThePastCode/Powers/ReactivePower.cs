using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ReactivePower : AftpPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() ||
            result.UnblockedDamage <= 0 || target.CurrentHp <= 0)
            return Task.CompletedTask;

        Flash();
        if (Owner.Monster is not { } monster) return Task.CompletedTask;
        var currentMoveId = monster.NextMove?.Id;
        if (monster.MoveStateMachine == null) return Task.CompletedTask;
        var candidates = monster.MoveStateMachine.States.Values
            .Where(s => s.IsMove && s.Id != currentMoveId && s.Id != "MOVE_BRANCH")
            .ToList();

        if (candidates.Count <= 0) return Task.CompletedTask;

        // Filter out one-time moves that have already been used
        if (monster is WrithingMass { UsedMegaDebuff: true })
            candidates.RemoveAll(s => s.Id == "MEGA_DEBUFF");

        if (candidates.Count <= 0) return Task.CompletedTask;
        var next = candidates[monster.RunRng.MonsterAi.NextInt(candidates.Count)];
        monster.SetMoveImmediate((MoveState)next);


        return Task.CompletedTask;
    }
}