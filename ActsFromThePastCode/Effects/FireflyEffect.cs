using Godot;

namespace ActsFromThePast;

public partial class FireFlyEffect : NSts1Effect
{
    private const string AtlasPath = AtlasRegions.Vfx;
    private const float TrailTime = 0.04f;
    private const int TrailMaxAmt = 30;
    private readonly List<Vector2> _prevPositions = [];
    private readonly List<Sprite2D> _trailSprites = [];
    private float _aX;
    private float _baseAlpha;
    private float _scale;

    private Color _setColor;

    private Sprite2D _sprite = null!;
    private float _trailTimer;
    private float _vX;
    private float _vY;
    private float _waveDst;

    private float _x;
    private float _y;

    public static FireFlyEffect Create(Color color)
    {
        var effect = new FireFlyEffect();
        effect._setColor = color;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        StartingDuration = (float)GD.RandRange(6.0, 14.0);
        Duration = StartingDuration;

        var region = AtlasRegions.GetRegion(AtlasPath, "combat/blurDot");
        if (region == null)
        {
            IsDone = true;
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = region.Value.Texture;
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = region.Value.Region;
        _sprite.Centered = true;
        _sprite.Material = CreateAdditiveMaterial();
        AddChild(_sprite);

        var imgWidth = region.Value.Region.Size.X;
        var imgHeight = region.Value.Region.Size.Y;

        const float floorY = 800f;

        // Random starting position
        _x = GD.RandRange(0, 1920) - imgWidth / 2f;

        // In StS1: y ranged from (floorY - 100) to (floorY + 400) in bottom-up coords
        // In Godot (top-down): subtract offset to go above the floor
        var yOffset = (float)GD.RandRange(-100.0, 400.0);
        _y = floorY - yOffset - imgHeight / 2f;

        _vX = (float)GD.RandRange(18.0, 90.0);
        _aX = (float)GD.RandRange(-5.0, 5.0);
        _waveDst = _vX * (float)GD.RandRange(0.03, 0.07);
        _scale = _vX / 60f;

        if (GD.Randf() > 0.5f)
            _vX = -_vX;

        // Invert vY for Godot's coordinate system
        _vY = -(float)GD.RandRange(-36.0, 36.0);

        EffectColor = _setColor;
        _baseAlpha = 0.25f;
        EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, 0f);

        UpdateSprite();
    }

    protected override void Update(float delta)
    {
        // Trail management
        _trailTimer -= delta;
        if (_trailTimer < 0f)
        {
            _trailTimer = TrailTime;
            _prevPositions.Add(new Vector2(_x, _y));

            // Create trail sprite
            var region = AtlasRegions.GetRegion(AtlasPath, "combat/blurDot");
            if (region != null)
            {
                var trailSprite = new Sprite2D();
                trailSprite.Texture = region.Value.Texture;
                trailSprite.RegionEnabled = true;
                trailSprite.RegionRect = region.Value.Region;
                trailSprite.Centered = true;
                trailSprite.Material = CreateAdditiveMaterial();
                AddChild(trailSprite);
                _trailSprites.Add(trailSprite);
            }

            if (_prevPositions.Count > TrailMaxAmt)
            {
                _prevPositions.RemoveAt(0);
                if (_trailSprites.Count > 0)
                {
                    _trailSprites[0].QueueFree();
                    _trailSprites.RemoveAt(0);
                }
            }
        }

        Duration -= delta;

        // Movement
        _x += _vX * delta;
        _vX += _aX * delta;

        // Check if off screen
        if (_prevPositions.Count > 0 && (_prevPositions[0].X < 0f || _prevPositions[0].X > 1920f))
            IsDone = true;

        _y += _vY * delta;
        _y -= Mathf.Sin(Duration * _waveDst) * _waveDst / 4f * delta * 60f;

        if (Duration < 0f)
            IsDone = true;

        // Alpha fade in/out
        float alpha;
        if (Duration > StartingDuration / 2f)
        {
            var tmp = Duration - StartingDuration / 2f;
            alpha = EaseOut(1f - tmp / (StartingDuration / 2f)) * _baseAlpha;
        }
        else
        {
            alpha = EaseOut(Duration / StartingDuration * 2f) * _baseAlpha;
        }

        EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, alpha);

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        _sprite.GlobalPosition = new Vector2(_x, _y);
        _sprite.Modulate = EffectColor;

        var jitterScale = _scale * (float)GD.RandRange(2.5, 3.0);
        _sprite.Scale = new Vector2(jitterScale, jitterScale);

        // Update trail sprites
        var trailAlpha = EffectColor.A;
        for (var i = _trailSprites.Count - 1; i >= 0; i--)
        {
            trailAlpha *= 0.95f;
            var trailColor = new Color(_setColor.R, _setColor.G, _setColor.B, trailAlpha);
            _trailSprites[i].Modulate = trailColor;
            _trailSprites[i].GlobalPosition = _prevPositions[i];

            var trailScale = _scale * (i + 5f) / _prevPositions.Count;
            _trailSprites[i].Scale = new Vector2(trailScale, trailScale);
        }
    }

    private static CanvasItemMaterial CreateAdditiveMaterial()
    {
        var material = new CanvasItemMaterial();
        material.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        return material;
    }
}