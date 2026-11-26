using ActsFromThePast.Compatibility;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using Void = MegaCrit.Sts2.Core.Models.Cards.Void;

namespace ActsFromThePast.Acts.TheBeyond.Enemies;

public sealed class AwakenedOne : CustomMonsterModel
{
    private const int SlashDamage = 20;
    private const int SoulStrikeDamage = 6;
    private const int SoulStrikeHits = 4;
    private const int DarkEchoDamage = 40;
    private const int SludgeDamage = 18;
    private const int TackleDamage = 10;
    private const int TackleHits = 3;
    private const float ParticleInterval = 0.1f;

    private const string SLASH = "SLASH";
    private const string SOUL_STRIKE = "SOUL_STRIKE";
    private const string REBIRTH = "REBIRTH";
    private const string DARK_ECHO = "DARK_ECHO";
    private const string SLUDGE = "SLUDGE";
    private const string TACKLE = "TACKLE";

    private static readonly LocString _deathDialog = L10NMonsterLookup("ACTSFROMTHEPAST-AWAKENED_ONE.deathLine");

    private bool _particlesActive;
    public int _respawns;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);

    // Dread: Sludge adds 2 Voids and it awakens with extra Strength.
    private static int SludgeVoidCount => GameApiCompat.IsDread() ? 2 : 1;
    private static int RebirthStrength => GameApiCompat.IsDread() ? 3 : 0;
    private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);
    private int RegenAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 10);
    private int CuriosityAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
    private int StartingStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 0);

    protected override string VisualsPath => "res://ActsFromThePast/monsters/awakened_one/awakened_one.tscn";

    private MoveState DeadState
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    } = null!;

    public int Respawns
    {
        get => _respawns;
        set
        {
            AssertMutable();
            _respawns = value;
        }
    }

    public override bool ShouldDisappearFromDoom => Respawns >= 1;

    public async Task TriggerDeadState()
    {
        await CreatureCmd.TriggerAnim(Creature, "DeadTrigger", 0.0f);
        SetMoveImmediate(DeadState, true);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        var curiosity = Creature.CombatState?.Players.Count >= 2 ? 1 : CuriosityAmount;

        await PowerCmd.Apply<RegenEnemyPower>(new BlockingPlayerChoiceContext(), Creature, RegenAmount, Creature, null);
        await PowerCmd.Apply<CuriosityPower>(new BlockingPlayerChoiceContext(), Creature, curiosity, Creature, null);
        await PowerCmd.Apply<UnawakenedPower>(new BlockingPlayerChoiceContext(), Creature, 1, Creature, null);

        if (StartingStrength > 0)
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, StartingStrength, Creature,
                null);

        Creature.Died += OnParticleDeath;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        // Phase 1 moves
        var slashState = new MoveState(
            SLASH,
            Slash,
            new SingleAttackIntent(SlashDamage)
        );

        var soulStrikeState = new MoveState(
            SOUL_STRIKE,
            SoulStrike,
            new MultiAttackIntent(SoulStrikeDamage, SoulStrikeHits)
        );

        // Phase 2 moves
        var darkEchoState = new MoveState(
            DARK_ECHO,
            DarkEcho,
            new SingleAttackIntent(DarkEchoDamage)
        );

        var sludgeState = new MoveState(
            SLUDGE,
            Sludge, new SingleAttackIntent(SludgeDamage), new StatusIntent(SludgeVoidCount));

        var tackleState = new MoveState(
            TACKLE,
            Tackle,
            new MultiAttackIntent(TackleDamage, TackleHits)
        );

        // Dead/Rebirth
        DeadState = new MoveState(
            REBIRTH,
            RebirthMove, new HealIntent(), new BuffIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        // Phase 1 AI
        var phase1Branch = new ConditionalBranchState("PHASE1_BRANCH", SelectPhase1Move);

        slashState.FollowUpState = phase1Branch;
        soulStrikeState.FollowUpState = phase1Branch;

        // Phase 2 AI
        var phase2Branch = new ConditionalBranchState("PHASE2_BRANCH", SelectPhase2Move);

        sludgeState.FollowUpState = phase2Branch;
        tackleState.FollowUpState = phase2Branch;

        // Rebirth leads to Dark Echo, then into phase 2 cycle
        DeadState.FollowUpState = darkEchoState;
        darkEchoState.FollowUpState = phase2Branch;

        states.Add(slashState);
        states.Add(soulStrikeState);
        states.Add(darkEchoState);
        states.Add(sludgeState);
        states.Add(tackleState);
        states.Add(DeadState);
        states.Add(phase1Branch);
        states.Add(phase2Branch);

        return new MonsterMoveStateMachine(states, slashState);
    }

    private string SelectPhase1Move(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var num = rng.NextInt(100);

        if (num < 25)
        {
            if (!LastMove(stateMachine, SOUL_STRIKE))
                return SOUL_STRIKE;
            return SLASH;
        }

        if (!LastTwoMoves(stateMachine, SLASH))
            return SLASH;
        return SOUL_STRIKE;
    }

    private string SelectPhase2Move(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
    {
        var num = rng.NextInt(100);

        if (num < 50)
        {
            if (!LastTwoMoves(stateMachine, SLUDGE))
                return SLUDGE;
            return TACKLE;
        }

        if (!LastTwoMoves(stateMachine, TACKLE))
            return TACKLE;
        return SLUDGE;
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

    private async Task Slash(IReadOnlyList<Creature> targets)
    {
        AFTPModAudio.Play("awakened_one", "awakened_one_pounce");
        await CreatureCmd.TriggerAnim(Creature, "Slash", 0.0f);
        await Cmd.Wait(0.3f);

        await DamageCmd.Attack(SlashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SoulStrike(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SoulStrikeDamage)
            .FromMonster(this)
            .WithHitCount(SoulStrikeHits)
            .WithHitFx("vfx/vfx_fire_burst", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task DarkEcho(IReadOnlyList<Creature> targets)
    {
        AFTPModAudio.Play("awakened_one", "awakened_one_talk_3");

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            animState.SetAnimation("Attack_2", false);
            using var queued = animState.AddAnimationTracked("Idle_2");
            queued?.SetMixDuration(0.2f);
        }

        await Cmd.Wait(0.1f);

        if (creatureNode != null)
        {
            var pos = creatureNode.VfxSpawnPosition;
            ShockWaveEffect.PlayChaotic(pos);
        }

        await DamageCmd.Attack(DarkEchoDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task Sludge(IReadOnlyList<Creature> targets)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            animState.SetAnimation("Attack_2", false);
            using var queued = animState.AddAnimationTracked("Idle_2");
            queued?.SetMixDuration(0.2f);
        }

        await Cmd.Wait(0.3f);

        await DamageCmd.Attack(SludgeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_poison_impact", tmpSfx: "blunt_attack.mp3")
            .Execute(null);

        foreach (var target in targets)
        {
            var player = target.Player ?? target.PetOwner;
            if (player == null) continue;
            var statusCards = new CardPileAddResult[SludgeVoidCount];
            for (var i = 0; i < statusCards.Length; i++)
            {
                var voidCard = CombatState.CreateCard<Void>(player);
                statusCards[i] =
                    await CardPileCmd.AddGeneratedCardToCombat(voidCard, PileType.Draw, null, CardPilePosition.Random);
            }
            if (LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(statusCards);
                await Cmd.Wait(1f);
            }
        }
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        AFTPModAudio.Play("awakened_one", "awakened_one_attack");

        await DamageCmd.Attack(TackleDamage)
            .FromMonster(this)
            .WithHitCount(TackleHits)
            .AfterAttackerAnim(async () =>
            {
                await FastAttackAnimation.Play(Creature);
                await Cmd.Wait(0.06f);
            })
            .WithHitFx("vfx/vfx_fire_burst", tmpSfx: "blunt_attack.mp3")
            .Execute(null);
    }

    private async Task RebirthMove(IReadOnlyList<Creature> targets)
    {
        Respawns++;
        AFTPModAudio.Play("awakened_one", "awakened_one_talk_1");
        StartParticleLoop();

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode != null)
        {
            var pos = creatureNode.VfxSpawnPosition;
            var zoom = IntenseZoomEffect.Create(pos, true);
            NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(zoom);
        }

        await Cmd.Wait(0.05f);

        var spineBody = creatureNode?.Visuals.SpineBody;
        if (spineBody != null)
        {
            var animState = spineBody.GetAnimationState();
            animState.SetAnimation("Idle_2");
            var trackEntry = animState.GetCurrent(0);
            trackEntry?.SetMixDuration(1.0f);
        }

        await Cmd.Wait(1.0f);

        var unawakenedPower = Creature.Powers.OfType<UnawakenedPower>().FirstOrDefault();
        unawakenedPower?.DoRevive();

        // Heal FIRST, before removing powers
        var scaledHp = Phase2Hp * (Creature.CombatState?.Players.Count ?? 1);
        await CreatureCmd.SetMaxHp(Creature, scaledHp);
        await CreatureCmd.Heal(Creature, scaledHp);

        // NOW safe to remove powers
        var powersToRemove = Creature.Powers
            .Where(p => p.Type == PowerType.Debuff || p is CuriosityPower || p is UnawakenedPower)
            .ToList();
        foreach (var power in powersToRemove) await PowerCmd.Remove(power);

        if (RebirthStrength > 0)
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Creature, RebirthStrength, Creature,
                null);
    }

    private void StartParticleLoop()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var spineBody = creatureNode?.Visuals?.SpineBody;
        if (spineBody == null || creatureNode == null) return;

        var eyeSpineBone = SpineBones.Find(spineBody, "Eye");
        var hipsSpineBone = SpineBones.Find(spineBody, "Hips");

        if (eyeSpineBone == null && hipsSpineBone == null) return;

        if (Engine.GetMainLoop() is not SceneTree tree) return;

        _particlesActive = true;
        SpawnParticles(creatureNode, eyeSpineBone, hipsSpineBone, tree);
    }

    private void SpawnParticles(object creatureNode, GodotObject? eyeBone, GodotObject? hipsBone, SceneTree tree)
    {
        if (!_particlesActive) return;

        try
        {
            var globalPos = ((dynamic)creatureNode).GlobalPosition;

            if (eyeBone != null)
            {
                var eyeX = (float)eyeBone.Call("get_world_x");
                var eyeY = (float)eyeBone.Call("get_world_y");
                var eyePos = new Vector2(
                    globalPos.X + eyeX * 1.1f,
                    globalPos.Y + eyeY * 1.1f - 20f
                );
                var eyeParticle = AwakenedEyeParticle.Create(eyePos);
                NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(eyeParticle);
            }

            if (hipsBone != null)
            {
                var nCreature = NCombatRoom.Instance?.GetCreatureNode(Creature);
                if (nCreature != null)
                {
                    var wingParticle = AwakenedWingParticle.Create(nCreature, hipsBone);
                    NCombatRoom.Instance?.CombatVfxContainer?.AddChildSafely(wingParticle);
                }
            }
        }
        catch (Exception)
        {
            //  Log.Info($"[AwakenedOne] Particle error: {e.Message}");
        }

        var timer = tree.CreateTimer(ParticleInterval);
        timer.Connect("timeout", Callable.From(() => SpawnParticles(creatureNode, eyeBone, hipsBone, tree)));
    }

    private void OnParticleDeath(Creature _)
    {
        _particlesActive = false;
        Creature.Died -= OnParticleDeath;
    }

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);

        if (creature != Creature)
            return;

        if (Respawns < 1)
        {
            var text = _deathDialog.GetFormattedText();
            var bubble = NSpeechBubbleVfx.Create(text, Creature, 2.5);
            if (bubble != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);
        }
        else
        {
            var cultists = CombatState.Enemies
                .Where(e => e is { IsAlive: true, Monster: Cultist })
                .ToList();

            foreach (var cultist in cultists) await CreatureCmd.Escape(cultist);
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle1 = new AnimState("Idle_1", true);
        var attack1 = new AnimState("Attack_1");
        var hit1 = new AnimState("Hit");

        attack1.NextState = idle1;
        hit1.NextState = idle1;

        var idle2 = new AnimState("Idle_2", true);
        var hit2 = new AnimState("Hit");
        hit2.NextState = idle2;

        idle1.AddBranch("Awaken", idle2);

        var animator = new CreatureAnimator(idle1, controller);
        animator.AddAnyState("Dead", new AnimState("Dead"));

        animator.AddAnyState("Slash", attack1, () => Respawns == 0);
        animator.AddAnyState("Hit", hit1, () => Respawns == 0);
        animator.AddAnyState("Hit", hit2, () => Respawns >= 1);
        animator.AddAnyState("Phase2Hit", idle2, () => Respawns >= 1);

        return animator;
    }
}