using ActsFromThePast.Compatibility;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ActsFromThePast.Minigames;

public partial class NWheelSpinScreen : Control, IOverlayScreen, IScreenContext
{
    // ─── Layout ───
    private const float WheelDisplaySize = 1024f;
    private const float ArrowDisplaySize = 512f;
    private const float ButtonDisplaySize = 512f;
    private const float ArrowOffsetX = 480f;

    // Rotational correction to align segment centers with the arrow.
    // Adjust this until result 0 lands dead-center on its segment.
    private const float WheelAngleOffset = 0f;

    // Button sits to the left of the wheel, below center
    private const float ButtonCenterX = -460f;
    private const float ButtonFinalY = 330f;
    private const float ButtonStartY = 900f;

    // ─── Animation Timing ───
    private const float BounceInDuration = 1.5f;
    private const float ButtonSlideInDuration = 0.6f;
    private const float ButtonSlideOutDuration = 0.4f;
    private const float SpinDuration = 2f;
    private const float SpinVelocity = 1500f;
    private const float DecelerateDuration = 3f;
    private const float PauseDuration = 1f;
    private const float BounceOutDuration = 0.8f;

    // Wheel drops in from above (negative = above center in Godot Y-down)
    private const float WheelStartOffset = -600f;

    // Nudge everything down from exact center to match STS1's OPTION_Y
    private const float WheelBaseY = 50f;

    // ─── Event Background ───
    private const string EventRegionName = "event";

    private static NWheelSpinScreen? _instance;
    private TextureRect _arrowRect = null!;
    private TextureRect _buttonGlowRect = null!;
    private TextureRect _buttonRect = null!;
    private Tween? _buttonTween;
    private float _buttonY = ButtonStartY;
    private NProceedButton? _controllerButton;
    private TextureRect? _controllerIcon;
    private Tween? _glowTween;

    // ─── Tweens ───
    private Tween? _mainTween;
    private WheelSpinMinigame _minigame = null!;

    // ─── State ───
    private Control _particleContainer = null!;
    private Tween? _spawnTween;
    private bool _spinning;

    // ─── UI Elements ───
    private TextureRect _wheelRect = null!;
    private float _wheelSlideOffset = WheelStartOffset;

    // ─── IOverlayScreen / IScreenContext ───

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => false;
    public Control DefaultFocusedControl => this;

    public void AfterOverlayOpened()
    {
    }

    public void AfterOverlayClosed()
    {
        KillAllTweens();
        this.QueueFreeSafely();
    }

    public void AfterOverlayShown()
    {
    }

    public void AfterOverlayHidden()
    {
    }

    private static string GetEventAtlasPath(int actIndex)
    {
        return AtlasRegions.ForAct(actIndex);
    }

    // ─── ShowScreen ───

    public static NWheelSpinScreen ShowScreen(WheelSpinMinigame minigame)
    {
        if (_instance != null && IsInstanceValid(_instance))
            _instance.QueueFree();

        var screen = new NWheelSpinScreen();
        screen._minigame = minigame;
        screen.BuildUI();
        screen.BindEvents();
        _instance = screen;
        NOverlayStack.Instance?.Push(screen);
        screen.StartBounceIn();

        return screen;
    }

    private void BindEvents()
    {
        _minigame.Finished += OnMinigameFinished;
    }

    private void UnbindEvents()
    {
        _minigame.Finished -= OnMinigameFinished;
    }

    // ─── Lifecycle ───

    public override void _ExitTree()
    {
        if (NControllerManager.Instance != null)
        {
            NControllerManager.Instance.Disconnect(
                NControllerManager.SignalName.MouseDetected,
                Callable.From(UpdateControllerIcon));
            NControllerManager.Instance.Disconnect(
                NControllerManager.SignalName.ControllerDetected,
                Callable.From(UpdateControllerIcon));
        }

        UnbindEvents();
        KillAllTweens();
        _minigame.ForceEnd();
        _instance = null;
    }

    private void KillAllTweens()
    {
        _mainTween?.Kill();
        _buttonTween?.Kill();
        _glowTween?.Kill();
        _spawnTween?.Kill();
    }
    // ─── UI Construction ───

    private void BuildUI()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Event background (cropped from atlas)
        var region = AtlasRegions.GetRegion(GetEventAtlasPath(_minigame.ActIndex), EventRegionName);
        if (region != null)
        {
            var atlasTex = new AtlasTexture();
            atlasTex.Atlas = region.Value.Texture;
            atlasTex.Region = region.Value.Region;

            var bgRect = new TextureRect
            {
                Texture = atlasTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            bgRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(bgRect);
        }

        // Particle layer (renders between background and wheel)
        _particleContainer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _particleContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_particleContainer);

        // Wheel (centered, offset controlled by _wheelSlideOffset)
        var wheelTex = GD.Load<Texture2D>("res://ActsFromThePast/images/event_extras/wheel.png");
        var halfWheel = WheelDisplaySize / 2f;
        _wheelRect = new TextureRect
        {
            Texture = wheelTex,
            CustomMinimumSize = new Vector2(WheelDisplaySize, WheelDisplaySize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            PivotOffset = new Vector2(halfWheel, halfWheel),
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -halfWheel, OffsetTop = -halfWheel,
            OffsetRight = halfWheel, OffsetBottom = halfWheel,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_wheelRect);

        // Arrow (fixed pointer to the right of the wheel)
        var arrowTex = GD.Load<Texture2D>("res://ActsFromThePast/images/event_extras/wheelArrow.png");
        var halfArrow = ArrowDisplaySize / 2f;
        _arrowRect = new TextureRect
        {
            Texture = arrowTex,
            CustomMinimumSize = new Vector2(ArrowDisplaySize, ArrowDisplaySize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = ArrowOffsetX - halfArrow,
            OffsetTop = -halfArrow,
            OffsetRight = ArrowOffsetX + halfArrow,
            OffsetBottom = halfArrow,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_arrowRect);

        // Spin button (to the left of the wheel, slides in independently)
        var buttonTex = GD.Load<Texture2D>("res://ActsFromThePast/images/event_extras/spinButton.png");
        var halfButton = ButtonDisplaySize / 2f;
        _buttonRect = new TextureRect
        {
            Texture = buttonTex,
            CustomMinimumSize = new Vector2(ButtonDisplaySize, ButtonDisplaySize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            PivotOffset = new Vector2(halfButton, halfButton),
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        _buttonRect.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(ev =>
            {
                if (_spinning) return;
                if (ev is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    _buttonRect.AcceptEvent();
                    StartSpinning();
                }
            }));
        _buttonRect.Connect(Control.SignalName.MouseEntered,
            Callable.From(() => SetButtonHovered(true)));
        _buttonRect.Connect(Control.SignalName.MouseExited,
            Callable.From(() => SetButtonHovered(false)));
        AddChild(_buttonRect);

        // Button glow overlay (additive blend)
        _buttonGlowRect = new TextureRect
        {
            Texture = buttonTex,
            CustomMinimumSize = new Vector2(ButtonDisplaySize, ButtonDisplaySize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            PivotOffset = new Vector2(halfButton, halfButton),
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            Visible = false
        };
        AddChild(_buttonGlowRect);

        // Hidden controller button — uses NButton's hotkey system (MegaInput.accept = Y button)
        var proceedScene = GD.Load<PackedScene>("res://scenes/ui/proceed_button.tscn");
        if (proceedScene != null)
        {
            _controllerButton = proceedScene.Instantiate<NProceedButton>();
            _controllerButton.Modulate = Colors.Transparent;
            AddChild(_controllerButton);
            Callable.From(() =>
            {
                _controllerButton.UpdateText(new LocString("gameplay_ui", "PROCEED_BUTTON"));
                _controllerButton.Disable();
            }).CallDeferred();
            _controllerButton.Connect(
                NClickableControl.SignalName.Released,
                Callable.From(new Action<NButton>(_ =>
                {
                    if (_spinning || !_buttonRect.Visible) return;
                    StartSpinning();
                }))
            );
        }

        // Controller icon next to spin button (visible only in controller mode)
        _controllerIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(128, 128),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            AnchorLeft = 0.5f, AnchorTop = 0.5f,
            AnchorRight = 0.5f, AnchorBottom = 0.5f,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
            Scale = new Vector2(0.5f, 0.5f),
            PivotOffset = new Vector2(64, 64),
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        AddChild(_controllerIcon);

        // React to controller/mouse switching
        if (NControllerManager.Instance != null)
        {
            NControllerManager.Instance.Connect(
                NControllerManager.SignalName.MouseDetected,
                Callable.From(UpdateControllerIcon));
            NControllerManager.Instance.Connect(
                NControllerManager.SignalName.ControllerDetected,
                Callable.From(UpdateControllerIcon));
        }

        // Apply initial positions
        ApplyWheelOffset();
        ApplyButtonPosition();

        // Start everything transparent
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    // ─── Position Helpers ───

    private void SetWheelSlideOffset(float value)
    {
        _wheelSlideOffset = value;
        ApplyWheelOffset();
    }

    private void ApplyWheelOffset()
    {
        var halfWheel = WheelDisplaySize / 2f;
        var halfArrow = ArrowDisplaySize / 2f;
        var y = WheelBaseY + _wheelSlideOffset;

        _wheelRect.OffsetTop = -halfWheel + y;
        _wheelRect.OffsetBottom = halfWheel + y;

        _arrowRect.OffsetTop = -halfArrow + y;
        _arrowRect.OffsetBottom = halfArrow + y;
    }

    private void SetButtonY(float y)
    {
        _buttonY = y;
        ApplyButtonPosition();
    }

    private void ApplyButtonPosition()
    {
        var halfButton = ButtonDisplaySize / 2f;
        var y = _buttonY + WheelBaseY;

        _buttonRect.OffsetLeft = ButtonCenterX - halfButton;
        _buttonRect.OffsetRight = ButtonCenterX + halfButton;
        _buttonRect.OffsetTop = y - halfButton;
        _buttonRect.OffsetBottom = y + halfButton;

        _buttonGlowRect.OffsetLeft = _buttonRect.OffsetLeft;
        _buttonGlowRect.OffsetRight = _buttonRect.OffsetRight;
        _buttonGlowRect.OffsetTop = _buttonRect.OffsetTop;
        _buttonGlowRect.OffsetBottom = _buttonRect.OffsetBottom;

        // Controller icon to the left of the spin button
        if (_controllerIcon != null)
        {
            var iconOffset = 80f;
            _controllerIcon.OffsetLeft = ButtonCenterX - halfButton - iconOffset - 64f;
            _controllerIcon.OffsetRight = ButtonCenterX - halfButton - iconOffset + 64f;
            _controllerIcon.OffsetTop = y - 64f;
            _controllerIcon.OffsetBottom = y + 64f;
        }
    }

    // ─── Bounce In ───

    private void StartBounceIn()
    {
        _wheelSlideOffset = WheelStartOffset;
        _buttonY = ButtonStartY;
        ApplyWheelOffset();
        ApplyButtonPosition();
        StartParticleSpawner();

        _mainTween?.Kill();
        _mainTween = CreateTween();
        _mainTween.SetParallel();

        // Fade in
        _mainTween.TweenProperty(
                this, (NodePath)"modulate:a",
                1.0f, BounceInDuration
            ).From(0.0f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        // Wheel drops in from above
        _mainTween.TweenMethod(
                Callable.From<float>(SetWheelSlideOffset),
                WheelStartOffset, 0f, BounceInDuration
            ).SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);

        _mainTween.SetParallel(false);
        _mainTween.TweenCallback(Callable.From(() =>
        {
            // Wheel is settled, now slide button in from below
            _buttonRect.Visible = true;
            _buttonGlowRect.Visible = true;
            _controllerButton?.Enable();
            UpdateControllerIcon();
            StartGlowPulse();
            SlideButtonIn();
        }));
    }

    // ─── Button Slide ───

    private void SlideButtonIn()
    {
        _buttonTween?.Kill();
        _buttonTween = CreateTween();

        _buttonTween.TweenMethod(
                Callable.From<float>(SetButtonY),
                ButtonStartY, ButtonFinalY, ButtonSlideInDuration
            ).SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
    }

    private void SlideButtonOut()
    {
        _buttonTween?.Kill();
        _buttonTween = CreateTween();

        _buttonTween.TweenMethod(
                Callable.From<float>(SetButtonY),
                ButtonFinalY, ButtonStartY, ButtonSlideOutDuration
            ).SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);

        _buttonTween.TweenCallback(Callable.From(() =>
        {
            _buttonRect.Visible = false;
            _buttonGlowRect.Visible = false;
        }));
    }

    // ─── Spin Sequence (all tween-driven) ───

    private void StartSpinning()
    {
        _spinning = true;
        _glowTween?.Kill();
        _controllerButton?.Disable();
        _controllerIcon?.Visible = false;
        SlideButtonOut();

        AFTPModAudio.Play("events", "wheel");

        var resultAngle = _minigame.ResultAngle;
        var spinEnd = SpinVelocity * SpinDuration; // ~3000°

        _mainTween?.Kill();
        _mainTween = CreateTween();

        // Phase 1: Fast constant-speed spin (CCW via negation)
        _mainTween.TweenMethod(
            Callable.From<float>(angle => { _wheelRect.RotationDegrees = -angle + WheelAngleOffset; }),
            0f, spinEnd, SpinDuration
        ).SetTrans(Tween.TransitionType.Linear);

        // Phase 2: Original STS1 ElasticIn deceleration
        // t goes 1→0: large oscillations at start (masked by fast spin),
        // smooth natural settling at the end (no snap).
        // The phase boundary has the same ~60° visual jump as the original game.
        _mainTween.TweenMethod(
            Callable.From<float>(t =>
            {
                _wheelRect.RotationDegrees = -ElasticLerp(resultAngle, -180f, t) + WheelAngleOffset;
            }),
            1.0f, 0.0f, DecelerateDuration
        ).SetTrans(Tween.TransitionType.Linear);

        _mainTween.TweenCallback(Callable.From(() =>
        {
            var expected = -resultAngle + WheelAngleOffset;
            var actual = _wheelRect.RotationDegrees;
            var drift = actual - expected;
            // Snap to exact angle in case the tween's final frame drifted
            _wheelRect.RotationDegrees = expected;
        }));

        // Pause on result
        _mainTween.TweenInterval(PauseDuration);

        // Bounce out
        _mainTween.TweenCallback(Callable.From(StartBounceOut));
    }

    // ─── Bounce Out ───

    private void StartBounceOut()
    {
        _spawnTween?.Kill();

        _mainTween?.Kill();
        _mainTween = CreateTween();
        _mainTween.SetParallel();

        // Fade out
        _mainTween.TweenProperty(
                this, (NodePath)"modulate:a",
                0.0f, BounceOutDuration
            ).SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);

        // Wheel slides back up
        _mainTween.TweenMethod(
                Callable.From<float>(SetWheelSlideOffset),
                0f, WheelStartOffset, BounceOutDuration
            ).SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);

        _mainTween.SetParallel(false);
        _mainTween.TweenCallback(Callable.From(() => { _minigame.Complete(); }));
    }

    // ─── Button Glow ───

    private void StartGlowPulse()
    {
        _glowTween?.Kill();
        _glowTween = CreateTween();
        _glowTween.SetLoops();

        _glowTween.TweenMethod(
                Callable.From<float>(a => { _buttonGlowRect.Modulate = new Color(1f, 1f, 1f, a); }),
                0.07f, 0.35f, 0.8f
            ).SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        _glowTween.TweenMethod(
                Callable.From<float>(a => { _buttonGlowRect.Modulate = new Color(1f, 1f, 1f, a); }),
                0.35f, 0.07f, 0.8f
            ).SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void SetButtonHovered(bool hovered)
    {
        if (_buttonRect == null || _spinning) return;
        _buttonRect.Scale = hovered ? Vector2.One * 1.05f : Vector2.One;
        _buttonGlowRect.Scale = _buttonRect.Scale;

        if (hovered)
        {
            _glowTween?.Kill();
            _buttonGlowRect.Modulate = new Color(1f, 1f, 1f, 0.25f);
        }
        else
        {
            StartGlowPulse();
        }
    }

    // ─── Controller Icon ───

    private void UpdateControllerIcon()
    {
        if (_controllerIcon == null) return;

        if (!_buttonRect.Visible || _spinning)
        {
            _controllerIcon.Visible = false;
            return;
        }

        var controllerManager = NControllerManager.Instance;
        if (controllerManager == null || controllerManager.InputType != InputType.Controller)
        {
            _controllerIcon.Visible = false;
            return;
        }

        var hotkeyIcon = NInputManager.Instance?.GetHotkeyIcon(MegaInput.select);
        if (hotkeyIcon != null)
        {
            _controllerIcon.Texture = hotkeyIcon;
            _controllerIcon.Visible = true;
        }
        else
        {
            _controllerIcon.Visible = false;
        }
    }

    // ─── Particle Spawner ───

    private void StartParticleSpawner()
    {
        _spawnTween?.Kill();
        _spawnTween = CreateTween();
        _spawnTween.SetLoops();
        _spawnTween.TweenCallback(Callable.From(SpawnParticle));
        _spawnTween.TweenInterval(1.0f);
    }

    private void SpawnParticle()
    {
        var size = Size;
        if (size.X <= 0 || size.Y <= 0) return;
        var particle = EventBgParticleEffect.Create(size / 2f);
        _particleContainer.AddChild(particle);
    }

    // ─── Elastic Interpolation (based on LibGDX ElasticIn) ───

    private static float ElasticIn(float a)
    {
        if (a >= 0.99f) return 1f;
        if (a <= 0f) return 0f;
        var raw = -(float)(Math.Pow(2, 10 * (a - 1))
                           * Math.Sin((a - 1.1f) * 900f * Mathf.Pi / 180f));
        // Dampen the tail oscillations so the wheel settles cleanly.
        // The big swings (a > 0.5) that sell the deceleration are untouched;
        // only the small wobbles near the end get faded out.
        if (a < 0.5f)
        {
            var damp = a / 0.5f;
            raw *= damp * damp;
        }

        return raw;
    }

    private static float ElasticLerp(float from, float to, float t)
    {
        return from + (to - from) * ElasticIn(t);
    }

    // ─── Cleanup ───

    private void OnMinigameFinished()
    {
        NOverlayStack.Instance?.Remove(this);
    }
}