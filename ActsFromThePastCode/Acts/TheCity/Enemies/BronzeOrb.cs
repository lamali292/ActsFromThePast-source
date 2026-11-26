using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Animation;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class BronzeOrb : CustomMonsterModel
{
    private const int BeamDamage = 8;
    private const int SupportBlockAmount = 12;

    private const string BEAM = "BEAM";
    private const string SUPPORT_BEAM = "SUPPORT_BEAM";
    private const string STASIS = "STASIS";

    private bool _usedStasis;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 52);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 58);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/bronze_orb/bronze_orb.tscn";

    public int BobIndex
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }

    private bool UsedStasis
    {
        get => _usedStasis;
        set
        {
            AssertMutable();
            _usedStasis = value;
        }
    }

    public bool SpawnAnimPending
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    }


    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // The bob is part of the animations, see scripts/spine/make_bronze_orb.py. Even and odd orbs bob in opposite phase.
        var odd = BobIndex % 2 != 0;
        var idle = new AnimState(odd ? "idle_odd" : "idle", true);
        var hit = new AnimState(odd ? "HitOdd" : "Hit");
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Dead", new AnimState(odd ? "DeadOdd" : "Dead"));
        return animator;
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _usedStasis = false;

        if (SpawnAnimPending)
        {
            var visuals = NCombatRoom.Instance?.GetCreatureNode(Creature)?.Visuals;
            var sprite = visuals?.GetNodeOrNull<Node2D>("Visuals");
            sprite?.Visible = false;
        }

    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var beamState = new MoveState(
            BEAM,
            BeamMove, new SingleAttackIntent(BeamDamage));

        var supportBeamState = new MoveState(
            SUPPORT_BEAM,
            SupportBeamMove, new DefendIntent());

        var stasisState = new MoveState(
            STASIS,
            StasisMove, new CardDebuffIntent());

        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        beamState.FollowUpState = moveBranch;
        supportBeamState.FollowUpState = moveBranch;
        stasisState.FollowUpState = moveBranch;

        states.Add(beamState);
        states.Add(supportBeamState);
        states.Add(stasisState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, moveBranch);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var num = rng.NextInt(100);

        if (!UsedStasis && num >= 25)
        {
            UsedStasis = true;
            return STASIS;
        }

        if (num >= 70 && !LastTwoMoves(stateMachine, SUPPORT_BEAM)) return SUPPORT_BEAM;

        if (!LastTwoMoves(stateMachine, BEAM)) return BEAM;

        return SUPPORT_BEAM;
    }

    private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
    {
        var log = stateMachine.StateLog;
        if (log.Count < 2) return false;
        return log[^1].Id == moveId && log[^2].Id == moveId;
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        BorderFlashEffect.PlaySky();
        AFTPModAudio.Play("general", "magic_beam_short", -6f);

        var target = targets.FirstOrDefault(t => t.IsAlive);
        var targetPos = target != null
            ? Sts1VfxHelper.GetCreatureCenter(target)
            : Vector2.Zero;
        var orbPos = Sts1VfxHelper.GetCreatureCenter(Creature);

        var laser = SmallLaserEffect.Create(orbPos, targetPos);
        Sts1VfxHelper.Play(laser);

        await Cmd.Wait(0.3f);
        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .Execute(null);
    }

    private async Task SupportBeamMove(IReadOnlyList<Creature> targets)
    {
        var automaton = CombatState.GetTeammatesOf(Creature)
            .FirstOrDefault(t => t is { Monster: BronzeAutomaton, IsAlive: true });

        if (automaton == null)
            return;

        AFTPModAudio.Play("general", "magic_beam_short", -6f);

        var orbPos = Sts1VfxHelper.GetCreatureCenter(Creature);
        var automatonPos = Sts1VfxHelper.GetCreatureCenter(automaton);

        var laser = SmallLaserEffect.Create(orbPos, automatonPos);
        Sts1VfxHelper.Play(laser);

        await Cmd.Wait(0.3f);
        await CreatureCmd.GainBlock(automaton, SupportBlockAmount, ValueProp.Move, null);
    }

    private async Task StasisMove(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive))
        {
            var player = target.Player ?? target.PetOwner;
            if (player == null) continue;

            var drawCards = CardPile.GetCards(player, PileType.Draw).ToList();

            var discardCards = CardPile.GetCards(player, PileType.Discard).ToList();

            if (drawCards.Count == 0 && discardCards.Count == 0)
                continue;

            var pool = drawCards.Count > 0 ? drawCards : discardCards;
            pool.StableShuffle(RunRng.CombatCardGeneration);

            var cardToSteal = pool.FirstOrDefault(c => c.Rarity == CardRarity.Rare)
                              ?? pool.FirstOrDefault(c => c.Rarity == CardRarity.Uncommon)
                              ?? pool.FirstOrDefault(c => c.Rarity == CardRarity.Common)
                              ?? pool.FirstOrDefault();

            if (cardToSteal == null)
                continue;

            await CardPileCmd.RemoveFromCombat(cardToSteal);

            // Show stolen card on the orb
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode != null && LocalContext.IsMine(cardToSteal))
            {
                var specialNode = creatureNode.GetSpecialNode<Marker2D>("%StolenCardPos");
                if (specialNode != null)
                {
                    var ncard = NCard.Create(cardToSteal);
                    if (ncard != null)
                    {
                        specialNode.AddChildSafely(ncard);
                        ncard.Position += ncard.Size * 0.5f;
                        ncard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
                    }
                }
            }

            var stasis = (StasisPower)ModelDb.Power<StasisPower>().ToMutable();
            await stasis.Capture(cardToSteal, target);
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), stasis, Creature, 1, Creature, null);
        }
    }
}