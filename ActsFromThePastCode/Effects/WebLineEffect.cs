using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public partial class WebLineEffect : NSts1Effect
{
    private const string TexturePath = "res://ActsFromThePast/vfx/horizontal_line.png";
    private const float EffectDuration = 1.0f;
    private float _baseScale;
    private Color _color;

    private Sprite2D _sprite = null!;

    public static WebLineEffect Create(Vector2 position, bool facingLeft)
    {
        var effect = new WebLineEffect();

        var xOffset = (float)GD.RandRange(-20f, 20f);
        var yOffset = (float)GD.RandRange(-10f, 10f);
        effect.Position = position + new Vector2(xOffset, yOffset);

        effect.RotationDegrees = facingLeft
            ? (float)GD.RandRange(175f, 190f)
            : (float)GD.RandRange(175f, 190f) + 180f;

        effect._baseScale = (float)GD.RandRange(0.8f, 1.0f);

        var g = (float)GD.RandRange(0.6f, 0.9f);
        effect._color = new Color(g, g, g + 0.1f, 0f);

        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = EffectDuration;
        StartingDuration = EffectDuration;

        var texture = GD.Load<Texture2D>(TexturePath);
        if (texture == null)
        {
            Log.Error("[WebLineEffect] Failed to load texture: " + TexturePath);
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = texture;
        _sprite.Centered = false;
        // Pivot at left-center of 256x256 texture so rotation happens around the source point
        _sprite.Offset = new Vector2(0, -128);
        _sprite.Material = CreateAdditiveMaterial();
        AddChild(_sprite);

        UpdateSprite();
    }

    protected override void Update(float delta)
    {
        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        var halfDuration = StartingDuration / 2f;
        if (Duration > halfDuration)
        {
            var t = (StartingDuration - Duration) / halfDuration;
            _color.A = Lerp(0.01f, 0.8f, EaseOut(t));
        }
        else
        {
            var t = Duration / halfDuration;
            _color.A = Lerp(0.01f, 0.8f, EaseOut(t));
        }

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        var wobble = Mathf.Cos(Duration * 16f) / 4f + 1.5f;
        var scaleX = _baseScale * 2f * wobble;
        var scaleY = _baseScale;
        _sprite.Scale = new Vector2(scaleX, scaleY);
        _sprite.Modulate = new Color(1f, 1f, 1f, _color.A);
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}