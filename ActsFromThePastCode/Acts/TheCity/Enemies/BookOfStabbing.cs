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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast;

public sealed class BookOfStabbing : CustomMonsterModel
{
    private const string STAB = "STAB";
    private const string BIG_STAB = "BIG_STAB";

    private static readonly string[] StabVfxPaths =
    [
        "vfx/slash/vfx_slash_core",
        "vfx/vfx_dramatic_stab",
        "vfx/vfx_attack_slash",
        "vfx/vfx_big_slash"
    ];

    private int _stabCount;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 168, 160);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 172, 164);

    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int BigStabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 21);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/book_of_stabbing/book_of_stabbing.tscn";

    private int StabCount
    {
        get => _stabCount;
        set
        {
            AssertMutable();
            _stabCount = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _stabCount = 1;
        await PowerCmd.Apply<PainfulStabsPower>(new BlockingPlayerChoiceContext(), Creature, 1, Creature, null);
        Creature.Died += OnDeath;
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;

        // The game's Painful Stabs keeps its owner on the field after death (its corpse stays in the vanilla fight).
        // CreatureCmd.Kill raises Died before it asks whether the creature is removed, so dropping the power here is
        // enough to make the Book leave like every other monster.
        if (Creature.GetPower<PainfulStabsPower>() is { } stabs)
            Creature.RemovePowerInternal(stabs);

        AFTPModAudio.Play("book_of_stabbing", "book_of_stabbing_death");
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var stabState = new MoveState(
            STAB,
            Stab,
            new DynamicMultiAttackIntent(() => StabDamage, () => StabCount)
        );

        var bigStabState = new MoveState(
            BIG_STAB,
            BigStab,
            new SingleAttackIntent(BigStabDamage)
        );

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        stabState.FollowUpState = moveBranch;
        bigStabState.FollowUpState = moveBranch;

        states.Add(stabState);
        states.Add(bigStabState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var num = rng.NextInt(100);

        if (num < 15)
        {
            if (LastMove(stateMachine, BIG_STAB))
            {
                StabCount++;
                return STAB;
            }

            StabCount++; // A18+ behavior
            return BIG_STAB;
        }

        if (LastTwoMoves(stateMachine, STAB))
        {
            StabCount++; // A18+ behavior
            return BIG_STAB;
        }

        StabCount++;
        return STAB;
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

    private async Task Stab(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StabDamage)
            .WithHitCount(StabCount)
            .FromMonster(this)
            .WithAttackerAnim("Stab", 0.5f)
            .OnlyPlayAnimOnce()
            .BeforeDamage(() =>
            {
                PlayStabSfx();
                return Task.CompletedTask;
            })
            .WithHitVfxNode(CreateRandomStabVfx)
            .Execute(null);
    }


    private static Node2D? CreateRandomStabVfx(Creature target)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode == null) return null;
        var path = StabVfxPaths[Rng.Chaotic.NextInt(StabVfxPaths.Length)];
        var vfx = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(path)).Instantiate<Node2D>();
        vfx.Scale = new Vector2(-1f, 1f);
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
        return vfx;
    }

    private void PlayStabSfx()
    {
        var roll = Rng.Chaotic.NextInt(4) + 1;
        var sfxName = $"book_of_stabbing_attack_{roll}";
        AFTPModAudio.Play("book_of_stabbing", sfxName);
    }

    private async Task BigStab(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigStabDamage)
            .FromMonster(this)
            .WithAttackerAnim("BigStab", 0.5f)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack")
            .WithHitFx("vfx/vfx_attack_slash")
            .BeforeDamage(() =>
            {
                PlayStabSfx();
                return Task.CompletedTask;
            })
            .Execute(null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var stab = new AnimState("Attack");
        var bigStab = new AnimState("Attack_2");
        var hit = new AnimState("Hit");

        stab.NextState = idle;
        bigStab.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Stab", stab);
        animator.AddAnyState("BigStab", bigStab);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}