using ActsFromThePast.Compatibility;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class TimeEater : CustomMonsterModel
{
    private const int ReverbHits = 3;
    private const int RippleBlock = 20;
    // Dread: the Ripple debuffs last 2 turns, Head Slam adds 3 Slimed and Haste also gains Strength.
    private static int DebuffTurns => GameApiCompat.IsDread() ? 2 : 1;
    private static int SlimedCount => GameApiCompat.IsDread() ? 3 : 2;
    private static int HasteStrength => GameApiCompat.IsDread() ? 3 : 0;

    private const string REVERBERATE = "REVERBERATE";
    private const string RIPPLE = "RIPPLE";
    private const string HEAD_SLAM = "HEAD_SLAM";
    private const string HASTE = "HASTE";

    private static readonly LocString _hasteDialog = L10NMonsterLookup("ACTSFROMTHEPAST-TIME_EATER.banter.haste");
    private static readonly LocString _introDialog = L10NMonsterLookup("ACTSFROMTHEPAST-TIME_EATER.banter.intro");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);

    private int ReverbDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int HeadSlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 26);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/time_eater/time_eater.tscn";

    private bool UsedHaste
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    private bool FirstTurn
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    } = true;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<TimeWarpPower>(new BlockingPlayerChoiceContext(), Creature, 1M, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var reverberateState = new MoveState(
            REVERBERATE,
            Reverberate, new MultiAttackIntent(ReverbDamage, ReverbHits));
        var rippleState = new MoveState(
            RIPPLE,
            Ripple, new DefendIntent(), new DebuffIntent());
        var headSlamState = new MoveState(
            HEAD_SLAM,
            HeadSlam, new SingleAttackIntent(HeadSlamDamage), new DebuffIntent(), new StatusIntent(SlimedCount));
        var hasteState = new MoveState(
            HASTE,
            Haste, new BuffIntent());

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        reverberateState.FollowUpState = moveBranch;
        rippleState.FollowUpState = moveBranch;
        headSlamState.FollowUpState = moveBranch;
        hasteState.FollowUpState = moveBranch;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { reverberateState, rippleState, headSlamState, hasteState, moveBranch },
            moveBranch
        );
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (owner.CurrentHp < owner.MaxHp / 2 && !UsedHaste)
        {
            UsedHaste = true;
            return HASTE;
        }

        var num = rng.NextInt(100);

        if (num < 45)
        {
            if (!LastTwoMoves(stateMachine, REVERBERATE))
                return REVERBERATE;
            // Reroll into 50-99
            num = 50 + rng.NextInt(50);
        }

        if (num < 80)
        {
            if (!LastMove(stateMachine, HEAD_SLAM))
                return HEAD_SLAM;
            return rng.NextFloat() < 0.66f ? REVERBERATE : RIPPLE;
        }

        if (!LastMove(stateMachine, RIPPLE))
            return RIPPLE;
        // Reroll into 0-74
        num = rng.NextInt(75);
        if (num < 45)
            if (!LastTwoMoves(stateMachine, REVERBERATE))
                return REVERBERATE;
        if (!LastMove(stateMachine, HEAD_SLAM))
            return HEAD_SLAM;
        return rng.NextFloat() < 0.66f ? REVERBERATE : RIPPLE;
    }

    private static bool LastMove(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count == 0) return false;
        return log[log.Count - 1].Id == moveId;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[log.Count - 1].Id == moveId && log[log.Count - 2].Id == moveId;
    }

    private async Task PlayIntroIfFirstTurn()
    {
        if (!FirstTurn)
            return;
        FirstTurn = false;
        TalkCmd.Play(_introDialog, Creature, VfxColor.Purple, VfxDuration.VeryLong);
        await Cmd.Wait(0.5f);
    }

    private async Task Reverberate(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        await DamageCmd.Attack(ReverbDamage)
            .FromMonster(this)
            .WithHitCount(ReverbHits)
            .AfterAttackerAnim(async () =>
            {
                var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
                if (creatureNode != null)
                    ShockWaveEffect.PlayRoyal(creatureNode.VfxSpawnPosition);
                await Cmd.Wait(0.75f);
            })
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Ripple(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        await CreatureCmd.GainBlock(Creature, RippleBlock, ValueProp.Move, null);
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), target, DebuffTurns, Creature,
                null);
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, DebuffTurns, Creature, null);
            await PowerCmd.Apply<FrailPower>(new BlockingPlayerChoiceContext(), target, DebuffTurns, Creature, null);
        }
    }

    private async Task HeadSlam(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        await CreatureCmd.TriggerAnim(Creature, "Slam", 0.4f);
        await DamageCmd.Attack(HeadSlamDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/twig_slime_s/twig_slime_s_attack")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        foreach (var target in targets.Where(t => t.IsAlive))
            await PowerCmd.Apply<DrawReductionPower>(new BlockingPlayerChoiceContext(), target, 1, Creature, null);
        try
        {
            ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
        }
        finally
        {
            ClassicSlimedTracker.CreatingClassicSlimed = false;
        }
    }

    private async Task Haste(IReadOnlyList<Creature> targets)
    {
        await PlayIntroIfFirstTurn();
        TalkCmd.Play(_hasteDialog, Creature, VfxColor.Purple, VfxDuration.VeryLong);

        var debuffs = Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        foreach (var debuff in debuffs)
            await PowerCmd.Remove(debuff);

        var healAmount = Creature.MaxHp / 2 - Creature.CurrentHp;
        if (healAmount > 0)
            await CreatureCmd.Heal(Creature, healAmount);

        await CreatureCmd.GainBlock(Creature, HeadSlamDamage, ValueProp.Move, null);
        if (HasteStrength > 0)
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, HasteStrength, Creature,
                null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Slam", attack);
        animator.AddAnyState("Hit", hit);
        controller.GetAnimationState().SetTimeScale(0.8f);

        return animator;
    }
}