using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Exploder : CustomMonsterModel
{
    private const int ExplosiveCountdown = 3;
    private const int ExplodeDamage = 30;

    private const string ATTACK = "ATTACK";
    private const string EXPLODE = "EXPLODE";


    private int _turnCount;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 30, 30);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 35, 30);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 9);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/exploder/exploder.tscn";

    private int TurnCount
    {
        get => _turnCount;
        set
        {
            AssertMutable();
            _turnCount = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _turnCount = 0;
        // await PowerCmd.Apply<ExplosivePower>(new BlockingPlayerChoiceContext(), Creature, ExplosiveCountdown, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var attackState = new MoveState(
            ATTACK,
            Attack,
            new SingleAttackIntent(AttackDamage)
        );

        var explodeState = new MoveState(
            EXPLODE,
            Explode,
            new DeathBlowIntent(() => ExplodeDamage)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        attackState.FollowUpState = moveBranch;
        explodeState.FollowUpState = moveBranch;

        states.Add(attackState);
        states.Add(explodeState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        TurnCount++;
        return TurnCount <= 2 ? ATTACK : EXPLODE;
    }

    private async Task Attack(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Explode(IReadOnlyList<Creature> targets)
    {
        if (Creature.IsDead)
            return;

        await CreatureCmd.TriggerAnim(Creature, "ExplodeTrigger", 0.3f);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
            NFireSmokePuffVfx.Create(Creature));
        await Cmd.Wait(0.1f);

        foreach (var target in targets.Where(t => t.IsAlive))
            await GameApiCallCompat.Damage(
                new BlockingPlayerChoiceContext(),
                target,
                ExplodeDamage,
                ValueProp.Move,
                null);

        await CreatureCmd.Kill(Creature);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var hit = new AnimState("Hit");
        hit.NextState = idle;
        var explode = new AnimState("explode");

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("ExplodeTrigger", explode);

        return animator;
    }
}