using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheBeyond;

public partial class TheBeyondBackground : NCombatBackground
{

    // Background layers
    private TextureRect _bg1 = null!, _bg1Glow = null!;
    private TextureRect _bg2 = null!, _bg2Glow = null!;

    // Columns
    private TextureRect _c1 = null!, _c1Glow = null!;
    private TextureRect _c2 = null!, _c2Glow = null!;
    private TextureRect _c3 = null!, _c3Glow = null!;
    private TextureRect _c4 = null!, _c4Glow = null!;
    private TextureRect _ceiling = null!;
    private ColumnConfig _columnConfig = ColumnConfig.Open;

    // Floaters
    private TextureRect _f1 = null!, _f2 = null!, _f3 = null!, _f4 = null!, _f5 = null!;

    // Store original positions for animation offsets
    private Vector2 _f1Base, _f2Base, _f3Base, _f4Base, _f5Base;
    private TextureRect _fg = null!;
    private TextureRect _floor = null!;

    // Ice
    private TextureRect _i1 = null!, _i2 = null!, _i3 = null!, _i4 = null!, _i5 = null!;

    private bool _initialized;

    // Midground
    private TextureRect _mg1 = null!, _mg1Glow = null!;
    private TextureRect _mg2 = null!, _mg2Glow = null!;
    private TextureRect _mg3 = null!, _mg3Glow = null!;
    private TextureRect _mg4 = null!, _mg4Glow = null!;

    // Colors
    private Color _overlayColor;
    private float _overlayGlowAlpha; // the low alpha used for the additive pass

    // Render flags
    private bool _renderAltBg;
    private bool _renderF1, _renderF2, _renderF3, _renderF4, _renderF5;
    private bool _renderI1, _renderI2, _renderI3, _renderI4, _renderI5;
    private bool _renderIce;
    private bool _renderM1, _renderM2, _renderM3, _renderM4;
    private bool _renderS1, _renderS2, _renderS3, _renderS4, _renderS5;
    private bool _renderStalactites;

    // Stalactites
    private TextureRect _s1 = null!, _s1Glow = null!;
    private TextureRect _s2 = null!, _s2Glow = null!;
    private TextureRect _s3 = null!, _s3Glow = null!;
    private TextureRect _s4 = null!, _s4Glow = null!;
    private TextureRect _s5 = null!, _s5Glow = null!;

    public override void _Ready()
    {
        base._Ready();
        Initialize();
        SetUpInCombatRoom();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Z-index plan (back to front):
        // -60s: bg, floor, ceiling
        // -50s: bg glow pass, mg, mg glow pass
        // -40s: columns, column glow pass, stalactites, stalactite glow pass
        // -30s: ice
        // -20s: floaters
        // -10s: fg

        _floor = Layer("Floor");
        _ceiling = Layer("Ceiling");
        _bg1 = Layer("Bg1");
        _bg2 = Layer("Bg2");

        // Midground (rendered before columns in original)
        _mg2 = Layer("Mg2");
        _mg1 = Layer("Mg1");
        _mg3 = Layer("Mg3");
        _mg4 = Layer("Mg4");

        // Columns
        _c1 = Layer("C1");
        _c2 = Layer("C2");
        _c3 = Layer("C3");
        _c4 = Layer("C4");

        // Stalactites
        _s1 = Layer("S1");
        _s2 = Layer("S2");
        _s3 = Layer("S3");
        _s4 = Layer("S4");
        _s5 = Layer("S5");

        // Additive glow duplicates (same textures, additive blend)
        _bg1Glow = Layer("Bg1Glow");
        _bg2Glow = Layer("Bg2Glow");
        _mg2Glow = Layer("Mg2Glow");
        _mg1Glow = Layer("Mg1Glow");
        _mg3Glow = Layer("Mg3Glow");
        _mg4Glow = Layer("Mg4Glow");
        _c1Glow = Layer("C1Glow");
        _c2Glow = Layer("C2Glow");
        _c3Glow = Layer("C3Glow");
        _c4Glow = Layer("C4Glow");
        _s1Glow = Layer("S1Glow");
        _s2Glow = Layer("S2Glow");
        _s3Glow = Layer("S3Glow");
        _s4Glow = Layer("S4Glow");
        _s5Glow = Layer("S5Glow");

        // Ice (rendered after glow pass in original, with full-alpha overlay)
        _i1 = Layer("I1");
        _i2 = Layer("I2");
        _i3 = Layer("I3");
        _i4 = Layer("I4");
        _i5 = Layer("I5");

        // Floaters (rendered after ice, with tmpColor and animation)
        _f1 = Layer("F1");
        _f2 = Layer("F2");
        _f3 = Layer("F3");
        _f4 = Layer("F4");
        _f5 = Layer("F5");

        // Foreground
        _fg = Layer("Fg");


        // Store base positions for floater animation
        _f1Base = _f1.Position;
        _f2Base = _f2.Position;
        _f3Base = _f3.Position;
        _f4Base = _f4.Position;
        _f5Base = _f5.Position;

        RandomizeScene();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().ProcessFrame += OnProcessFrame;
    }

    public override void _ExitTree()
    {
        GetTree().ProcessFrame -= OnProcessFrame;
        base._ExitTree();
    }

    private void OnProcessFrame()
    {
        if (!_initialized || !IsInsideTree()) return;
        UpdateFloaterAnimations();
    }

    private void UpdateFloaterAnimations()
    {
        var ms = Time.GetTicksMsec();
        var scale = 1.0f;

        if (_renderF1)
        {
            var offsetX = Mathf.Cos(Mathf.DegToRad((ms + 180) / 180 % 360)) * 40f * scale;
            var offsetY = Mathf.Cos(Mathf.DegToRad((ms + 500) / 72 % 360)) * 20f * scale;
            var rotDeg = Mathf.Cos(Mathf.DegToRad((ms + 180) / 180 % 360));
            _f1.Position = _f1Base + new Vector2(offsetX, offsetY);
            _f1.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF2)
        {
            var offsetX = Mathf.Cos(Mathf.DegToRad((ms + 91723) / 72 % 360)) * 20f;
            var rotDeg = (float)(ms / 120 % 360);
            _f2.Position = _f2Base + new Vector2(offsetX, 0);
            _f2.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF3)
        {
            var offsetX = -80f * scale;
            var offsetY = Mathf.Cos(Mathf.DegToRad((ms + 73) / 2000 % 360)) * 10f - 90f * scale;
            var rotDeg = ms / 1000 % 360 * 2f;
            _f3.Position = _f3Base + new Vector2(offsetX, offsetY);
            _f3.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF4)
        {
            var offsetY = Mathf.Cos(Mathf.DegToRad((ms + 4442) / 20 % 360)) * 30f * scale;
            var rotDeg = Mathf.Cos(Mathf.DegToRad((ms + 4442) / 10 % 360)) * 20f;
            _f4.Position = _f4Base + new Vector2(0, offsetY);
            _f4.Rotation = Mathf.DegToRad(rotDeg);
        }

        if (_renderF5)
        {
            var offsetY = Mathf.Cos(Mathf.DegToRad(ms / 48 % 360)) * 20f;
            _f5.Position = _f5Base + new Vector2(0, offsetY);
            _f5.Rotation = 0;
        }
    }

    // The layers are authored in the scene, scenes/backgrounds/the_beyond_act.
    private TextureRect Layer(string name)
    {
        return GetNode<TextureRect>("%" + name);
    }

    public void RandomizeScene()
    {
        _overlayColor = new Color(
            (float)GD.RandRange(0.7, 0.9),
            (float)GD.RandRange(0.7, 0.9),
            (float)GD.RandRange(0.7, 1.0)
        );
        _overlayGlowAlpha = (float)GD.RandRange(0.0, 0.2);

        _renderAltBg = GD.Randf() < 0.2f;

        _renderM1 = false;
        _renderM2 = false;
        _renderM3 = false;
        _renderM4 = false;
        if (!_renderAltBg && GD.Randf() < 0.8f)
        {
            _renderM1 = GD.Randf() > 0.5f;
            _renderM2 = GD.Randf() > 0.5f;
            _renderM3 = GD.Randf() > 0.5f;
            if (!_renderM3)
                _renderM4 = GD.Randf() > 0.5f;
        }

        // Column config
        if (GD.Randf() < 0.6f)
            _columnConfig = ColumnConfig.Open;
        else if (GD.Randf() > 0.5f)
            _columnConfig = ColumnConfig.SmallOnly;
        else if (GD.Randf() > 0.5f)
            _columnConfig = ColumnConfig.SmallPlusLeft;
        else
            _columnConfig = ColumnConfig.SmallPlusRight;

        // Floaters (max 2, each 25% chance, 30% chance of none)
        _renderF1 = false;
        _renderF2 = false;
        _renderF3 = false;
        _renderF4 = false;
        _renderF5 = false;

        var floaterCount = 0;
        _renderF1 = GD.Randf() < 0.25f;
        if (_renderF1) floaterCount++;
        _renderF2 = GD.Randf() < 0.25f;
        if (_renderF2) floaterCount++;
        if (floaterCount < 2)
        {
            _renderF3 = GD.Randf() < 0.25f;
            if (_renderF3) floaterCount++;
        }

        if (floaterCount < 2)
        {
            _renderF4 = GD.Randf() < 0.25f;
            if (_renderF4) floaterCount++;
        }

        if (floaterCount < 2)
            _renderF5 = GD.Randf() < 0.25f;

        // 30% chance to disable all floaters
        if (GD.Randf() < 0.3f)
        {
            _renderF1 = false;
            _renderF2 = false;
            _renderF3 = false;
            _renderF4 = false;
            _renderF5 = false;
        }

        // Ice
        _renderIce = GD.Randf() > 0.5f;
        if (_renderIce)
        {
            _renderI1 = GD.Randf() > 0.5f;
            _renderI2 = GD.Randf() > 0.5f;
            _renderI3 = GD.Randf() > 0.5f;
            _renderI4 = GD.Randf() > 0.5f;
            _renderI5 = GD.Randf() > 0.5f;
        }
        else
        {
            _renderI1 = false;
            _renderI2 = false;
            _renderI3 = false;
            _renderI4 = false;
            _renderI5 = false;
        }

        // Stalactites
        _renderStalactites = GD.Randf() > 0.5f;
        if (_renderStalactites)
        {
            _renderS1 = GD.Randf() > 0.5f;
            _renderS2 = GD.Randf() > 0.5f;
            _renderS3 = GD.Randf() > 0.5f;
            _renderS4 = GD.Randf() > 0.5f;
            _renderS5 = GD.Randf() > 0.5f;
        }
        else
        {
            _renderS1 = false;
            _renderS2 = false;
            _renderS3 = false;
            _renderS4 = false;
            _renderS5 = false;
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        // Compute tmpColor: halfway between overlay and white
        var tmpColor = new Color(
            (1f + _overlayColor.R) / 2f,
            (1f + _overlayColor.G) / 2f,
            (1f + _overlayColor.B) / 2f
        );

        // Glow color: overlay color at low alpha
        var glowColor = new Color(_overlayColor.R, _overlayColor.G, _overlayColor.B, _overlayGlowAlpha);

        // Normal pass: full-alpha overlay color
        _floor.Modulate = _overlayColor;
        _ceiling.Modulate = _overlayColor;
        _bg1.Modulate = _overlayColor;
        _bg2.Modulate = _overlayColor;
        _mg1.Modulate = _overlayColor;
        _mg2.Modulate = _overlayColor;
        _mg3.Modulate = _overlayColor;
        _mg4.Modulate = _overlayColor;
        _c1.Modulate = _overlayColor;
        _c2.Modulate = _overlayColor;
        _c3.Modulate = _overlayColor;
        _c4.Modulate = _overlayColor;
        _s1.Modulate = _overlayColor;
        _s2.Modulate = _overlayColor;
        _s3.Modulate = _overlayColor;
        _s4.Modulate = _overlayColor;
        _s5.Modulate = _overlayColor;

        // Glow pass: additive layers at low alpha
        _bg1Glow.Modulate = glowColor;
        _bg2Glow.Modulate = glowColor;
        _mg1Glow.Modulate = glowColor;
        _mg2Glow.Modulate = glowColor;
        _mg3Glow.Modulate = glowColor;
        _mg4Glow.Modulate = glowColor;
        _c1Glow.Modulate = glowColor;
        _c2Glow.Modulate = glowColor;
        _c3Glow.Modulate = glowColor;
        _c4Glow.Modulate = glowColor;
        _s1Glow.Modulate = glowColor;
        _s2Glow.Modulate = glowColor;
        _s3Glow.Modulate = glowColor;
        _s4Glow.Modulate = glowColor;
        _s5Glow.Modulate = glowColor;

        // Ice: full-alpha overlay
        _i1.Modulate = _overlayColor;
        _i2.Modulate = _overlayColor;
        _i3.Modulate = _overlayColor;
        _i4.Modulate = _overlayColor;
        _i5.Modulate = _overlayColor;

        // Floaters and fg: tmpColor
        _f1.Modulate = tmpColor;
        _f2.Modulate = tmpColor;
        _f3.Modulate = tmpColor;
        _f4.Modulate = tmpColor;
        _f5.Modulate = tmpColor;
        _fg.Modulate = tmpColor;

        // Visibility
        _bg2.Visible = _renderAltBg;
        _bg2Glow.Visible = _renderAltBg;
        _mg1.Visible = _renderM1;
        _mg1Glow.Visible = _renderM1;
        _mg2.Visible = _renderM2;
        _mg2Glow.Visible = _renderM2;
        _mg3.Visible = _renderM3;
        _mg3Glow.Visible = _renderM3;
        _mg4.Visible = _renderM4;
        _mg4Glow.Visible = _renderM4;

        // Columns
        var showC1 = _columnConfig != ColumnConfig.Open;
        var showC2 = _columnConfig == ColumnConfig.SmallPlusLeft;
        var showC3 = _columnConfig == ColumnConfig.SmallPlusRight;
        var showC4 = _columnConfig != ColumnConfig.Open;

        _c1.Visible = showC1;
        _c1Glow.Visible = showC1;
        _c2.Visible = showC2;
        _c2Glow.Visible = showC2;
        _c3.Visible = showC3;
        _c3Glow.Visible = showC3;
        _c4.Visible = showC4;
        _c4Glow.Visible = showC4;

        // Stalactites
        _s1.Visible = _renderS1;
        _s1Glow.Visible = _renderS1;
        _s2.Visible = _renderS2;
        _s2Glow.Visible = _renderS2;
        _s3.Visible = _renderS3;
        _s3Glow.Visible = _renderS3;
        _s4.Visible = _renderS4;
        _s4Glow.Visible = _renderS4;
        _s5.Visible = _renderS5;
        _s5Glow.Visible = _renderS5;

        // Ice
        _i1.Visible = _renderI1;
        _i2.Visible = _renderI2;
        _i3.Visible = _renderI3;
        _i4.Visible = _renderI4;
        _i5.Visible = _renderI5;

        // Floaters
        _f1.Visible = _renderF1;
        _f2.Visible = _renderF2;
        _f3.Visible = _renderF3;
        _f4.Visible = _renderF4;
        _f5.Visible = _renderF5;
    }

    // Needs the combat room around it, so it runs once the scene has been added to it.
    private void SetUpInCombatRoom()
    {
        Scale = LegacyBackgroundScaling.Oversize;

        var parent = GetParent();
        while (parent != null && parent is not NCombatRoom)
            parent = parent.GetParent();

        if (parent is NCombatRoom combatRoom)
        {
            LegacyBackgroundScaling.ApplyContainerScale(combatRoom);
            var allyContainer = combatRoom.GetNodeOrNull<Control>("%AllyContainer");
            var enemyContainer = combatRoom.GetNodeOrNull<Control>("%EnemyContainer");

            allyContainer?.Position += Vector2.Down * 30f;
            enemyContainer?.Position += Vector2.Down * 30f;
        }
    }

    public void SetBossMode()
    {
        // Beyond boss fights don't have special scene elements like The City's throne,
        // but the original still silences BGM in nextRoom for boss rooms.
        // Add any boss-specific visual overrides here if needed.
    }

    private enum ColumnConfig
    {
        Open,
        SmallOnly,
        SmallPlusLeft,
        SmallPlusRight
    }
}