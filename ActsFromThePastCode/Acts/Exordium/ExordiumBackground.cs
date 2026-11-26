using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.Exordium;

public partial class ExordiumBackground : NCombatBackground
{
    private const string LOG_TAG = "[ActsFromThePast]";
    private const int MaxDust = 96;
    private const int MaxFog = 50;

    // Effects
    private readonly List<DustEffect> _dust = [];
    private readonly List<BottomFogEffect> _fog = [];
    private readonly List<InteractableTorchEffect> _torches = [];

    // Background layers
    private TextureRect _bg = null!;
    private TextureRect _ceiling = null!;

    // Ceiling mods
    private TextureRect _ceilingMod1 = null!;
    private TextureRect _ceilingMod2 = null!;
    private TextureRect _ceilingMod3 = null!;
    private TextureRect _ceilingMod4 = null!;
    private TextureRect _ceilingMod5 = null!;
    private TextureRect _ceilingMod6 = null!;
    private TextureRect _fg = null!;
    private TextureRect _hollowWall = null!;

    private bool _initialized;

    // Wall variants
    private TextureRect _leftWall = null!;
    private TextureRect _mg = null!;
    private Color _overlayColor;

    // Overlay
    private ColorRect _overlayRect = null!;
    private bool _renderHollowMid;

    // Render flags
    private bool _renderLeftWall;
    private bool _renderSolidMid;
    private TextureRect _solidWall = null!;

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

        _bg = Layer("Bg");
        _mg = Layer("Mg");
        _solidWall = Layer("SolidWall");
        _hollowWall = Layer("HollowWall");
        _leftWall = Layer("LeftWall");
        _ceiling = Layer("Ceiling");
        _ceilingMod1 = Layer("CeilingMod1");
        _ceilingMod2 = Layer("CeilingMod2");
        _ceilingMod3 = Layer("CeilingMod3");
        _ceilingMod4 = Layer("CeilingMod4");
        _ceilingMod5 = Layer("CeilingMod5");
        _ceilingMod6 = Layer("CeilingMod6");
        _fg = Layer("Fg");
        _overlayRect = GetNode<ColorRect>("%Overlay");

        RandomizeScene();
    }

    public void _DeferredInit()
    {
        if (IsInsideTree())
        {
            GetTree().ProcessFrame -= OnProcessFrame;
            GetTree().ProcessFrame += OnProcessFrame;
        }

        Initialize();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().ProcessFrame += OnProcessFrame;
    }


    private void OnProcessFrame()
    {
        if (!_initialized || !IsInsideTree()) return;

        UpdateDust();
        UpdateFog();
    }

    public override void _ExitTree()
    {
        GetTree().ProcessFrame -= OnProcessFrame;
        base._ExitTree();
    }

    // The layers are authored in the scene, scenes/backgrounds/exordium_act.
    private TextureRect Layer(string name)
    {
        return GetNode<TextureRect>("%" + name);
    }

    private void RandomizeTorches()
    {
        // Clear existing torches
        foreach (var torch in _torches)
            if (IsInstanceValid(torch))
                torch.QueueFree();

        _torches.Clear();

        // 10% chance for torch on right side
        if (GD.Randf() < 0.1f)
            _torches.Add(InteractableTorchEffect.Create(1790f, 850f, InteractableTorchEffect.TorchSize.S));

        if (_renderHollowMid && !_renderSolidMid)
        {
            var roll = (int)(GD.Randi() % 3);
            if (roll == 0)
            {
                _torches.Add(InteractableTorchEffect.Create(800f, 768f));
                _torches.Add(InteractableTorchEffect.Create(1206f, 768f));
            }
            else if (roll == 1)
            {
                _torches.Add(InteractableTorchEffect.Create(328f, 865f, InteractableTorchEffect.TorchSize.S));
            }
        }
        else if (!_renderLeftWall && !_renderHollowMid)
        {
            if (GD.Randf() < 0.75f)
            {
                _torches.Add(InteractableTorchEffect.Create(613f, 860f));
                _torches.Add(InteractableTorchEffect.Create(613f, 672f));
                if (GD.Randf() < 0.3f)
                {
                    _torches.Add(InteractableTorchEffect.Create(1482f, 860f));
                    _torches.Add(InteractableTorchEffect.Create(1482f, 672f));
                }
            }
        }
        else if (_renderSolidMid && _renderHollowMid)
        {
            if (!_renderLeftWall)
            {
                var roll = (int)(GD.Randi() % 4);
                if (roll == 0)
                {
                    _torches.Add(InteractableTorchEffect.Create(912f, 790f));
                    _torches.Add(InteractableTorchEffect.Create(912f, 526f));
                    _torches.Add(InteractableTorchEffect.Create(844f, 658f, InteractableTorchEffect.TorchSize.S));
                    _torches.Add(InteractableTorchEffect.Create(980f, 658f, InteractableTorchEffect.TorchSize.S));
                }
                else if (roll == 1 || roll == 2)
                {
                    _torches.Add(InteractableTorchEffect.Create(1828f, 720f));
                }
            }
            else if (GD.Randf() < 0.75f)
            {
                _torches.Add(InteractableTorchEffect.Create(970f, 874f, InteractableTorchEffect.TorchSize.L));
            }
        }
        else if (_renderLeftWall && !_renderHollowMid)
        {
            if (GD.Randf() < 0.75f)
            {
                _torches.Add(InteractableTorchEffect.Create(970f, 873f, InteractableTorchEffect.TorchSize.L));
                _torches.Add(InteractableTorchEffect.Create(616f, 813f));
                _torches.Add(InteractableTorchEffect.Create(1266f, 708f));
            }
        }

        // Randomize green rendering for torch effects
        InteractableTorchEffect.RenderGreen = GD.Randf() > 0.5f;

        // Add torches to scene
        foreach (var torch in _torches)
        {
            torch.ZIndex = -15;
            AddChild(torch);
            torch.Initialize();
        }
    }

    public void RandomizeScene()
    {
        // Randomize wall configuration (matches original logic)
        if (GD.Randf() > 0.5f)
        {
            _renderSolidMid = false;
            _renderLeftWall = false;
            _renderHollowMid = true;

            if (GD.Randf() > 0.5f)
            {
                _renderSolidMid = true;
                if (GD.Randf() > 0.5f) _renderLeftWall = true;
            }
        }
        else
        {
            _renderLeftWall = false;
            _renderHollowMid = false;
            _renderSolidMid = true;

            if (GD.Randf() > 0.5f) _renderLeftWall = true;
        }

        // Randomize ceiling mods
        _ceilingMod1.Visible = GD.Randf() > 0.5f;
        _ceilingMod2.Visible = GD.Randf() > 0.5f;
        _ceilingMod3.Visible = GD.Randf() > 0.5f;
        _ceilingMod4.Visible = GD.Randf() > 0.5f;
        _ceilingMod5.Visible = GD.Randf() > 0.5f;
        _ceilingMod6.Visible = GD.Randf() > 0.5f;

        // Randomize overlay color (StS1 values with Godot correction factor)
        const float blendFactor = 0.2f;
        _overlayColor = new Color(
            (float)GD.RandRange(0.0, 0.05) * blendFactor,
            (float)GD.RandRange(0.0, 0.2) * blendFactor,
            (float)GD.RandRange(0.0, 0.2) * blendFactor
        );

        // Randomize torches
        RandomizeTorches();

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        _solidWall.Visible = _renderSolidMid;
        _hollowWall.Visible = _renderHollowMid;
        _leftWall.Visible = _renderLeftWall;

        if (_renderHollowMid && (_renderSolidMid || _renderLeftWall))
            _solidWall.Modulate = Colors.Gray;
        else
            _solidWall.Modulate = Colors.White;

        _overlayRect.Color = _overlayColor;
    }

    private void UpdateDust()
    {
        for (var i = _dust.Count - 1; i >= 0; i--)
        {
            var dust = _dust[i];
            if (dust.IsDone || !IsInstanceValid(dust))
            {
                if (IsInstanceValid(dust)) dust.QueueFree();
                _dust.RemoveAt(i);
            }
        }

        while (_dust.Count < MaxDust)
        {
            var dust = DustEffect.Create();
            dust.ZIndex = -11;
            AddChild(dust);
            _dust.Add(dust);
        }
    }

    private void UpdateFog()
    {
        for (var i = _fog.Count - 1; i >= 0; i--)
        {
            var fog = _fog[i];
            if (fog.IsDone || !IsInstanceValid(fog))
            {
                if (IsInstanceValid(fog)) fog.QueueFree();
                _fog.RemoveAt(i);
            }
        }

        while (_fog.Count < MaxFog)
        {
            var fog = BottomFogEffect.Create();
            fog.ZIndex = -45;
            AddChild(fog);
            _fog.Add(fog);
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
            var allyContainer = combatRoom.GetNodeOrNull<Control>("%AllyContainer");
            var enemyContainer = combatRoom.GetNodeOrNull<Control>("%EnemyContainer");

            allyContainer?.Position += Vector2.Down * 30f;
            enemyContainer?.Position += Vector2.Down * 30f;
        }
    }
}