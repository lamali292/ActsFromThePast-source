using Godot;

namespace ActsFromThePast;

public partial class NemesisFireParticle : NSts1Effect
{
    private const string AtlasPath = AtlasRegions.Vfx;

    private static readonly string[] FireRegions =
    [
        "env/fire1", "env/fire2", "env/fire3"
    ];

    private Color _color;
    private float _rotation;
    private float _scale;

    private Sprite2D _sprite = null!;
    private float _vY;
    private float _x;
    private float _y;

    public static NemesisFireParticle Create(Vector2 position)
    {
        var effect = new NemesisFireParticle();
        effect._x = position.X;
        effect._y = position.Y;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        var duration = (float)GD.RandRange(0.5, 1.0);
        Duration = duration;
        StartingDuration = duration;

        var vY = (float)GD.RandRange(1.0, 10.0);
        _vY = -(vY * vY);

        _scale = (float)GD.RandRange(0.25, 0.5);
        _rotation = (float)GD.RandRange(-20.0, 20.0);
        _color = new Color(0.1f, 0.2f, 0.1f, 0.01f);

        var regionName = FireRegions[Random.Shared.Next(FireRegions.Length)];
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
        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    protected override void Update(float delta)
    {
        _y += _vY * delta;
        Duration -= delta;

        if (Duration < 0f)
        {
            IsDone = true;
            return;
        }

        var t = Duration / StartingDuration;
        var fade = t * t * t * (t * (t * 6f - 15f) + 10f);
        _color.A = fade;

        Position = new Vector2(_x, _y);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_sprite == null) return;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Rotation = _rotation * (Mathf.Pi / 180f);
        _sprite.Modulate = _color;
    }
}