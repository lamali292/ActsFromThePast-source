using MegaCrit.Sts2.Core.Entities.Players;

namespace ActsFromThePast;

public static class DarvOfferTracker
{
    public static HashSet<string> GetPreviouslyOfferedTitles(Player owner)
    {
        var result = new HashSet<string>();

        foreach (var actHistory in owner.RunState.MapPointHistory)
        {
            foreach (var entry in actHistory)
            {
                var playerEntry = entry.PlayerStats
                    .FirstOrDefault(p => p.PlayerId == owner.NetId);

                if (playerEntry?.AncientChoices == null || playerEntry.AncientChoices.Count == 0)
                    continue;

                foreach (var choice in playerEntry.AncientChoices)
                    result.Add(choice.Title.GetFormattedText());
            }
        }

        return result;
    }
}