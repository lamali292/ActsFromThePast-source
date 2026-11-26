using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public sealed class Snecko : CustomMonsterModel
{
    private const int VulnerableAmount = 2;
    private const int WeakAmount = 2;

    private const string GLARE = "GLARE";
    private const string BITE = "BITE";
    private const string TAIL_WHIP = "TAIL_WHIP";
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 120, 114);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 125, 120);

    private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);
    private int TailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/snecko/snecko.tscn";


    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        AFTPModAudio.Play("snecko", "snecko_death");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var glareState = new MoveState(
            GLARE,
            Glare, new DebuffIntent());
        var biteState = new MoveState(
            BITE,
            Bite, new SingleAttackIntent(BiteDamage));
        var tailState = new MoveState(
            TAIL_WHIP,
            TailWhip, new SingleAttackIntent(TailDamage), new DebuffIntent());

        var randomBranch = new RandomBranchState("RANDOM");

        glareState.FollowUpState = randomBranch;
        biteState.FollowUpState = randomBranch;
        tailState.FollowUpState = randomBranch;

        randomBranch.AddBranch(biteState, 2, 60f);
        randomBranch.AddBranch(tailState, MoveRepeatType.CanRepeatForever, 40f);

        states.Add(glareState);
        states.Add(biteState);
        states.Add(tailState);
        states.Add(randomBranch);

        return new MonsterMoveStateMachine(states, glareState);
    }

    private async Task Glare(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Glare", 0.0f);

        var combatRoom = NCombatRoom.Instance;
        var sneckoNode = combatRoom?.GetCreatureNode(Creature);
        if (sneckoNode != null)
        {
            var position = sneckoNode.VfxSpawnPosition;
            var effect = IntimidateEffect.Create(position);
            combatRoom?.CombatVfxContainer.AddChild(effect);
            effect.GlobalPosition = position;
        }

        AFTPModAudio.Play(Creature, "snecko", "snecko_glare");
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Long);
        await Cmd.Wait(1.5f);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<ConfusedPower>(new BlockingPlayerChoiceContext(), target, 1, Creature, null);
            AFTPModAudio.Play("snecko", "confusion_applied");
        }
    }

    private async Task Bite(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Bite", 0.0f);
        await Cmd.Wait(0.3f);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (targetNode != null)
            {
                var offsetX = (float)GD.RandRange(-50.0, 50.0);
                var offsetY = (float)GD.RandRange(-50.0, 50.0);
                var position = targetNode.VfxSpawnPosition + new Vector2(offsetX, offsetY);
                var effect = BiteEffect.CreateChartreuse(position);
                NCombatRoom.Instance?.CombatVfxContainer.AddChild(effect);
                effect.GlobalPosition = position;
            }
        }

        await Cmd.Wait(0.3f);

        await DamageCmd.Attack(BiteDamage)
            .FromMonster(this)
            .Execute(null);
    }

    private async Task TailWhip(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TailDamage)
            .FromMonster(this)
            .WithAttackerAnim("TailWhip", 0.25f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            if (AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies))
                await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, WeakAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), target, VulnerableAmount, Creature,
                null);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var glare = new AnimState("Attack");
        var bite = new AnimState("Attack_2");
        var hit = new AnimState("Hit");

        glare.NextState = idle;
        bite.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Glare", glare);
        animator.AddAnyState("Bite", bite);
        animator.AddAnyState("TailWhip", glare);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}