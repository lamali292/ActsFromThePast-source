using ActsFromThePast.Compatibility;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class Deca : CustomMonsterModel
{
    private const int BeamCount = 2;
    // Dread: Beam adds 3 Dazed and shuffles them into the Draw Pile instead of the Discard Pile.
    private static int BeamDazeAmount => GameApiCompat.IsDread() ? 3 : 2;
    private static PileType BeamDazePile => GameApiCompat.IsDread() ? PileType.Draw : PileType.Discard;
    private static CardPilePosition BeamDazePosition => GameApiCompat.IsDread() ? CardPilePosition.Random : CardPilePosition.Bottom;
    private const int ProtectBlock = 16;
    private const int ProtectPlatedArmorAmount = 3;

    private const string BEAM = "BEAM";
    private const string SQUARE_OF_PROTECTION = "SQUARE_OF_PROTECTION";
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250) + (GameApiCompat.IsDread() ? 20 : 0);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250) + (GameApiCompat.IsDread() ? 20 : 0);

    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private int ArtifactAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/deca/deca.tscn";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(new BlockingPlayerChoiceContext(), Creature, ArtifactAmount, Creature,
            null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var beamState = new MoveState(
            BEAM,
            Beam, new MultiAttackIntent(BeamDamage, BeamCount), new StatusIntent(BeamDazeAmount));

        var squareState = new MoveState(
            SQUARE_OF_PROTECTION,
            SquareOfProtection, new DefendIntent(), new BuffIntent());

        beamState.FollowUpState = squareState;
        squareState.FollowUpState = beamState;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { beamState, squareState },
            beamState
        );
    }

    private async Task Beam(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Beam", 0.0f);
        await Cmd.Wait(0.5f);

        await DamageCmd.Attack(BeamDamage)
            .FromMonster(this)
            .WithHitCount(BeamCount)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, BeamDazePile, BeamDazeAmount, null, BeamDazePosition);
    }

    private async Task SquareOfProtection(IReadOnlyList<Creature> targets)
    {
        var teammates = CombatState.GetTeammatesOf(Creature);
        foreach (var teammate in teammates)
            if (teammate.IsAlive)
            {
                await CreatureCmd.GainBlock(teammate, ProtectBlock, ValueProp.Move, null);
                await PowerCmd.Apply<PlatedArmorPower>(new BlockingPlayerChoiceContext(), teammate,
                    ProtectPlatedArmorAmount, Creature, null);
            }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("Idle", true);
        var attack = new AnimState("Attack_2");
        var hit = new AnimState("Hit");

        attack.NextState = idle;
        hit.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));
        animator.AddAnyState("Beam", attack);
        animator.AddAnyState("Hit", hit);

        return animator;
    }
}