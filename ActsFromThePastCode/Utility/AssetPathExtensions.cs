using System.Collections.Concurrent;
using BaseLib.Extensions;
using Godot;

namespace ActsFromThePast;

/// <summary>
///     Resolves asset paths from a model's <c>Id.Entry</c>, so models don't have to spell them out. The mod prefix of the id is dropped,
///     because the folder already namespaces the files:
///     <c>Id.Entry.BigPowerImagePath()</c> gives <c>res://ActsFromThePast/images/powers/angry_power.png</c>.
///     Every method returns <c>null</c> when the file doesn't exist, so the base game's default lookup applies instead.
/// </summary>
public static class AssetPathExtensions
{
    private const string Root = "res://ActsFromThePast";

    // Only hits are cached: a miss may just mean the pck isn't mounted yet.
    private static readonly ConcurrentDictionary<string, bool> Found = new();

    private static string? Resolve(string folder, string name, string extension)
    {
        var path = $"{Root}/{folder}/{name.RemovePrefix().ToLowerInvariant()}{extension}";
        if (Found.ContainsKey(path))
            return path;
        if (!ResourceLoader.Exists(path))
            return null;
        Found[path] = true;
        return path;
    }

    // Atlas sprites and big icons are generated from ImageGen/images by `dotnet run --project ImageGen`.
    public static string? PowerAtlasPath(this string entry)
    {
        return Resolve("images/atlases/power_atlas.sprites", entry, ".tres");
    }

    public static string? BigPowerImagePath(this string entry)
    {
        return Resolve("images/powers", entry, ".png");
    }

    public static string? RelicAtlasPath(this string entry)
    {
        return Resolve("images/atlases/relic_atlas.sprites", entry, ".tres");
    }

    public static string? RelicOutlineAtlasPath(this string entry)
    {
        return Resolve("images/atlases/relic_atlas.sprites", entry + "_outline", ".tres");
    }

    public static string? BigRelicImagePath(this string entry)
    {
        return Resolve("images/relics", entry, ".png");
    }

    public static string? EventPortraitPath(this string entry)
    {
        return Resolve("images/events", entry, ".png");
    }

    public static string? CardPortraitPath(this string entry)
    {
        return Resolve("images/atlases/card_atlas.sprites", entry, ".tres");
    }

    public static string? EncounterScenePath(this string entry)
    {
        return Resolve("scenes/encounters", entry, ".tscn");
    }

    public static string? RunHistoryIconPath(this string entry)
    {
        return Resolve("images/ui/run_history", entry, ".png");
    }

    public static string? RunHistoryIconOutlinePath(this string entry)
    {
        return Resolve("images/ui/run_history", entry + "_outline", ".png");
    }
}
