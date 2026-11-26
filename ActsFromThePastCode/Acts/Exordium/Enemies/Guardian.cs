using ActsFromThePast.Compatibility;
using ActsFromThePast.Powers;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Guardian : CustomMonsterModel
{
    private const int WhirlwindDamage = 5;
    private const int WhirlwindCount = 4;
    private const int TwinSlamDamage = 8;
    private const int TwinSlamHits = 2;
    private const int DefensiveBlock = 20;
    private const int ChargeUpBlock = 9;
    private const int VentDebuffAmount = 2;

    // Dread: Vent Steam lasts a turn longer and Charge Up also gains Strength.
    private const int DreadChargeUpStrength = 2;
    private static int VentDebuff => VentDebuffAmount + (GameApiCompat.IsDread() ? 1 : 0);
    private const int DmgThresholdIncrease = 10;

    private const string CLOSE_UP = "CLOSE_UP";
    private const string FIERCE_BASH = "FIERCE_BASH";
    private const string ROLL_ATTACK = "ROLL_ATTACK";
    private const string TWIN_SLAM = "TWIN_SLAM";
    private const string WHIRLWIND = "WHIRLWIND";
    private const string CHARGE_UP = "CHARGE_UP";
    private const string VENT_STEAM = "VENT_STEAM";

    private static readonly LocString _destroyDialog =
        L10NMonsterLookup("ACTSFROMTHEPAST-GUARDIAN.moves.CHARGE_UP.dialog");

    private MoveState _closeUpState = null!;

    private bool _closeUpTriggered;

    private int _nextThreshold;

    private bool _pendingModeShift;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);
    private int FierceBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 36, 32);
    private int RollDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);
    private int SharpHideThorns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int DmgThresholdBase => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 30);
    public bool IsOpen { get; private set; } = true;

    public bool CloseUpTriggered
    {
        get => _closeUpTriggered;
        set
        {
            AssertMutable();
            _closeUpTriggered = value;
        }
    }

    public bool PendingModeShift
    {
        get => _pendingModeShift;
        set
        {
            AssertMutable();
            _pendingModeShift = value;
        }
    }

    public bool IsExecutingMove { get; private set; }

    protected override string VisualsPath => "res://ActsFromThePast/monsters/guardian/guardian.tscn";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _nextThreshold = DmgThresholdBase;
        await PowerCmd.Apply<ModeShiftPower>(new BlockingPlayerChoiceContext(), Creature, _nextThreshold, Creature,
            null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var chargeUpState = new MoveState(
            CHARGE_UP,
            ChargeUp, GameApiCompat.IsDread()
                ? new AbstractIntent[] { new DefendIntent(), new BuffIntent() }
                : new AbstractIntent[] { new DefendIntent() });
        var fierceBashState = new MoveState(
            FIERCE_BASH,
            FierceBash, new SingleAttackIntent(FierceBashDamage));
        var ventSteamState = new MoveState(
            VENT_STEAM,
            VentSteam, new DebuffIntent());
        var whirlwindState = new MoveState(
            WHIRLWIND,
            Whirlwind, new MultiAttackIntent(WhirlwindDamage, WhirlwindCount));
        _closeUpState = new MoveState(
            CLOSE_UP,
            CloseUp, new BuffIntent());
        var rollAttackState = new MoveState(
            ROLL_ATTACK,
            RollAttack, new SingleAttackIntent(RollDamage));
        var twinSlamState = new MoveState(
            TWIN_SLAM,
            TwinSlam, new MultiAttackIntent(TwinSlamDamage, TwinSlamHits), new BuffIntent());

        var offensiveBranch = new ConditionalBranchState("OFFENSIVE_BRANCH", SelectNextOffensiveMove);

        chargeUpState.FollowUpState = offensiveBranch;
        fierceBashState.FollowUpState = offensiveBranch;
        ventSteamState.FollowUpState = offensiveBranch;
        whirlwindState.FollowUpState = offensiveBranch;
        twinSlamState.FollowUpState = offensiveBranch;

        _closeUpState.FollowUpState = rollAttackState;
        rollAttackState.FollowUpState = twinSlamState;

        states.Add(chargeUpState);
        states.Add(fierceBashState);
        states.Add(ventSteamState);
        states.Add(whirlwindState);
        states.Add(_closeUpState);
        states.Add(rollAttackState);
        states.Add(twinSlamState);
        states.Add(offensiveBranch);

        return new MonsterMoveStateMachine(states, chargeUpState);
    }

    private string SelectNextOffensiveMove(Creature owner, Rng rng, MonsterMoveStateMachine sm)
    {
        if (!IsOpen)
            return CLOSE_UP;

        var last = sm.StateLog.LastOrDefault(s => s is MoveState)?.Id;
        return last switch
        {
            CHARGE_UP => FIERCE_BASH,
            FIERCE_BASH => VENT_STEAM,
            VENT_STEAM => WHIRLWIND,
            TWIN_SLAM => WHIRLWIND,
            WHIRLWIND => CHARGE_UP,
            _ => CHARGE_UP
        };
    }

    private async Task CheckPendingModeShift()
    {
        if (!_pendingModeShift)
            return;
        _pendingModeShift = false;
        CloseUpTriggered = true;
        await TransitionToDefensiveMode(false);
    }

    private async Task ChargeUp(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, ChargeUpBlock, ValueProp.Move, null);
        if (GameApiCompat.IsDread())
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, DreadChargeUpStrength,
                Creature, null);
        AFTPModAudio.Play("guardian", "guardian_destroy");
        TalkCmd.Play(_destroyDialog, Creature, VfxColor.Gold, VfxDuration.VeryLong);
        await CheckPendingModeShift();
    }

    private async Task FierceBash(IReadOnlyList<Creature> targets)
    {
        IsExecutingMove = true;
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(FierceBashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
        IsExecutingMove = false;
        await CheckPendingModeShift();
    }

    private async Task VentSteam(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, VentDebuff, Creature,
                null);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), target, VentDebuff, Creature,
                null);
        }

        await CheckPendingModeShift();
    }

    private async Task Whirlwind(IReadOnlyList<Creature> targets)
    {
        IsExecutingMove = true;
        await FastAttackAnimation.Play(Creature);
        AFTPModAudio.Play("general", "whirlwind");

        await DamageCmd.Attack(WhirlwindDamage)
            .FromMonster(this)
            .WithHitCount(WhirlwindCount)
            .AfterAttackerAnim(async () =>
            {
                AFTPModAudio.Play("general", "attack_heavy");

                var target = targets.FirstOrDefault(t => t.IsAlive);
                if (target != null)
                {
                    var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                    if (targetNode != null)
                    {
                        var cleaveVfx = CleaveEffect.Create(targetNode.VfxSpawnPosition);
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(cleaveVfx);
                    }
                }

                await Cmd.Wait(0.15f);
            })
            .Execute(null);

        IsExecutingMove = false;
        await CheckPendingModeShift();
    }

    private async Task CloseUp(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<SharpHidePower>(new BlockingPlayerChoiceContext(), Creature, SharpHideThorns, Creature,
            null);
    }

    private async Task RollAttack(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(RollDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/punch_construct/punch_construct_attack_single")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task TwinSlam(IReadOnlyList<Creature> targets)
    {
        IsExecutingMove = true;
        await TransitionToOffensiveMode();
        await DamageCmd.Attack(TwinSlamDamage)
            .WithHitCount(TwinSlamHits)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/punch_construct/punch_construct_attack_double")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Remove<SharpHidePower>(Creature);
        IsExecutingMove = false;
        await CheckPendingModeShift();
    }

    public async Task TransitionToDefensiveMode(bool setMove = true)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var vfx = IntenseZoomEffect.Create(creatureNode.VfxSpawnPosition);
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
        }

        await PowerCmd.Remove<ModeShiftPower>(Creature);
        _nextThreshold += DmgThresholdIncrease;
        AFTPModAudio.Play("guardian", "guardian_boss_transform");
        await CreatureCmd.GainBlock(Creature, DefensiveBlock, ValueProp.Move, null);

        await CreatureCmd.TriggerAnim(Creature, "transition", 0.0f);

        var spineBody = creatureNode?.Visuals.SpineBody;

        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var trackEntry = animState.GetCurrent(0);
            if (trackEntry != null)
            {
                trackEntry.SetTimeScale(2.0f);
                var duration = trackEntry.GetAnimationEnd() / 2.0f;
                await Cmd.Wait(duration);
            }

            animState.SetAnimation("defensive");
        }

        IsOpen = false;
        if (setMove)
            SetMoveImmediate(_closeUpState, true);
    }

    private async Task TransitionToOffensiveMode()
    {
        await PowerCmd.Apply<ModeShiftPower>(new BlockingPlayerChoiceContext(), Creature, _nextThreshold, Creature,
            null);
        if (Creature.Block > 0)
            await CreatureCmd.LoseBlock(
                new BlockingPlayerChoiceContext(),
                Creature,
                Creature.Block,
                null);

        await CreatureCmd.TriggerAnim(Creature, "idle", 0.0f);

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;

        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            var trackEntry = animState.GetCurrent(0);
            trackEntry?.SetMixDuration(0.2f);
        }

        IsOpen = true;
        _closeUpTriggered = false;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        if (creature == Creature)
        {
            var sharpHide = Creature.GetPower<SharpHidePower>();
            if (sharpHide is { AttackInProgress: true, AttackSource.IsAlive: true })
                await GameApiCallCompat.Damage(
                    new BlockingPlayerChoiceContext(),
                    sharpHide.AttackSource,
                    sharpHide.Amount,
                    DamageProps.nonCardUnpowered,
                    null);
        }

        await base.BeforeDeath(creature);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true)
        {
            BoundsContainer = "IdleBounds"
        };

        var defensive = new AnimState("defensive", true)
        {
            BoundsContainer = "DefensiveBounds"
        };

        var transition = new AnimState("transition");

        transition.NextState = defensive;
        idle.AddBranch("transition", transition);
        idle.AddBranch("defensive", defensive);
        defensive.AddBranch("idle", idle);

        // A hit plays on top of the current mode and has to answer the same triggers as the idle state it stands in for.
        var hit = new AnimState("Hit") { BoundsContainer = "IdleBounds" };
        var hitDefensive = new AnimState("HitDefensive") { BoundsContainer = "DefensiveBounds" };
        hit.NextState = idle;
        hitDefensive.NextState = defensive;

        hit.AddBranch("transition", transition);
        hit.AddBranch("defensive", defensive);
        hitDefensive.AddBranch("idle", idle);

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Hit", hitDefensive, () => !IsOpen);
        animator.AddAnyState("Hit", hit);
        return animator;
    }
}