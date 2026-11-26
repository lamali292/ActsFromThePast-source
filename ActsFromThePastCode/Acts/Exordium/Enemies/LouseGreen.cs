using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
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

namespace ActsFromThePast;

public sealed class LouseGreen : CustomMonsterModel
{
    private const int WeakAmount = 2;
    private readonly Dictionary<Creature, int> _biteDamageByCreature = new();

    // The game removes Curl Up when the louse curls up, and the louse applies it when it joins the fight, so it is curled
    // up from the moment that power is gone until it has opened up again for its next move.
    private bool _openedAgain;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 11);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/louse_green/louse_green.tscn";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Insect;

    private bool IsOpen => _openedAgain || Creature.GetPower<CurlUpPower>() != null;

    private int GetBiteDamage()
    {
        return _biteDamageByCreature.GetValueOrDefault(Creature, 0);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var biteState = new MoveState(
            "BITE",
            Bite, new DynamicSingleAttackIntent(() => GetBiteDamage()));
        var spitWebState = new MoveState(
            "SPIT_WEB",
            SpitWeb, new DebuffIntent());

        var randomBranch = new RandomBranchState("RANDOM");

        biteState.FollowUpState = randomBranch;
        spitWebState.FollowUpState = randomBranch;

        var spitWebMaxRepeats = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies) ? 1 : 2;

        randomBranch.AddBranch(spitWebState, spitWebMaxRepeats, 25f);
        randomBranch.AddBranch(biteState, 2, 75f);

        states.Add(biteState);
        states.Add(spitWebState);
        states.Add(randomBranch);

        return new MonsterMoveStateMachine(states, randomBranch);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        var dmg = AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies)
            ? RunRng.MonsterAi.NextInt(6, 9)
            : RunRng.MonsterAi.NextInt(5, 8);
        _biteDamageByCreature[Creature] = dmg;

        var curlUpAmount = AscensionHelper.HasAscension(AscensionLevel.ToughEnemies)
            ? RunRng.MonsterAi.NextInt(9, 13)
            : RunRng.MonsterAi.NextInt(3, 8);

        await PowerCmd.Apply<CurlUpPower>(new BlockingPlayerChoiceContext(), Creature, curlUpAmount, Creature, null);
    }

    private async Task Bite(IReadOnlyList<Creature> targets)
    {
        if (!IsOpen)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(Creature, "transitiontoopened", 0.0f);
            await Cmd.Wait(0.5f);
            _openedAgain = true;
        }

        await FastAttackAnimation.Play(Creature);

        await DamageCmd.Attack(GetBiteDamage())
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_attack")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SpitWeb(IReadOnlyList<Creature> targets)
    {
        if (!IsOpen)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(Creature, "transitiontoopened", 0.0f);
            await Cmd.Wait(0.3f);
            await CreatureCmd.TriggerAnim(Creature, "rear", 0.0f);
            await Cmd.Wait(0.7f);
            _openedAgain = true;
        }
        else
        {
            await CreatureCmd.TriggerAnim(Creature, "rear", 0.0f);
            await Cmd.Wait(0.5f);
        }

        AFTPModAudio.Play("general", "attack_magic_fast_3", 0f, 0.02f, 1.9f);

        var combatRoom = NCombatRoom.Instance;
        var louseNode = combatRoom?.GetCreatureNode(Creature);
        if (louseNode != null)
        {
            var sourcePos = louseNode.VfxSpawnPosition + new Vector2(-70f, -10f);

            foreach (var target in targets.Where(t => t.IsAlive))
            {
                var targetNode = combatRoom?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    var targetPos = targetNode.VfxSpawnPosition;
                    var effect = WebEffect.Create(sourcePos, targetPos);
                    combatRoom?.CombatVfxContainer.AddChild(effect);
                }
            }
        }

        foreach (var target in targets.Where(t => t.IsAlive))
            await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), target, WeakAmount, Creature, null);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle", true);
        var idleClosed = new AnimState("idle closed", true);
        var rear = new AnimState("rear");
        var open = new AnimState("transitiontoopened");
        var close = new AnimState("transitiontoclosed");

        rear.NextState = idle;
        open.NextState = idle;
        close.NextState = idleClosed;

        idle.AddBranch("rear", rear);
        idle.AddBranch("Curl", close);
        idle.AddBranch("transitiontoclosed", close);

        idleClosed.AddBranch("transitiontoopened", open);
        idleClosed.AddBranch("rear", rear);

        open.AddBranch("rear", rear);

        // A hit plays on top of the current idle state: the open one or the curled up one. It has to answer the same
        // triggers as the idle state it stands in for.
        var hit = new AnimState("Hit");
        var hitClosed = new AnimState("HitClosed");
        hit.NextState = idle;
        hitClosed.NextState = idleClosed;

        hit.AddBranch("rear", rear);
        hit.AddBranch("Curl", close);
        hit.AddBranch("transitiontoclosed", close);

        hitClosed.AddBranch("transitiontoopened", open);
        hitClosed.AddBranch("rear", rear);

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Hit", hitClosed, () => !IsOpen);
        animator.AddAnyState("Hit", hit);
        return animator;
    }
}