using System.Reflection;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class Byrd : CustomMonsterModel
{
    private const int HeadbuttDamage = 3;
    private const int CawStrength = 1;

    private const string FlyingVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_flying.tscn";
    private const string GroundedVisualsPath = "res://ActsFromThePast/monsters/byrd/byrd_grounded.tscn";

    private const string PECK = "PECK";
    private const string SWOOP = "SWOOP";
    private const string CAW = "CAW";
    private const string GO_AIRBORNE = "GO_AIRBORNE";
    private const string HEADBUTT = "HEADBUTT";
    private static readonly LocString _cawLine = L10NMonsterLookup("ACTSFROMTHEPAST-BYRD.moves.CAW.speakLine");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 33, 31);

    private int PeckDamage => 1;
    private int PeckCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private int SwoopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int FlightAmountPerPlayer => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 4, 3);

    private int FlightAmount
    {
        get
        {
            var additionalPlayers = (Creature.CombatState?.Players.Count ?? 1) - 1;
            return FlightAmountPerPlayer + additionalPlayers * 2;
        }
    }

    protected override string VisualsPath => FlyingVisualsPath;

    public override IEnumerable<string> AssetPaths
    {
        get
        {
            var paths = new List<string>(base.AssetPaths) { GroundedVisualsPath };
            return paths;
        }
    }

    private bool IsFlying
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
        await PowerCmd.Apply<FlightPower>(new BlockingPlayerChoiceContext(), Creature, FlightAmount, Creature, null);
    }

    public async Task OnFlightBroken()
    {
        IsFlying = false;
        await ByrdFallAnimation.Play(Creature, 100f);
        SwapVisuals(GroundedVisualsPath);

        // Squash the grounded visuals on impact
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var visuals = creatureNode.Visuals;
            var baseScale = visuals.Scale;
            var squashTween = creatureNode.CreateTween();
            squashTween.TweenProperty(visuals, "scale",
                new Vector2(baseScale.X * 1.2f, baseScale.Y * 0.8f), ByrdFallAnimation.SquashDuration * 0.5f);
            squashTween.TweenProperty(visuals, "scale",
                baseScale, ByrdFallAnimation.SquashDuration * 0.5f);
            await creatureNode.ToSignal(squashTween, Tween.SignalName.Finished);
        }

        await CreatureCmd.Stun(Creature, HEADBUTT);
    }

    private void SwapVisuals(string newVisualsPath)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null)
            return;

        var oldVisuals = creatureNode.Visuals;
        var newVisuals = PreloadManager.Cache.GetScene(newVisualsPath).Instantiate<NCreatureVisuals>();

        creatureNode.Visuals = newVisuals;

        creatureNode.AddChildSafely(newVisuals);
        creatureNode.MoveChild(newVisuals, 0);
        newVisuals.Position = Vector2.Zero;

        // Re-initialize the spine animator
        if (newVisuals is { HasSpineAnimation: true, SpineBody: not null })
        {
            var animator = GenerateAnimatorForState(newVisuals.SpineBody);
            creatureNode._spineAnimator = animator;
        }

        // Update bounds to match new visuals
        creatureNode.UpdateBounds(newVisuals);

        oldVisuals.QueueFree();
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;
        AFTPModAudio.Play("byrd", "byrd_death");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var peckState = new MoveState(
            PECK,
            Peck, new MultiAttackIntent(PeckDamage, PeckCount));

        var swoopState = new MoveState(
            SWOOP,
            Swoop, new SingleAttackIntent(SwoopDamage));

        var cawState = new MoveState(
            CAW,
            Caw, new BuffIntent());

        var goAirborneState = new MoveState(
            GO_AIRBORNE,
            GoAirborne, new BuffIntent());

        var headbuttState = new MoveState(
            HEADBUTT,
            Headbutt, new SingleAttackIntent(HeadbuttDamage));

        var flyingBranch = new ConditionalBranchState("FLYING_BRANCH", SelectFlyingMove);
        var firstMoveBranch = new ConditionalBranchState("FIRST_MOVE_BRANCH", SelectFirstMove);

        // Flying moves all loop back to flying branch
        peckState.FollowUpState = flyingBranch;
        swoopState.FollowUpState = flyingBranch;
        cawState.FollowUpState = flyingBranch;

        // Grounded sequence: headbutt -> go airborne -> back to flying
        headbuttState.FollowUpState = goAirborneState;
        goAirborneState.FollowUpState = flyingBranch;

        states.Add(peckState);
        states.Add(swoopState);
        states.Add(cawState);
        states.Add(goAirborneState);
        states.Add(headbuttState);
        states.Add(flyingBranch);
        states.Add(firstMoveBranch);

        return new MonsterMoveStateMachine(states, firstMoveBranch);
    }

    private string SelectFirstMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        return rng.NextFloat() < 0.375f ? CAW : PECK;
    }

    private string SelectFlyingMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var num = rng.NextInt(100);

        if (num < 50)
        {
            if (LastTwoMoves(stateMachine, PECK))
                return rng.NextFloat() < 0.4f ? SWOOP : CAW;
            return PECK;
        }

        if (num < 70)
        {
            if (LastMove(stateMachine, SWOOP))
                return rng.NextFloat() < 0.375f ? CAW : PECK;
            return SWOOP;
        }

        if (LastMove(stateMachine, CAW))
            return rng.NextFloat() < 0.2857f ? SWOOP : PECK;
        return CAW;
    }

    private async Task Peck(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(PeckDamage)
            .FromMonster(this)
            .WithHitCount(PeckCount)
            .AfterAttackerAnim(() =>
            {
                PlayRandomBirdSfx();
                return Task.CompletedTask;
            })
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Swoop(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(SwoopDamage)
            .FromMonster(this)
            .WithHitFx(tmpSfx: "slash_attack.mp3")
            .WithHitVfxNode(target =>
            {
                var vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_scratch"))
                    .Instantiate<Node2D>();
                vfx.Scale = new Vector2(-1f, 1f);
                vfx.GlobalPosition = NCombatRoom.Instance?.GetCreatureNode(target)?.VfxSpawnPosition ?? Vector2.Zero;
                return vfx;
            })
            .Execute(null);
    }

    private async Task Caw(IReadOnlyList<Creature> targets)
    {
        AFTPModAudio.Play("byrd", "byrd_death");
        TalkCmd.Play(_cawLine, Creature, VfxColor.Swamp, VfxDuration.Short);
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, CawStrength, Creature, null);
    }

    private async Task GoAirborne(IReadOnlyList<Creature> targets)
    {
        IsFlying = true;
        SwapVisuals(FlyingVisualsPath);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var visuals = creatureNode.Visuals;
            visuals.Position = new Vector2(visuals.Position.X, visuals.Position.Y + 80f);

            // Rise up past the target
            await RiseAnimation.Play(Creature, 100f);

            // Settle back to correct position
            var settleTween = creatureNode.CreateTween();
            settleTween.TweenProperty(visuals, "position:y",
                    0f, 0.2f)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            await creatureNode.ToSignal(settleTween, Tween.SignalName.Finished);
        }

        AFTPModAudio.Play("byrd", "flight");
        await PowerCmd.Apply<FlightPower>(new BlockingPlayerChoiceContext(), Creature, FlightAmount, Creature, null);
    }

    private async Task Headbutt(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(HeadbuttDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private void PlayRandomBirdSfx()
    {
        var roll = Rng.Chaotic.NextInt(6) + 1;
        AFTPModAudio.Play("byrd", $"byrd_talk_{roll}");
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

    // TODO investigate making head lift animation intermittently play during grounded non-stunned idle state

    private CreatureAnimator GenerateAnimatorForState(MegaSprite controller)
    {
        CreatureAnimator animator;

        if (IsFlying)
        {
            var idle = new AnimState("idle_flap", true);
            var hit = new AnimState("Hit");
            hit.NextState = idle;
            animator = new CreatureAnimator(idle, controller);
            animator.AddAnyState("Dead", new AnimState("Dead"));
            animator.AddAnyState("Hit", hit);
        }
        else
        {
            var idle = new AnimState("idle", true);
            var headLift = new AnimState("head_lift");
            var hit = new AnimState("Hit");
            headLift.NextState = idle;
            hit.NextState = idle;
            animator = new CreatureAnimator(idle, controller);
            animator.AddAnyState("Dead", new AnimState("Dead"));
            animator.AddAnyState("Attack", headLift);
            animator.AddAnyState("Hit", hit);
        }

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current?.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        if (controller.GetSkeleton() is { } skeleton) animState.Apply(skeleton);

        return animator;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        return GenerateAnimatorForState(controller);
    }
}