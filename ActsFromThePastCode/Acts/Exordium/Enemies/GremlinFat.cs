using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class GremlinFat : CustomMonsterModel
{
    private const int WeakAmount = 1;
    private const int FrailAmount = 1;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 13);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private bool AppliesFrail => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);


    protected override string VisualsPath => "res://ActsFromThePast/monsters/gremlin_fat/gremlin_fat.tscn";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var smashState = new MoveState(
            "SMASH",
            Smash, new SingleAttackIntent(SmashDamage), new DebuffIntent());

        smashState.FollowUpState = smashState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { smashState },
            smashState
        );
    }

    private async Task Smash(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(SmashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, WeakAmount, Creature, null);
            if (AppliesFrail)
                await PowerCmd.Apply<FrailPower>(new BlockingPlayerChoiceContext(), target, FrailAmount, Creature,
                    null);
        }
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
            0 => "gremlin_fat_death_1",
            1 => "gremlin_fat_death_2",
            _ => "gremlin_fat_death_3"
        };
        AFTPModAudio.Play("gremlin_fat", sfxName);
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