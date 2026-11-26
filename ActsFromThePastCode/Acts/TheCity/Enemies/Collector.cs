using ActsFromThePast.Compatibility;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Collector : CustomMonsterModel
{
    private const string SPAWN = "SPAWN";
    private const string FIREBALL = "FIREBALL";
    private const string BUFF = "BUFF";
    private const string MEGA_DEBUFF = "MEGA_DEBUFF";
    private const string REVIVE = "REVIVE";

    private const float FireInterval = 0.07f;

    private static readonly LocString _megaDebuffDialog =
        L10NMonsterLookup("ACTSFROMTHEPAST-COLLECTOR.moves.MEGA_DEBUFF.dialog");

    private bool _alive = true;
    private bool _initialSpawn;

    private int _turnsTaken;
    private bool _ultUsed;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 282);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 282);

    private int FireballDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 15);
    // Dread: revives Torch Heads twice as often and its Buff also gives them Block.
    private static int ReviveChance => GameApiCompat.IsDread() ? 50 : 25;
    private int MegaDebuffAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/collector/collector.tscn";

    private int TurnsTaken
    {
        get => _turnsTaken;
        set
        {
            AssertMutable();
            _turnsTaken = value;
        }
    }

    private bool UltUsed
    {
        get => _ultUsed;
        set
        {
            AssertMutable();
            _ultUsed = value;
        }
    }

    private bool InitialSpawn
    {
        get => _initialSpawn;
        set
        {
            AssertMutable();
            _initialSpawn = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _turnsTaken = 0;
        _ultUsed = false;
        _initialSpawn = true;
        _alive = true;
        Creature.Died += OnCollectorDeath;

        StartFireLoop();
    }

    private void OnCollectorDeath(Creature _)
    {
        Creature.Died -= OnCollectorDeath;
        _alive = false;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature)
            return;

        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Long);

        var livingMinions = CombatState.GetTeammatesOf(Creature)
            .Where(t => t != Creature && t.IsAlive)
            .ToList();

        foreach (var minion in livingMinions)
            // TODO: Inflame VFX on minion
            await CreatureCmd.Kill(minion);
    }

    // ── Fire Eye Effects ──────────────────────────────────────────

    private void StartFireLoop()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals?.SpineBody;
        if (spineBody == null || creatureNode == null) return;

        var leftEyeBone = SpineBones.Find(spineBody, "lefteyefireslot");
        var rightEyeBone = SpineBones.Find(spineBody, "righteyefireslot");
        var staffBone = SpineBones.Find(spineBody, "fireslot");

        if (Engine.GetMainLoop() is not SceneTree tree) return;

        SpawnFireParticles(creatureNode, leftEyeBone, rightEyeBone, staffBone, tree);
    }

    private void SpawnFireParticles(object creatureNode, GodotObject? leftEye, GodotObject? rightEye, GodotObject? staff,
        SceneTree tree)
    {
        if (!_alive) return;
        try
        {
            var globalPos = GetCreatureGlobalPos(creatureNode);
            Node? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            if (vfxContainer == null) return;

            if (leftEye != null)
            {
                var pos = GetBoneWorldPosition(leftEye, globalPos);
                var effect = GlowyFireEyesEffect.Create(pos);
                vfxContainer.AddChildSafely(effect);
            }

            if (rightEye != null)
            {
                var pos = GetBoneWorldPosition(rightEye, globalPos);
                var effect = GlowyFireEyesEffect.Create(pos);
                vfxContainer.AddChildSafely(effect);
            }

            if (staff != null)
            {
                var pos = GetBoneWorldPosition(staff, globalPos);
                pos.Y -= 15f;
                var effect = StaffFireEffect.Create(pos);
                effect.ZIndex = -1;
                vfxContainer.AddChildSafely(effect);
            }

            var timer = tree.CreateTimer(FireInterval);
            timer.Connect("timeout",
                Callable.From(() => SpawnFireParticles(creatureNode, leftEye, rightEye, staff, tree)));
        }
        catch (Exception e)
        {
            Log.Info($"[Collector] Fire particle error: {e.Message}");
        }
    }

    private static Vector2 GetCreatureGlobalPos(object creatureNode)
    {
        return ((dynamic)creatureNode).GlobalPosition;
    }

    private Vector2 GetBoneWorldPosition(GodotObject bone, Vector2 creatureGlobalPos)
    {
        var boneX = (float)bone.Call("get_world_x");
        var boneY = (float)bone.Call("get_world_y");
        return new Vector2(
            creatureGlobalPos.X + boneX * 1.1f,
            creatureGlobalPos.Y + boneY * 1.1f
        );
    }

    // ── Move Logic ────────────────────────────────────────────────

    private bool IsMinionDead()
    {
        var aliveMinions = CombatState.GetTeammatesOf(Creature)
            .Count(t => t != Creature && t.IsAlive);
        var torchSlots = (CombatState.Encounter?.Slots ?? [])
            .Count(s => s.StartsWith("torch"));
        return aliveMinions < torchSlots;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var spawnState = new MoveState(
            SPAWN,
            SpawnMove, new SummonIntent());

        var fireballState = new MoveState(
            FIREBALL,
            FireballMove, new SingleAttackIntent(FireballDamage));

        var buffState = new MoveState(
            BUFF,
            BuffMove, new DefendIntent(), new BuffIntent());

        var megaDebuffState = new MoveState(
            MEGA_DEBUFF,
            MegaDebuffMove, new DebuffIntent());

        var reviveState = new MoveState(
            REVIVE,
            ReviveMove, new SummonIntent());

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        spawnState.FollowUpState = moveBranch;
        fireballState.FollowUpState = moveBranch;
        buffState.FollowUpState = moveBranch;
        megaDebuffState.FollowUpState = moveBranch;
        reviveState.FollowUpState = moveBranch;

        states.Add(spawnState);
        states.Add(fireballState);
        states.Add(buffState);
        states.Add(megaDebuffState);
        states.Add(reviveState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, spawnState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        if (InitialSpawn)
        {
            TurnsTaken++;
            return SPAWN;
        }

        TurnsTaken++;

        if (TurnsTaken >= 3 && !UltUsed) return MEGA_DEBUFF;

        var num = rng.NextInt(100);

        if (num <= ReviveChance && IsMinionDead() && !LastMove(stateMachine, REVIVE)) return REVIVE;

        if (num <= 70 && !LastTwoMoves(stateMachine, FIREBALL)) return FIREBALL;

        if (!LastMove(stateMachine, BUFF)) return BUFF;

        return FIREBALL;
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

    // ── Move Actions ──────────────────────────────────────────────

    private async Task SpawnMove(IReadOnlyList<Creature> targets)
    {
        InitialSpawn = false;

        var slots = (CombatState.Encounter?.Slots ?? [])
            .Where(s => s.StartsWith("torch"))
            .ToList();

        foreach (var slot in slots)
        {
            AFTPModAudio.Play("collector", "collector_summon");
            var summoned = await CreatureCmd.Add<TorchHead>(CombatState, slot);
            await PowerCmd.Apply<MinionPower>(new BlockingPlayerChoiceContext(), summoned, 1, Creature, null);
        }
    }

    private async Task FireballMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FireballDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
        if (GameApiCompat.IsDread())
            foreach (var minion in CombatState.GetTeammatesOf(Creature).Where(t => t != Creature && t.IsAlive))
                await CreatureCmd.GainBlock(minion, BlockAmount, ValueProp.Move, null);

        foreach (var teammate in CombatState.GetTeammatesOf(Creature).Where(t => t.IsAlive))
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), teammate, StrengthAmount, Creature,
                null);
    }

    private async Task MegaDebuffMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_megaDebuffDialog, Creature, VfxColor.Swamp, VfxDuration.Long);
        AFTPModAudio.Play("collector", "collector_debuff");

        var target = targets.FirstOrDefault(t => t.IsAlive);
        if (target != null)
        {
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (targetNode != null)
            {
                var curse = CollectorCurseEffect.Create(targetNode.VfxSpawnPosition);
                Node? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
                vfxContainer?.AddChildSafely(curse);
            }
        }

        await Cmd.Wait(2.0f);

        foreach (var t in targets.Where(t => t.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), t, MegaDebuffAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), t, MegaDebuffAmount, Creature,
                null);
            await PowerCmd.Apply<FrailPower>(new BlockingPlayerChoiceContext(), t, MegaDebuffAmount, Creature, null);
        }

        UltUsed = true;
    }

    private async Task ReviveMove(IReadOnlyList<Creature> targets)
    {
        var emptySlots = (CombatState.Encounter?.Slots ?? [])
            .Where(s => s.StartsWith("torch"))
            .Where(s =>
            {
                var occupiedSlots = CombatState.GetTeammatesOf(Creature)
                    .Where(t => t.IsAlive)
                    .Select(t => t.SlotName)
                    .ToHashSet();
                return !occupiedSlots.Contains(s);
            })
            .ToList();

        foreach (var slot in emptySlots)
        {
            AFTPModAudio.Play("collector", "collector_summon");
            var summoned = await CreatureCmd.Add<TorchHead>(CombatState, slot);
            await PowerCmd.Apply<MinionPower>(new BlockingPlayerChoiceContext(), summoned, 1, Creature, null);
        }
    }

    // ── Animator ──────────────────────────────────────────────────

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