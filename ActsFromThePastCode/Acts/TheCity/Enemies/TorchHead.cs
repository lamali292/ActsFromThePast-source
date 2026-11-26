using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public sealed class TorchHead : CustomMonsterModel
{
    private const int AttackDamage = 7;
    private const float FireInterval = 0.04f;

    private const string TACKLE = "TACKLE";
    private bool _alive = true;

    private SceneTreeTimer _fireTimer = null!;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 40);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/torch_head/torch_head.tscn";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        Creature.Died += OnDeath;
        StartFireLoop();
    }

    private void OnDeath(Creature _)
    {
        Creature.Died -= OnDeath;
        _alive = false;
    }

    private void StartFireLoop()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals?.SpineBody;
        if (spineBody == null || creatureNode == null) return;

        if (SpineBones.Find(spineBody, "fireslot") is not { } spineBone) return;

        if (Engine.GetMainLoop() is not SceneTree tree) return;

        SpawnFireParticle(creatureNode, spineBone, tree);
    }

    private void SpawnFireParticle(object creatureNode, GodotObject spineBone, SceneTree tree)
    {
        if (!_alive) return;
        try
        {
            var boneX = (float)spineBone.Call("get_world_x");
            var boneY = (float)spineBone.Call("get_world_y");
            var globalPos = ((dynamic)creatureNode).GlobalPosition;
            var firePos = new Vector2(
                globalPos.X + boneX * 1.1f,
                globalPos.Y + boneY * 1.1f - 20f
            );

            var effect = TorchHeadFireEffect.Create(firePos);
            NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(effect);

            _fireTimer = tree.CreateTimer(FireInterval);
            _fireTimer.Connect("timeout", Callable.From(() => SpawnFireParticle(creatureNode, spineBone, tree)));
        }
        catch (Exception e)
        {
            Log.Info($"[TorchHead] Fire particle error: {e.Message}");
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackleState = new MoveState(
            TACKLE,
            TackleMove, new SingleAttackIntent(AttackDamage));

        tackleState.FollowUpState = tackleState;

        states.Add(tackleState);

        return new MonsterMoveStateMachine(states, tackleState);
    }

    private async Task TackleMove(IReadOnlyList<Creature> targets)
    {
        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/axe_ruby_raider/axe_ruby_raider_attack")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
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