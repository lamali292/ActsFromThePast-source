using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Maw : CustomMonsterModel
{
    private const int NomDamage = 5;

    private const string ROAR = "ROAR";
    private const string SLAM = "SLAM";
    private const string DROOL = "DROOL";
    private const string NOMNOMNOM_SINGLE = "NOMNOMNOM_SINGLE";
    private const string NOMNOMNOM_MULTI = "NOMNOMNOM_MULTI";

    private static readonly LocString _roarDialog = L10NMonsterLookup("ACTSFROMTHEPAST-MAW.moves.ROAR.dialog");

    public override int MinInitialHp => 300;
    public override int MaxInitialHp => 300;

    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    private int StrUp => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    private int TerrifyDuration => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/maw/maw.tscn";

    private bool Roared
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    private int TurnCount
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    } = 1;

    private int NomHitCount => TurnCount / 2;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var roarState = new MoveState(
            ROAR,
            Roar, new DebuffIntent());
        var slamState = new MoveState(
            SLAM,
            Slam, new SingleAttackIntent(SlamDamage));
        var droolState = new MoveState(
            DROOL,
            Drool, new BuffIntent());
        var nomSingleState = new MoveState(
            NOMNOMNOM_SINGLE,
            NomNomNom,
            new SingleAttackIntent(NomDamage)
        );
        var nomMultiState = new MoveState(
            NOMNOMNOM_MULTI,
            NomNomNom,
            new DynamicMultiAttackIntent(() => NomDamage, () => NomHitCount)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        roarState.FollowUpState = moveBranch;
        slamState.FollowUpState = moveBranch;
        droolState.FollowUpState = moveBranch;
        nomSingleState.FollowUpState = moveBranch;
        nomMultiState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { roarState, slamState, droolState, nomSingleState, nomMultiState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        TurnCount++;

        if (!Roared)
            return ROAR;

        var num = rng.NextInt(100);
        var lastMoveWasNom = LastMove(stateMachine, NOMNOMNOM_SINGLE) || LastMove(stateMachine, NOMNOMNOM_MULTI);

        if (num < 50 && !lastMoveWasNom)
            return NomHitCount <= 1 ? NOMNOMNOM_SINGLE : NOMNOMNOM_MULTI;

        if (!LastMove(stateMachine, SLAM) && !lastMoveWasNom)
            return SLAM;

        return DROOL;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private async Task Roar(IReadOnlyList<Creature> targets)
    {
        AFTPModAudio.Play("maw", "maw_death", pitchVariation: 0.1f);
        TalkCmd.Play(_roarDialog, Creature, VfxColor.Blue, VfxDuration.Long);
        await Cmd.Wait(0.05f);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, TerrifyDuration, Creature, null);
            await PowerCmd.Apply<FrailPower>(new BlockingPlayerChoiceContext(), target, TerrifyDuration, Creature,
                null);
        }

        Roared = true;
    }

    private async Task Slam(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Drool(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, StrUp, Creature, null);
    }

    private async Task NomNomNom(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        var hits = Math.Max(1, NomHitCount);
        for (var i = 0; i < hits; i++)
        {
            var target = targets.FirstOrDefault(t => t.IsAlive);
            if (target != null)
            {
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var biteEffect = BiteEffect.Create(targetNode.VfxSpawnPosition);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(biteEffect);
                }
            }

            await DamageCmd.Attack(NomDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        AFTPModAudio.Play("maw", "maw_death");
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var hit = new AnimState("Hit");
        hit.NextState = idle;
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Hit", hit);
        return animator;
    }
}