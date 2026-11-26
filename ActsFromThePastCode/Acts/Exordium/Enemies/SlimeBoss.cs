using ActsFromThePast.Compatibility;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class SlimeBoss : CustomMonsterModel
{
    private const string SLAM = "SLAM";
    private const string PREP_SLAM = "PREP_SLAM";
    private const string SPLIT = "SPLIT";
    private const string GOOP_SPRAY = "GOOP_SPRAY";

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);

    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 35);
    // Dread: Goop Spray also shuffles Slimed into the Draw Pile (the split slimes inherit the extra HP).
    private static int DrawPileSlimedCount => GameApiCompat.IsDread() ? 2 : 0;
    private int SlimedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/slime_boss/slime_boss.tscn";
    public override bool HasDeathSfx => false;
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    public bool SplitTriggered
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    public MoveState SplitState { get; private set; } = null!;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SplitPower>(new BlockingPlayerChoiceContext(), Creature, 1m, Creature, null);
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        AFTPModAudio.Play("slime_boss", "death");
        //  NGame.Instance?.ScreenShake(ShakeStrength.TooMuch, ShakeDuration.Long);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var goopSprayState = new MoveState(
            GOOP_SPRAY,
            GoopSpray, new StatusIntent(SlimedCount + DrawPileSlimedCount));

        var prepSlamState = new MoveState(
            PREP_SLAM,
            PrepSlam, new UnknownIntent());

        var slamState = new MoveState(
            SLAM,
            Slam, new SingleAttackIntent(SlamDamage));

        SplitState = new MoveState(
            SPLIT,
            Split, new UnknownIntent());

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        goopSprayState.FollowUpState = prepSlamState;
        prepSlamState.FollowUpState = slamState;
        slamState.FollowUpState = moveBranch;
        SplitState.FollowUpState = SplitState;

        states.Add(goopSprayState);
        states.Add(prepSlamState);
        states.Add(slamState);
        states.Add(SplitState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, goopSprayState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (SplitTriggered) return SPLIT;
        return GOOP_SPRAY;
    }

    private async Task GoopSpray(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);
        AFTPModAudio.Play("general", "slime_attack");

        try
        {
            ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
            if (DrawPileSlimedCount > 0)
                await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Draw, DrawPileSlimedCount, null,
                    CardPilePosition.Random);
        }
        finally
        {
            ClassicSlimedTracker.CreatingClassicSlimed = false;
        }
    }

    private async Task PrepSlam(IReadOnlyList<Creature> targets)
    {
        PlayPrepSfx();
        TalkCmd.Play(L10NMonsterLookup("ACTSFROMTHEPAST-SLIME_BOSS.moves.PREP_SLAM.banter"), Creature, VfxColor.Green,
            VfxDuration.Long);
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Long);
        await Cmd.Wait(0.3f);
    }

    private async Task Slam(IReadOnlyList<Creature> targets)
    {
        await JumpAnimation.Play(Creature);

        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var creatureNode = target.GetCreatureNode();
            if (creatureNode == null) continue;

            var vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_heavy_blunt"))
                .Instantiate<Node2D>();
            vfx.Modulate = Colors.Green;
            target.GetVfxContainer()?.AddChildSafely(vfx);
            vfx.GlobalPosition = creatureNode.GlobalPosition;
        }

        await Cmd.Wait(0.4f);

        await DamageCmd.Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Split(IReadOnlyList<Creature> targets)
    {
        var currentHp = Creature.CurrentHp;
        var combatState = Creature.CombatState;
        if (combatState == null) return;
        var originalPosition = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Position ?? Vector2.Zero;

        _ = ShakeAnimation.Play(Creature, 1.0f, 3.0f);
        await Cmd.Wait(1.0f);
        AFTPModAudio.Play("general", "slime_split");
        await CreatureCmd.Kill(Creature);

        var occupiedSlots = combatState.GetTeammatesOf(Creature)
            .Where(t => t.IsAlive)
            .Select(t => t.SlotName)
            .ToHashSet();

        var spikeSlot = combatState.Encounter?.Slots?
            .FirstOrDefault(s => s.StartsWith("spike_large") && !occupiedSlots.Contains(s));

        var acidSlot = combatState.Encounter?.Slots?
            .FirstOrDefault(s => s.StartsWith("acid_large") && !occupiedSlots.Contains(s));

        var useSlots = spikeSlot != null && acidSlot != null;

        Queue<Vector2>? positionQueue = null;
        var enemyContainer = NCombatRoom.Instance?.GetNode<Control>("%EnemyContainer");
        Callable? callable = null;

        if (!useSlots)
        {
            positionQueue = new Queue<Vector2>();

            void OnChildEntered(Node child)
            {
                if (child is NCreature nc && positionQueue.Count > 0)
                    nc.Position = positionQueue.Dequeue();
            }

            callable = Callable.From<Node>(OnChildEntered);
            enemyContainer?.Connect(Node.SignalName.ChildEnteredTree, callable.Value);
            positionQueue.Enqueue(originalPosition + new Vector2(-385f, 20f));
        }

        var spikeSlime = (SpikeSlimeLarge)ModelDb.Monster<SpikeSlimeLarge>().ToMutable();
        spikeSlime.OverrideHp = currentHp;
        var spikeCreature = await CreatureCmd.Add(spikeSlime, combatState, CombatSide.Enemy, spikeSlot);
        await CreatureCmd.SetMaxHp(spikeCreature, currentHp);
        await CreatureCmd.Heal(spikeCreature, currentHp);

        if (!useSlots)
            positionQueue!.Enqueue(originalPosition + new Vector2(120f, 20f));

        var acidSlime = (AcidSlimeLarge)ModelDb.Monster<AcidSlimeLarge>().ToMutable();
        acidSlime.OverrideHp = currentHp;
        var acidCreature = await CreatureCmd.Add(acidSlime, combatState, CombatSide.Enemy, acidSlot);
        await CreatureCmd.SetMaxHp(acidCreature, currentHp);
        await CreatureCmd.Heal(acidCreature, currentHp);

        if (!useSlots && callable.HasValue)
            enemyContainer?.Disconnect(Node.SignalName.ChildEnteredTree, callable.Value);
    }

    private void PlayPrepSfx()
    {
        var roll = Rng.Chaotic.NextInt(2);
        var sfxName = roll switch
        {
            0 => "slime_boss_talk_1",
            _ => "slime_boss_talk_2"
        };
        AFTPModAudio.Play("slime_boss", sfxName);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        return animator;
    }
}