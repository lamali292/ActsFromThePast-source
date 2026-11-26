using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Looter : CustomMonsterModel
{
    private const int EscapeBlock = 6;

    private const string MUG = "MUG";
    private const string SMOKE_BOMB = "SMOKE_BOMB";
    private const string ESCAPE = "ESCAPE";
    private const string LUNGE = "LUNGE";
    private bool _hasSpoken;

    private int _mugCount;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    private int MugDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int GoldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 15);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/looter/looter.tscn";
    public override bool HasDeathSfx => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        foreach (var player in Creature.CombatState?.Players ?? [])
        {
            var thievery = (ThieveryPower)ModelDb.Power<ThieveryPower>().ToMutable();
            thievery.Target = player.Creature;
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), thievery, Creature, GoldAmount, Creature, null);
        }

        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;

        if (Creature.CombatState?.RunState.CurrentRoom is CombatRoom currentRoom)
            foreach (var thievery in Creature.GetPowerInstances<ThieveryPower>())
            {
                var stolenGold = thievery.DynamicVars.Gold.IntValue;
                var victim = thievery.Target?.Player;
                if (stolenGold > 0 && victim != null)
                    currentRoom.AddExtraReward(victim, new GoldReward(stolenGold, victim, true));
            }

        if (Rng.Chaotic.NextFloat() < 0.3f)
            TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.deathLine"), Creature, VfxColor.Blue,
                VfxDuration.Long);

        PlayRandomDeathSfx();
    }

    private void PlayRandomDeathSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "looter_death_1",
            1 => "looter_death_2",
            _ => "looter_death_3"
        };
        AFTPModAudio.Play("looter", sfxName);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var mugState = new MoveState(
            MUG,
            Mug, new SingleAttackIntent(MugDamage));

        var smokeBombState = new MoveState(
            SMOKE_BOMB,
            SmokeBomb, new DefendIntent());

        var escapeState = new MoveState(
            ESCAPE,
            Escape, new EscapeIntent());

        var lungeState = new MoveState(
            LUNGE,
            Lunge, new SingleAttackIntent(LungeDamage));

        var afterSecondMugBranch = new RandomBranchState("AFTER_SECOND_MUG");
        afterSecondMugBranch.AddBranch(smokeBombState, MoveRepeatType.UseOnlyOnce, 50f);
        afterSecondMugBranch.AddBranch(lungeState, MoveRepeatType.UseOnlyOnce, 50f);

        var mugBranch = new ConditionalBranchState("MUG_BRANCH", SelectAfterMug);

        mugState.FollowUpState = mugBranch;
        smokeBombState.FollowUpState = escapeState;
        escapeState.FollowUpState = escapeState;
        lungeState.FollowUpState = smokeBombState;

        states.Add(mugState);
        states.Add(smokeBombState);
        states.Add(escapeState);
        states.Add(lungeState);
        states.Add(afterSecondMugBranch);
        states.Add(mugBranch);

        return new MonsterMoveStateMachine(states, mugState);
    }

    private string SelectAfterMug(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (_mugCount < 2) return MUG;
        return "AFTER_SECOND_MUG";
    }

    private async Task Mug(IReadOnlyList<Creature> targets)
    {
        if (!_hasSpoken && Rng.Chaotic.NextFloat() < 0.6f)
        {
            _hasSpoken = true;
            TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.MUG.banter"), Creature, VfxColor.Blue,
                VfxDuration.Long);
        }

        PlayAttackSfx();
        await FastAttackAnimation.Play(Creature);

        VfxCmd.PlayOnCreatureCenters(targets, "vfx/vfx_coin_explosion_regular");

        await DamageCmd.Attack(MugDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await StealGold();

        _mugCount++;
    }

    private async Task SmokeBomb(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.SMOKE_BOMB.banter"), Creature, VfxColor.Blue,
            VfxDuration.Long);

        await CreatureCmd.GainBlock(Creature, EscapeBlock, ValueProp.Move, null);
    }

    private async Task Escape(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-LOOTER.moves.ESCAPE.banter"), Creature, VfxColor.Blue,
            VfxDuration.Standard);

        var entry = Creature.CombatState?.RunState.CurrentMapPointHistoryEntry;
        foreach (var thievery in Creature.GetPowerInstances<ThieveryPower>())
        {
            var stolen = thievery.DynamicVars.Gold.IntValue;
            var victim = thievery.Target?.Player;
            if (stolen > 0 && victim != null)
                entry?.GetEntry(victim.NetId)?.MarkLootStolen(stolen);
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var effect = SmokeBombEffect.Create(creatureNode.VfxSpawnPosition);
            NCombatRoom.Instance?.CombatVfxContainer.AddChild(effect);
        }

        creatureNode?.SetInteractableCompat(false);

        await EscapeAnimation.Play(Creature);
        await CreatureCmd.Escape(Creature);
    }

    private async Task Lunge(IReadOnlyList<Creature> targets)
    {
        PlayAttackSfx();
        await FastAttackAnimation.Play(Creature);

        VfxCmd.PlayOnCreatureCenters(targets, "vfx/vfx_coin_explosion_regular");

        await DamageCmd.Attack(LungeDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await StealGold();

        _mugCount++;
    }

    private async Task StealGold()
    {
        foreach (var thievery in Creature.GetPowerInstances<ThieveryPower>())
        {
            var target = thievery.Target;
            if (target == null || target.IsDead || target.Player is not { Gold: > 0 })
                continue;

            var amount = Math.Min(thievery.Amount, target.Player.Gold);
            await PlayerCmd.LoseGold(amount, target.Player);

            var gold = thievery.DynamicVars.Gold;
            gold.BaseValue += amount;
        }
    }

    private void PlayAttackSfx()
    {
        var roll = Rng.Chaotic.NextInt(3);
        var sfxName = roll switch
        {
            0 => "looter_talk_1",
            1 => "looter_talk_2",
            _ => "looter_talk_3"
        };
        AFTPModAudio.Play("looter", sfxName);
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