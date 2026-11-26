using MegaCrit.Sts2.Core.Random;

public static class RngExtensions
{
    /// <summary>
    ///     Picks a random item and throws if the collection is empty, for call sites that have
    ///     already established that it cannot be.
    /// </summary>
    public static T NextRequired<T>(this Rng rng, IEnumerable<T> items) where T : class
    {
        return rng.NextItem(items) ?? throw new InvalidOperationException("Cannot pick from an empty collection.");
    }
}
