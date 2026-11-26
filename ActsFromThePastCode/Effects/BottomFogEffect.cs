using Godot;

namespace ActsFromThePast;

public partial class BottomFogEffect : NSts1Effect
{
    private const string AtlasPath = AtlasRegions.Vfx;

    private static readonly string[] SmokeRegions =
    [
        "env/smoke1", "env/smoke2", "env/smoke3"
    ];

    private float _angularVelocity;
    private Color _color;
    private bool _flipX;
    private bool _flipY;
    private float _rotation;
    private float _scale;

    private Sprite2D _sprite = null!;
    private float _vX;

    public static BottomFogEffect Create()
    {
        var effect = new BottomFogEffect();
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        StartingDuration = (float)GD.RandRange(10.0, 12.0);
        Duration = StartingDuration;

        var regionName = SmokeRegions[Random.Shared.Next(SmokeRegions.Length)];
        var textureRegion = AtlasRegions.GetRegion(AtlasPath, regionName);
        if (textureRegion == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = textureRegion.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = textureRegion.Value.Region;
        _sprite.Centered = true;
        AddChild(_sprite);

        var imgWidth = textureRegion.Value.Region.Size.X;
        var imgHeight = textureRegion.Value.Region.Size.Y;


        var x = GD.RandRange(-1160, 1160) - imgWidth / 2f;
        var y = -(float)GD.RandRange(60, 410) - imgHeight / 2f;

        Position = new Vector2(x, y);

        _vX = GD.RandRange(-200, 200);
        _angularVelocity = GD.RandRange(-10, 10);

        _scale = (float)GD.RandRange(4.0, 6.0);
        _rotation = GD.RandRange(0, 360);

        _flipX = GD.Randf() > 0.5f;
        _flipY = GD.Randf() > 0.5f;

        var tmp = (float)GD.RandRange(0.1, 0.15);
        var r = tmp + (float)GD.RandRange(0, 0.1);
        var g = tmp;
        var b = r + (float)GD.RandRange(0, 0.05);
        _color = new Color(r, g, b, 0f);

        UpdateSprite();
    }

    protected override void Update(float delta)
    {
        Position += new Vector2(_vX * delta, 0);
        _rotation += _angularVelocity * delta;
        _scale += delta / 3f;
        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        var elapsed = StartingDuration - Duration;
        if (elapsed < 5f)
            _color.A = Fade(elapsed / 5f) * 0.3f;
        else if (Duration < 5f)
            _color.A = Fade(Duration / 5f) * 0.3f;
        else
            _color.A = 0.3f;

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;

        _sprite.RotationDegrees = _rotation;
        _sprite.Scale = new Vector2(
            _flipX ? -_scale : _scale,
            _flipY ? -_scale : _scale
        );
        _sprite.Modulate = _color;
    }

    private static float Fade(float t)
    {
        return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
    }
}