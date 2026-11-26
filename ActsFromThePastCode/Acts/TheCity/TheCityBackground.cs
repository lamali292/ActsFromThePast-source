using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheCity;

public partial class TheCityBackground : NCombatBackground
{
    private readonly List<NSts1Effect> _ceilingDustEffects = [];

    // Effects
    private readonly List<FireFlyEffect> _fireFlies = [];

// Background layers
    private TextureRect _bg = null!;
    private TextureRect _bg2 = null!;
    private TextureRect _bg2Glow = null!;
    private TextureRect _bgGlow = null!;
    private TextureRect _bgGlow2 = null!;
    private TextureRect _bgGlow2Double = null!; // NEW: double render for dark day
    private bool _blueFlies;
    private TextureRect _ceiling = null!;
    private float _ceilingDustTimer = 1.0f;
    private TextureRect _chains = null!;
    private TextureRect _chainsGlow = null!;
    private TextureRect _chainsGlow2 = null!; // NEW: double render for chains
    private bool _darkDay;
    private TextureRect _fg = null!;
    private TextureRect _fg2 = null!;
    private TextureRect _fgGlow = null!;
    private TextureRect _floor = null!;
    private bool _hasFlies;

    private bool _initialized;
    private TextureRect _mg = null!;
    private TextureRect _mgAlt = null!; // RENAMED: was _mg2
    private TextureRect _mgGlow = null!;
    private TextureRect _mgGlow2 = null!; // NEW: double render for mg glow

    // Colors
    private Color _overlayColor = Colors.White;
    private ColorRect _overlayRect = null!;

    // Pillars
    private TextureRect _pillar1 = null!;
    private TextureRect _pillar2 = null!;
    private TextureRect _pillar3 = null!;
    private TextureRect _pillar4 = null!;
    private TextureRect _pillar5 = null!;
    private PillarConfig _pillarConfig = PillarConfig.Open;

    // Render flags
    private bool _renderAltBg;
    private bool _renderChains;
    private bool _renderFg2;
    private bool _renderMg;
    private bool _renderMgAlt;
    private bool _renderMgGlow;
    private bool _renderThrone;
    private bool _renderWall;
    private TextureRect _throne = null!;
    private TextureRect _throneGlow = null!;
    private TextureRect _wall = null!;

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

        // Create layers in order (back to front)
        _bg = Layer("Bg");
        _bgGlow = Layer("BgGlow");
        _bg2 = Layer("Bg2");
        _bg2Glow = Layer("Bg2Glow");
        _bgGlow2 = Layer("BgGlow2");
        _bgGlow2Double = Layer("BgGlow2Double"); // NEW: same texture
        _floor = Layer("Floor");
        _ceiling = Layer("Ceiling");
        _wall = Layer("Wall");
        _chains = Layer("Chains");
        _chainsGlow = Layer("ChainsGlow");
        _chainsGlow2 = Layer("ChainsGlow2"); // NEW: same texture, double render
        _mg = Layer("Mg");
        _mgGlow = Layer("MgGlow");
        _mgGlow2 = Layer("MgGlow2"); // NEW: same texture, double render
        _mgAlt = Layer("MgAlt"); // RENAMED
        _pillar1 = Layer("Pillar1");
        _pillar2 = Layer("Pillar2");
        _pillar3 = Layer("Pillar3");
        _pillar4 = Layer("Pillar4");
        _pillar5 = Layer("Pillar5");
        _throne = Layer("Throne");
        _throneGlow = Layer("ThroneGlow");
        _fg = Layer("Fg");
        _fgGlow = Layer("FgGlow");
        _fg2 = Layer("Fg2");


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

        UpdateFireFlies();
        UpdateGlowAnimations();
        UpdateCeilingDust((float)GetProcessDeltaTime());
    }

    private void UpdateCeilingDust(float delta)
    {
        // Clean up finished effects
        for (var i = _ceilingDustEffects.Count - 1; i >= 0; i--)
        {
            var effect = _ceilingDustEffects[i];
            if (effect.IsDone || !IsInstanceValid(effect))
            {
                if (IsInstanceValid(effect))
                    effect.QueueFree();
                _ceilingDustEffects.RemoveAt(i);
            }
        }

        _ceilingDustTimer -= delta;
        if (_ceilingDustTimer < 0f)
        {
            var roll = (int)(GD.Randi() % 5); // 0-4

            if (roll == 0)
            {
                SpawnCeilingDust();
                PlayDustSfx(false);
            }
            else if (roll == 1)
            {
                SpawnCeilingDust();
                SpawnCeilingDust();
                PlayDustSfx(false);
            }
            else
            {
                SpawnCeilingDust();
                SpawnCeilingDust();
                SpawnCeilingDust();
                PlayDustSfx(true);
            }

            _ceilingDustTimer = (float)GD.RandRange(0.5, 60.0);
        }
    }

    private void PlayDustSfx(bool boom)
    {
        var roll = (int)(GD.Randi() % 3);

        if (boom)
        {
            var sound = roll switch
            {
                0 => "ceiling_boom_1",
                1 => "ceiling_boom_2",
                _ => "ceiling_boom_3"
            };
            AFTPModAudio.Play("general", sound, 0f, 0.2f);
        }
        else
        {
            var sound = roll switch
            {
                0 => "ceiling_dust_1",
                1 => "ceiling_dust_2",
                _ => "ceiling_dust_3"
            };
            AFTPModAudio.Play("general", sound, 0f, 0.2f);
        }
    }

    private void SpawnCeilingDust()
    {
        var dust = CeilingDustEffect.Create(AddCeilingDustEffect);
        dust.ZIndex = -10;
        AddChild(dust);
        _ceilingDustEffects.Add(dust);
    }

    private void AddCeilingDustEffect(NSts1Effect effect)
    {
        effect.ZIndex = -10;
        AddChild(effect);
        _ceilingDustEffects.Add(effect);
    }

    // The layers are authored in the scene, scenes/backgrounds/the_city_act.
    private TextureRect Layer(string name)
    {
        return GetNode<TextureRect>("%" + name);
    }

    public void RandomizeScene()
    {
        _hasFlies = GD.Randf() > 0.5f;
        _blueFlies = GD.Randf() > 0.5f;

        // Overlay color tinting
        _overlayColor = new Color(
            (float)GD.RandRange(0.8, 0.9),
            (float)GD.RandRange(0.8, 0.9),
            (float)GD.RandRange(0.95, 1.0)
        );

        _darkDay = GD.Randf() < 0.33f;
        if (_darkDay)
            _overlayColor = new Color(
                0.6f,
                (float)GD.RandRange(0.7, 0.8),
                (float)GD.RandRange(0.8, 0.95)
            );

        _renderAltBg = GD.Randf() > 0.5f;
        _renderMg = true;

        if (_renderMg)
        {
            _renderMgAlt = GD.Randf() > 0.5f;
            if (!_renderMgAlt)
                _renderMgGlow = GD.Randf() > 0.5f;
        }

        // TODO: check if boss room for wall logic
        _renderWall = GD.Randi() % 5 == 4;
        _renderChains = _renderWall && GD.Randf() > 0.5f;
        _renderFg2 = GD.Randf() > 0.5f;

        // Pillar configuration
        if (_renderWall)
        {
            var roll = (int)(GD.Randi() % 3);
            _pillarConfig = roll switch
            {
                0 => PillarConfig.Open,
                1 => PillarConfig.Left1,
                _ => PillarConfig.Left2
            };
        }
        else
        {
            var roll = (int)(GD.Randi() % 3);
            _pillarConfig = roll switch
            {
                0 => PillarConfig.Open,
                1 => PillarConfig.SidesOnly,
                _ => PillarConfig.Full
            };
        }

        _renderThrone = false;

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        _bg.Modulate = _overlayColor;
        _floor.Modulate = _overlayColor;
        _ceiling.Modulate = _overlayColor;
        _wall.Modulate = _overlayColor;
        _chains.Modulate = _overlayColor;
        _mg.Modulate = _overlayColor;

        _bg2.Visible = _renderAltBg;
        _bg2Glow.Visible = _renderAltBg;
        _bgGlow2.Visible = _darkDay;
        _bgGlow2Double.Visible = _darkDay; // NEW: both layers show on dark days
        _bgGlow2Double.Modulate = new Color(1, 1, 1, 0.7f); // Tweak this value to adjust brightness of dark day lights
        _wall.Visible = _renderWall;
        _chains.Visible = _renderChains;
        _chainsGlow.Visible = _renderChains;
        _chainsGlow2.Visible = _renderChains; // NEW: both glow layers follow chains visibility
        _mg.Visible = _renderMg;
        _mgGlow.Visible = _renderMg; // CHANGED: always visible when mg is visible
        _mgGlow2.Visible = _renderMg && _renderMgGlow; // NEW: second layer only when animated
        _mgAlt.Visible = _renderMgAlt; // RENAMED
        _mgAlt.Modulate =
            _renderMgGlow ? new Color(1f, 1f, 0.9f) : Colors.White; // TODO check if this line is really needed
        _fg2.Visible = _renderFg2;
        _throne.Visible = _renderThrone;
        _throneGlow.Visible = _renderThrone;

        // Pillars
        _pillar1.Visible =
            _pillarConfig is PillarConfig.SidesOnly or PillarConfig.Full or PillarConfig.Left1 or PillarConfig.Left2;
        _pillar2.Visible = _pillarConfig is PillarConfig.Full or PillarConfig.Left2;
        _pillar3.Visible = _pillarConfig == PillarConfig.Full;
        _pillar4.Visible = _pillarConfig == PillarConfig.Full;
        _pillar5.Visible = _pillarConfig is PillarConfig.SidesOnly or PillarConfig.Full;
    }

    private void UpdateFireFlies()
    {
        if (!_hasFlies) return;

        // Remove finished fireflies
        for (var i = _fireFlies.Count - 1; i >= 0; i--)
        {
            var fly = _fireFlies[i];
            if (fly.IsDone || !IsInstanceValid(fly))
            {
                if (IsInstanceValid(fly))
                    fly.QueueFree();
                _fireFlies.RemoveAt(i);
            }
        }

        // Spawn new fireflies (max 9, 10% chance per frame)
        if (_fireFlies.Count < 9 && GD.Randf() < 0.1f)
        {
            Color flyColor;
            if (_blueFlies)
                flyColor = new Color(
                    (float)GD.RandRange(0.1, 0.2),
                    (float)GD.RandRange(0.6, 0.8),
                    (float)GD.RandRange(0.8, 1.0)
                );
            else
                flyColor = new Color(
                    (float)GD.RandRange(0.8, 1.0),
                    (float)GD.RandRange(0.5, 0.8),
                    (float)GD.RandRange(0.3, 0.5)
                );

            var fly = FireFlyEffect.Create(flyColor);
            fly.ZIndex = -15;
            AddChild(fly);
            _fireFlies.Add(fly);
        }
    }

    private void UpdateGlowAnimations()
    {
        // Animated glow for chains (double rendered)
        if (_renderChains)
        {
            var chainsDegrees = (float)(Time.GetTicksMsec() % 360);
            var chainsAlpha = Mathf.Cos(Mathf.DegToRad(chainsDegrees)) / 10f + 0.9f;
            var chainsColor = new Color(1, 1, 1, chainsAlpha);
            _chainsGlow.Modulate = chainsColor;
            _chainsGlow2.Modulate = chainsColor;
        }

        // Glow for mg
        if (_renderMg)
        {
            if (_renderMgGlow)
            {
                var mgDegrees = (float)(Time.GetTicksMsec() / 10 % 360);
                var mgAlpha = Mathf.Cos(Mathf.DegToRad(mgDegrees)) / 2f + 0.5f;
                var mgColor = new Color(1, 1, 0.9f, mgAlpha);
                _mgGlow.Modulate = mgColor;
                _mgGlow2.Modulate = mgColor;
            }
            else
            {
                _mgGlow.Modulate = Colors.White;
            }
        }
    }

    // Needs the combat room around it, so it runs once the scene has been added to it.
    private void SetUpInCombatRoom()
    {
        Scale = LegacyBackgroundScaling.Oversize;

        // Find the combat room by walking up the tree
        var parent = GetParent();
        while (parent != null && parent is not NCombatRoom) parent = parent.GetParent();

        if (parent is NCombatRoom combatRoom)
        {
            LegacyBackgroundScaling.ApplyContainerScale(combatRoom);
            var sceneContainer = combatRoom.GetNodeOrNull<Control>("%CombatSceneContainer");
            if (sceneContainer != null)
            {
                DrawInFront(_fg, sceneContainer, -3);
                DrawInFront(_fgGlow, sceneContainer, -3);
                DrawInFront(_fg2, sceneContainer, -3);
            }

            var allyContainer = combatRoom.GetNodeOrNull<Control>("%AllyContainer");
            var enemyContainer = combatRoom.GetNodeOrNull<Control>("%EnemyContainer");
            allyContainer?.Position += Vector2.Down * 30f;
            enemyContainer?.Position += Vector2.Down * 30f;
            if (combatRoom._visuals.Encounter is CollectorBoss)
                SetBossMode(true);
        }
    }

    private void LogSceneTree(Node node, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var zInfo = node is CanvasItem ci ? $" [Z:{ci.ZIndex}, ZRelative:{ci.ZAsRelative}]" : "";
        Log.Info($"{indent}{node.Name} ({node.GetType().Name}){zInfo}");
        foreach (var child in node.GetChildren()) LogSceneTree(child, depth + 1);
    }

    // The foreground layers have to draw in front of the creatures, but this node sits far behind them. An absolute z
    // index does that without moving them out of the node, so they keep its scale and position.
    private void DrawInFront(TextureRect layer, Control container, int zIndex)
    {
        var containerZ = 0;
        for (CanvasItem? item = container; item != null; item = item.GetParent() as CanvasItem)
        {
            containerZ += item.ZIndex;
            if (!item.ZAsRelative) break;
        }

        layer.ZAsRelative = false;
        layer.ZIndex = containerZ + zIndex;
    }

    public void SetBossMode(bool isCollector)
    {
        _renderWall = false;
        _renderChains = false;

        // Re-roll pillars with non-wall options since wall is forced off
        var roll = (int)(GD.Randi() % 3);
        _pillarConfig = roll switch
        {
            0 => PillarConfig.Open,
            1 => PillarConfig.SidesOnly,
            _ => PillarConfig.Full
        };

        _renderThrone = isCollector;
        UpdateVisibility();
    }

    private enum PillarConfig
    {
        Open,
        SidesOnly,
        Full,
        Left1,
        Left2
    }
}