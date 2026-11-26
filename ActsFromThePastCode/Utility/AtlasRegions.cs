using Godot;

namespace ActsFromThePast;

/// <summary>
///     The regions of the mod's texture atlases. Each region is an AtlasTexture resource,
///     <c>atlas/&lt;name&gt;/&lt;region&gt;.tres</c>, and Godot loads and caches
///     it like any other resource.
/// </summary>
public static class AtlasRegions
{
    public const string Vfx = "res://ActsFromThePast/atlas/vfx";
    public const string CardUi = "res://ActsFromThePast/atlas/cardui";

    /// <summary>The atlas that holds an act's event images, by act index.</summary>
    public static string ForAct(int actIndex)
    {
        return actIndex switch
        {
            0 => "res://ActsFromThePast/atlas/exordium",
            1 => "res://ActsFromThePast/atlas/city",
            _ => "res://ActsFromThePast/atlas/beyond"
        };
    }

    /// <summary>The page texture and the rectangle of the region on it, or null if there is no such region.</summary>
    public static TextureRegion? GetRegion(string atlas, string regionName)
    {
        var texture = ResourceLoader.Load<AtlasTexture>($"{atlas}/{regionName}.tres");
        if (texture?.Atlas == null) return null;

        return new TextureRegion { Texture = texture.Atlas, Region = texture.Region };
    }

    public struct TextureRegion
    {
        public Texture2D Texture;
        public Rect2 Region;
    }
}
