using ActsFromThePast.Compatibility;
using MegaCrit.Sts2.Core.Models;
using ActsFromThePast.Acts.TheBeyond.Events;
using MegaCrit.Sts2.Core.Nodes.Audio;
using ActsFromThePast.Patches.Cards;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class Hexaghost : CustomMonsterModel
{
    private const int SearDamage = 6;
    private const int FireTackleCount = 2;
    private const int InfernoHits = 6;
    private const int StrengthenBlock = 12;

    private const string ACTIVATE = "ACTIVATE";
    private const string DIVIDER = "DIVIDER";
    private const string TACKLE = "TACKLE";
    private const string INFLAME = "INFLAME";
    private const string SEAR = "SEAR";
    private const string INFERNO = "INFERNO";

    private bool _burnUpgraded;
    private int _dividerDamage;

    private MoveState _dividerState = null!;
    private int _orbActiveCount;

    /// <summary>Hexaghost's boss music (and the fight's silence before it) follows this, see MusicPatches.</summary>
    public bool IsActivated { get; private set; }

    private HexaghostVisuals? _visuals;

    // HACK: Hexaghost scene includes a hidden DummySpine node because 
// Sprite2D-only scenes fail to load from the mod folder for unknown reasons.
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 264, 250);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 264, 250);
    private int InfernoDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private int FireTackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2) + (GameApiCompat.IsDread() ? 1 : 0);

    // Dread: Divider hits harder (HP / 10 instead of HP / 12) and the Burns from Sear and Inferno go into the Draw Pile.
    private static int DividerDivisor => GameApiCompat.IsDread() ? 10 : 12;
    private static PileType SearPile => GameApiCompat.IsDread() ? PileType.Draw : PileType.Discard;
    private static CardPilePosition SearPosition => GameApiCompat.IsDread() ? CardPilePosition.Random : CardPilePosition.Bottom;
    private int SearBurnCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override string VisualsPath
    {
        get
        {
            var path = "res://ActsFromThePast/monsters/hexaghost/hexaghost.tscn";
            return path;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _burnUpgraded = false;
        _orbActiveCount = 0;

        // Initialize visuals
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null) _visuals = new HexaghostVisuals(Creature, creatureNode);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        // Activate is the first move - calculates divider damage based on player HP
        var activateState = new MoveState(
            ACTIVATE,
            Activate, new UnknownIntent());

        // Divider - 6 hits based on player HP / 12 + 1
        _dividerState = new MoveState(
            DIVIDER,
            Divider, new DynamicMultiAttackIntent(() => _dividerDamage, 6));

        // Fire Tackle - 2 hits
        var tackleState = new MoveState(
            TACKLE,
            Tackle, new MultiAttackIntent(FireTackleDamage, FireTackleCount));

        // Inflame - Block + Strength
        var inflameState = new MoveState(
            INFLAME,
            Inflame, new DefendIntent(), new BuffIntent());

        // Sear - Attack + Burns
        var searState = new MoveState(
            SEAR,
            Sear, new SingleAttackIntent(SearDamage), new StatusIntent(SearBurnCount));

        // Inferno - 6 hits + upgrade burns
        var infernoState = new MoveState(
            INFERNO,
            Inferno, new MultiAttackIntent(InfernoDamage, InfernoHits), new DebuffIntent());

        // Move selection branch
        var moveBranch = new ConditionalBranchState("MOVE_BRANCH", SelectNextMove);

        activateState.FollowUpState = _dividerState;
        _dividerState.FollowUpState = moveBranch;
        tackleState.FollowUpState = moveBranch;
        inflameState.FollowUpState = moveBranch;
        searState.FollowUpState = moveBranch;
        infernoState.FollowUpState = moveBranch;

        states.Add(activateState);
        states.Add(_dividerState);
        states.Add(tackleState);
        states.Add(inflameState);
        states.Add(searState);
        states.Add(infernoState);
        states.Add(moveBranch);

        return new MonsterMoveStateMachine(states, activateState);
    }

    private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        // Hexaghost follows a fixed pattern based on orb count
        switch (_orbActiveCount)
        {
            case 0:
                return SEAR;
            case 1:
                return TACKLE;
            case 2:
                return SEAR;
            case 3:
                return INFLAME;
            case 4:
                return TACKLE;
            case 5:
                return SEAR;
            case 6:
                return INFERNO;
            default:
                return SEAR;
        }
    }

    private async Task Activate(IReadOnlyList<Creature> targets)
    {
        _orbActiveCount = 6;

        _visuals?.ActivateAllOrbs();
        _visuals?.SetTargetRotationSpeed(120f);

        // Starts the boss music
        IsActivated = true;
        NRunMusicController.Instance?.UpdateTrack();

        // Calculate divider damage based on average HP among targets
        var livingTargets = targets.Where(t => t.IsAlive).ToList();
        var averageHp = livingTargets.Count > 0
            ? livingTargets.Average(t => t.CurrentHp)
            : 1;
        _dividerDamage = (int)(averageHp / DividerDivisor) + 1;
    }

    private async Task Divider(IReadOnlyList<Creature> targets)
    {
        for (var i = 0; i < 6; i++)
        {
            foreach (var target in targets.Where(t => t.IsAlive))
            {
                var playerCenter = Sts1VfxHelper.GetCreatureCenter(target);

                var offsetX = (float)(Rng.Chaotic.NextDouble() * 240.0 - 120.0);
                var offsetY = (float)(Rng.Chaotic.NextDouble() * 240.0 - 120.0);
                var ignite = GhostIgniteEffect.Create(playerCenter.X + offsetX, playerCenter.Y + offsetY);
                //  Sts1VfxHelper.Play(ignite);
            }

            //    PlayGhostOrbIgniteSfx();

            await Cmd.Wait(0.05f);

            await DamageCmd.Attack(_dividerDamage)
                .FromMonster(this)
                .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
                .WithHitVfxNode(target => CreateGhostFireBurst(target))
                .Execute(null);
        }

        await DeactivateAllOrbs();
    }

    public static Node2D? CreateGhostFireBurst(Creature target)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode is not { IsInteractable: true })
            return null;

        var vfx = PreloadManager.Cache.GetScene("scenes/vfx/vfx_fire_burst.tscn").Instantiate<Node2D>();
        vfx.GlobalPosition = creatureNode.VfxSpawnPosition;

        // Ghostly green color
        vfx.Modulate = new Color(0.455f, 0.918f, 0.027f);

        return vfx;
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        BorderFlashEffect.PlayChartreuse();
        await FastAttackAnimation.Play(Creature);
        await DamageCmd.Attack(FireTackleDamage)
            .WithHitCount(FireTackleCount)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitVfxNode(target => CreateGhostFireBurst(target))
            .Execute(null);
        await ActivateOrb();
    }


    private async Task Inflame(IReadOnlyList<Creature> targets)
    {
        NPowerUpVfx.CreateGhostly(Creature);

        await CreatureCmd.GainBlock(Creature, StrengthenBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, StrengthAmount, Creature,
            null);

        await ActivateOrb();
    }

    private async Task Sear(IReadOnlyList<Creature> targets)
    {
        var playerCreature = targets.FirstOrDefault(c => c.Player != null);
        if (playerCreature != null)
        {
            var startPos = Sts1VfxHelper.GetCreatureCenter(Creature);
            var targetPos = Sts1VfxHelper.GetCreatureCenter(playerCreature);

            var fireball = FireballEffect.Create(startPos, targetPos);
            Sts1VfxHelper.Play(fireball);

            await Cmd.Wait(0.5f);
        }

        await DamageCmd.Attack(SearDamage)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitVfxNode(target => CreateGhostFireBurst(target))
            .Execute(null);

        await AddBurnsToDiscard(targets, SearBurnCount);
        await ActivateOrb();
    }

    private async Task Inferno(IReadOnlyList<Creature> targets)
    {
        var screenFire = ScreenOnFireEffect.Create();
        Sts1VfxHelper.Play(screenFire);

        await Cmd.Wait(1.0f);

        await DamageCmd.Attack(InfernoDamage)
            .WithHitCount(InfernoHits)
            .FromMonster(this)
            .WithAttackerFx(sfx: "event:/sfx/characters/attack_fire")
            .WithHitVfxNode(target => CreateGhostFireBurst(target))
            .Execute(null);

        await UpgradeAllBurnsAndAddMore(targets);
        _burnUpgraded = true;
        await DeactivateAllOrbs();
    }

    private Task ActivateOrb()
    {
        _orbActiveCount++;
        _visuals?.ActivateNextOrb();
        return Task.CompletedTask;
    }

    private Task DeactivateAllOrbs()
    {
        _orbActiveCount = 0;
        _visuals?.DeactivateAllOrbs();
        PlayExhaustSfx();
        PlayExhaustSfx();
        return Task.CompletedTask;
    }

    private async Task UpgradeAllBurnsAndAddMore(IReadOnlyList<Creature> targets)
    {
        BurnUpgradePatch.AllowBurnUpgrade = true;

        try
        {
            foreach (var playerCreature in targets.Where(t => t.Player != null))
            {
                if (playerCreature.Player is not { } player) continue;

                // Upgrade all Burns in draw, discard, and hand piles
                var burnsToUpgrade = player.Piles
                    .Where(p => p.Type == PileType.Draw || p.Type == PileType.Discard || p.Type == PileType.Hand)
                    .SelectMany(p => p.Cards)
                    .OfType<Burn>()
                    .Where(b => b.IsUpgradable)
                    .ToList();

                foreach (var burn in burnsToUpgrade)
                {
                    burn.UpgradeInternal();
                    burn.FinalizeUpgradeInternal();
                }

                // Add 3 upgraded Burns to discard
                if (playerCreature.CombatState is not { } combatState) continue;
                var statusCards = new CardPileAddResult[3];

                for (var i = 0; i < 3; i++)
                {
                    var burn = combatState.CreateCard<Burn>(player);
                    burn.UpgradeInternal();
                    burn.FinalizeUpgradeInternal();
                    statusCards[i] = await CardPileCmd.AddGeneratedCardToCombat(burn, SearPile, null, SearPosition);
                }

                CardCmd.PreviewCardPileAdd(statusCards, style: CardPreviewStyle.HorizontalLayout);
            }

            await Cmd.Wait(1f);
        }
        finally
        {
            BurnUpgradePatch.AllowBurnUpgrade = false;
        }
    }

    private async Task AddBurnsToDiscard(IReadOnlyList<Creature> targets, int count)
    {
        if (!_burnUpgraded)
        {
            await CardPileCmd.AddToCombatAndPreview<Burn>(targets, SearPile, count, null, SearPosition);
            return;
        }

        BurnUpgradePatch.AllowBurnUpgrade = true;
        try
        {
            foreach (var playerCreature in targets.Where(t => t.Player != null))
            {
                if (playerCreature.Player is not { } player) continue;
                if (playerCreature.CombatState is not { } combatState) continue;
                var statusCards = new CardPileAddResult[count];

                for (var i = 0; i < count; i++)
                {
                    var burn = combatState.CreateCard<Burn>(player);
                    burn.UpgradeInternal();
                    burn.FinalizeUpgradeInternal();
                    statusCards[i] = await CardPileCmd.AddGeneratedCardToCombat(burn, SearPile, null, SearPosition);
                }

                CardCmd.PreviewCardPileAdd(statusCards,
                    style: count > 5 ? CardPreviewStyle.MessyLayout : CardPreviewStyle.HorizontalLayout);
            }

            await Cmd.Wait(1f);
        }
        finally
        {
            BurnUpgradePatch.AllowBurnUpgrade = false;
        }
    }

    private void PlayGhostOrbIgniteSfx()
    {
        var sfxName = Rng.Chaotic.NextInt(2) == 0 ? "ghost_orb_ignite_1" : "ghost_orb_ignite_2";
        AFTPModAudio.Play("hexaghost", sfxName);
    }

    private void PlayExhaustSfx()
    {
        NDebugAudioManager.Instance?.Play("card_exhaust.mp3");
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        CardModel? source,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        // No base call: AbstractModel.AfterDeath is empty, and its 0.113.0 signature does not exist on 0.111.1.
        if (creature != Creature)
            return;

        _visuals?.HideAllOrbs();
        _visuals?.Dispose();
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Long);
    }
}