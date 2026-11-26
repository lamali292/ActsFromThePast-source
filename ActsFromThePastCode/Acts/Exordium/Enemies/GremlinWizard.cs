using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinWizard : CustomMonsterModel
{
    private const int ChargeLimit = 3;

    private const string CHARGING = "CHARGING";
    private const string ULTIMATE_BLAST = "ULTIMATE_BLAST";

    private static readonly LocString _chargingDialog =
        L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.CHARGING.dialog");

    private static readonly LocString _ultimateDialog =
        L10NMonsterLookup("ACTSFROMTHEPAST-GREMLIN_WIZARD.moves.ULTIMATE_BLAST.dialog");

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);

    private int UltimateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_wizard/gremlin_wizard.tscn";

    private int CurrentCharge
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    } = 1;


    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var chargingState = new MoveState(
            CHARGING,
            Charging, new UnknownIntent());

        var ultimateBlastState = new MoveState(
            ULTIMATE_BLAST,
            UltimateBlast, new SingleAttackIntent(UltimateDamage));

        // After charging, check if ready to blast
        chargingState.FollowUpState = new ConditionalBranchState("AFTER_CHARGE", SelectAfterCharge);

        // After blast, just blast again (A17 behavior as default)
        ultimateBlastState.FollowUpState = ultimateBlastState;

        states.Add(chargingState);
        states.Add(ultimateBlastState);
        states.Add(chargingState.FollowUpState);

        return new MonsterMoveStateMachine(states, chargingState);
    }

    private string SelectAfterCharge(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        return CurrentCharge >= ChargeLimit ? ULTIMATE_BLAST : CHARGING;
    }

    private Task Charging(IReadOnlyList<Creature> targets)
    {
        CurrentCharge++;
        if (CurrentCharge >= ChargeLimit)
        {
            PlayRandomChargeSfx();
            TalkCmd.Play(_ultimateDialog, Creature, VfxColor.Purple, VfxDuration.Long);
        }

        return Task.CompletedTask;
    }

    private async Task UltimateBlast(IReadOnlyList<Creature> targets)
    {
        CurrentCharge = 0;

        await DamageCmd.Attack(UltimateDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_fire_burst", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private void PlayRandomChargeSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "gremlin_wizard_talk_1",
            _ => "gremlin_wizard_talk_2"
        };
        AFTPModAudio.Play("gremlin_wizard", sfxName);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
        GremlinLeaderHelper.SubscribeToLeaderDeath(Creature, (CombatState)CombatState);
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "gremlin_wizard_death_1",
            1 => "gremlin_wizard_death_2",
            _ => "gremlin_wizard_death_3"
        };
        AFTPModAudio.Play("gremlin_wizard", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("animation", true);
        var hit = new AnimState("Hit");
        hit.NextState = idle;
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Hit", hit);

        var animState = controller.GetAnimationState();
        var current = animState.GetCurrent(0);
        current?.SetTrackTime(Rng.Chaotic.NextFloat(current.GetAnimationEnd()));
        animState.Update(0.0f);
        if (controller.GetSkeleton() is { } skeleton) animState.Apply(skeleton);

        return animator;
    }
}