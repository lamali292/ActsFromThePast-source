using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Tests;

public sealed class TestContext(CombatState combat, Player player)
{
    public CombatState Combat { get; } = combat;
    public Player Player { get; } = player;

    public IReadOnlyList<Creature> Enemies => Combat.Enemies.ToList();

    public async Task<CardModel> AddCardToHand<T>() where T : CardModel
    {
        var card = Combat.CreateCard(ModelDb.Card<T>(), Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Player);
        return card;
    }

    public Task PlayCard(CardModel card, Creature? target = null) =>
        CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, target);

    /// <summary>Ends the player's turn and waits until the enemies are done and the player can act again (or the combat ended).</summary>
    public async Task EndTurnAndWait()
    {
        PlayerCmd.EndTurn(Player, false);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // The turn has to leave the Play phase first, otherwise the loop below would return immediately.
        while (Player.PlayerCombatState?.Phase == PlayerTurnPhase.Play && sw.Elapsed < TimeSpan.FromSeconds(5))
            await Task.Yield();
        while (CombatManager.Instance.IsInProgress && Player.PlayerCombatState?.Phase != PlayerTurnPhase.Play
                                                   && sw.Elapsed < TimeSpan.FromSeconds(10))
            await Task.Yield();
        Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(10), "Enemy turn never finished.");
    }
}
