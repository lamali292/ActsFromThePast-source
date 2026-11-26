using System.Reflection;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Tests;

public class AftpTests
{
    private static IEnumerable<Type> ModTypes<TBase>() =>
        typeof(AftpTests).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false } && t.IsSubclassOf(typeof(TBase))
                                                                                    && t.GetConstructor(Type.EmptyTypes) != null);

    /// <summary>Every model of ours must be reachable through ModelDb; this is what breaks first when a game update renames an API.</summary>
    [AftpTest]
    public Task AllModelsAreRegistered(TestContext ctx)
    {
        var missing = new List<string>();
        foreach (var type in ModTypes<CardModel>().Concat(ModTypes<RelicModel>()).Concat(ModTypes<PowerModel>())
                     .Concat(ModTypes<MonsterModel>()).Concat(ModTypes<EncounterModel>()).Concat(ModTypes<EventModel>()))
            try
            {
                ModelDb.GetById<AbstractModel>(ModelDb.GetId(type));
            }
            catch (Exception)
            {
                missing.Add(type.Name);
            }

        Assert.IsTrue(missing.Count == 0, $"Not in ModelDb: {string.Join(", ", missing)}");
        return Task.CompletedTask;
    }

    /// <summary>A title that is still the loc key (or empty) means the localization failed to load or a key was renamed.</summary>
    [AftpTest]
    public Task TitlesAreLocalised(TestContext ctx)
    {
        var bad = new List<string>();
        foreach (var type in ModTypes<CardModel>().Concat(ModTypes<RelicModel>()).Concat(ModTypes<PowerModel>()))
        {
            var model = ModelDb.GetById<AbstractModel>(ModelDb.GetId(type));
            var title = type.GetProperty("Title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(model) switch
            {
                string s => s,
                LocString l => l.GetFormattedText(),
                _ => null
            };
            if (string.IsNullOrWhiteSpace(title) || title.Contains(model.Id.Entry)) bad.Add($"{type.Name} ('{title}')");
        }

        Assert.IsTrue(bad.Count == 0, $"Unlocalised titles: {string.Join(", ", bad)}");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Dread Dominions live in ActsFromThePast.Dread.dll, which only loads on 0.113.0+. Everything here is by reflection so
    ///     the test also passes (as a no-op) on 0.111.1, where it proves the mod still loads without the Dread types.
    /// </summary>
    [AftpTest]
    public Task DreadDominionsAreRegistered(TestContext ctx)
    {
        var hasDread = HarmonyLib.AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.Dread.DominionModel") != null;
        var dominionType = HarmonyLib.AccessTools.TypeByName("ActsFromThePast.Dread.GuardianDominion");
        if (!hasDread)
        {
            Assert.IsTrue(dominionType == null, "The Dread assembly must not be loaded on a game without Dread mode.");
            return Task.CompletedTask;
        }

        var getDominion = typeof(EncounterModel).GetMethod("GetDominion")!;
        var bosses = new (EncounterModel Boss, string TypeName)[]
        {
            (ModelDb.Encounter<GuardianBoss>(), "GuardianDominion"),
            (ModelDb.Encounter<HexaghostBoss>(), "HexaghostDominion"),
            (ModelDb.Encounter<SlimeBossBoss>(), "SlimeBossDominion"),
            (ModelDb.Encounter<BronzeAutomatonBoss>(), "BronzeAutomatonDominion"),
            (ModelDb.Encounter<ChampBoss>(), "ChampDominion"),
            (ModelDb.Encounter<CollectorBoss>(), "CollectorDominion"),
            (ModelDb.Encounter<AwakenedOneBoss>(), "AwakenedOneDominion"),
            (ModelDb.Encounter<DonuAndDecaBoss>(), "DonuAndDecaDominion"),
            (ModelDb.Encounter<TimeEaterBoss>(), "TimeEaterDominion")
        };

        var problems = new List<string>();
        foreach (var (boss, typeName) in bosses)
        {
            var dominion = getDominion.Invoke(boss, null) as AbstractModel;
            if (dominion == null || dominion.GetType().Name != typeName)
            {
                problems.Add($"{boss.GetType().Name}: GetDominion returned {dominion?.GetType().Name ?? "null"}");
                continue;
            }

            if (ModelDb.GetByIdOrNull<AbstractModel>(dominion.Id) == null) problems.Add($"{typeName}: not in ModelDb");
            var description = dominion.GetType().GetProperty("Description")!.GetValue(dominion) as LocString;
            if (description == null || !description.Exists())
                problems.Add($"{typeName}: no description (ascension table, {dominion.Id.Entry})");
        }

        // Powers that belong to a Dominion are registered by the Dread assembly too.
        foreach (var powerName in new[]
                 {
                     "TimeWarpDominionPower", "CollectorDominionPower", "ChampDominionPower", "DonuAndDecaDominionPower",
                     "HexaghostDominionPower"
                 })
        {
            var powerType = HarmonyLib.AccessTools.TypeByName("ActsFromThePast.Dread." + powerName);
            var power = powerType == null ? null : ModelDb.GetByIdOrNull<AbstractModel>(ModelDb.GetId(powerType));
            if (power == null)
                problems.Add($"{powerName}: not in ModelDb");
            else if (powerType!.GetProperty("Title")!.GetValue(power) is not LocString { } title || !title.Exists())
                problems.Add($"{powerName}: no title");
        }

        Assert.IsTrue(problems.Count == 0, string.Join("; ", problems));
        return Task.CompletedTask;
    }

    /// <summary>Flight halves the damage of attacks against its owner.</summary>
    [AftpTest]
    public async Task FlightHalvesAttackDamage(TestContext ctx)
    {
        var enemy = ctx.Enemies[0];
        await CreatureCmd.SetMaxHp(enemy, 999);
        await CreatureCmd.SetCurrentHp(enemy, 999);

        await ctx.PlayCard(await ctx.AddCardToHand<StrikeIronclad>(), enemy);
        var normal = 999 - enemy.CurrentHp;
        Assert.IsTrue(normal > 0, "Strike did nothing.");

        await CreatureCmd.SetCurrentHp(enemy, 999);
        await PowerCmd.Apply<FlightPower>(new BlockingPlayerChoiceContext(), enemy, 3, enemy, null);
        await ctx.PlayCard(await ctx.AddCardToHand<StrikeIronclad>(), enemy);
        var withFlight = 999 - enemy.CurrentHp;

        Assert.AreEqual((int)Math.Floor(normal * 0.5m), withFlight, "Flight should halve attack damage.");
    }

    /// <summary>Covers the AfterDeath signature difference between 0.111.1 and 0.113.x.</summary>
    [AftpTest(typeof(ThreeCultistsNormal))] // needs a second enemy: powers cannot be applied once the last enemy dies and the combat ends
    public async Task SporeCloudAppliesVulnerableOnDeath(TestContext ctx)
    {
        var enemy = ctx.Enemies[0];
        await PowerCmd.Apply<SporeCloudPower>(new BlockingPlayerChoiceContext(), enemy, 2, enemy, null);
        Assert.IsTrue(!ctx.Player.Creature.HasPower<VulnerablePower>(), "Player starts vulnerable.");

        await CreatureCmd.Kill(enemy, true);
        Assert.IsTrue(enemy.IsDead, "Kill did not kill the enemy.");

        Assert.AreEqual(2, ctx.Player.Creature.GetPower<VulnerablePower>()?.Amount ?? 0,
            "Spore Cloud should apply Vulnerable to the player when its owner dies.");
    }

    /// <summary>Every encounter of ours has to start, spawn its monsters and survive a few enemy turns.</summary>
    [AftpPoolTest]
    public IEnumerable<AftpTestCase> PlayEveryEncounter() =>
        ModTypes<EncounterModel>().OrderBy(t => t.Name).Select(type => new AftpTestCase(type.Name, type, async ctx =>
        {
            Assert.IsTrue(ctx.Enemies.Count > 0, "Encounter spawned no monsters.");
            for (var i = 0; i < 3 && MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress; i++)
                await ctx.EndTurnAndWait();
        }));
}
