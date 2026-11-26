using Godot;

namespace ActsFromThePast;

public partial class TimeWarpTurnEndEffect : NSts1Effect
{
    // The Time Warp power's own icon, drawn large
    private const string IconPath = "res://ActsFromThePast/images/powers/time_warp_power.png";
    private Color _color;
    private float _scale;
    private Sprite2D _sprite = null!;
    private float _x;
    private float _y;

    public static TimeWarpTurnEndEffect Create()
    {
        var effect = new TimeWarpTurnEndEffect();
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 2.0f;
        StartingDuration = 2.0f;

        var icon = GD.Load<Texture2D>(IconPath);
        if (icon == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D { Texture = icon, Centered = true };

        AddChild(_sprite);

        var viewport = GetViewport();
        var viewportSize = viewport?.GetVisibleRect().Size ?? new Vector2(1920, 1080);

        _scale = 1.0f;
        _color = new Color(1f, 1f, 1f);

        _x = viewportSize.X * 0.5f;

        _y = viewportSize.Y + _sprite.Texture.GetHeight() / 2f;

        Position = new Vector2(_x, _y);
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

        if (Duration < 1.0f)
        {
            var t = Duration;
            var fade = t * t * t * (t * (t * 6f - 15f) + 10f);
            _color.A = fade;
        }
        else
        {
            var t = Mathf.Clamp(Duration - 1.0f, 0f, 1f);
            var swingIn = t * t * ((2.70158f + 1f) * t - 2.70158f);
            var viewport = GetViewport();
            var viewportSize = viewport?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            var regionHeight = _sprite.Texture.GetHeight();

            _y = Mathf.Lerp(viewportSize.Y + regionHeight / 2f, viewportSize.Y * 0.5f, 1.0f - swingIn);
        }

        _sprite.Rotation = Duration * Mathf.Pi * 2f;

        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Modulate = _color;
    }
}