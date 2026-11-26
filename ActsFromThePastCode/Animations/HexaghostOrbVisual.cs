using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public class HexaghostOrbVisual : IDisposable
{
    private const float BobSpeed = 2.0f;
    private const float BobAmount = 3.0f;
    private const float ParticleInterval = 0.06f;
    private readonly Vector2 _basePosition;

    private readonly int _index;

    private float _activateTimer;
    private float _bobTimer;
    private Vector2 _currentPosition;
    private Node? _parentNode;
    private float _particleTimer;
    private bool _playedSfx;

    public HexaghostOrbVisual(int index, Vector2 position)
    {
        _index = index;
        _basePosition = position + new Vector2(
            (float)GD.RandRange(-10f, 10f),
            (float)GD.RandRange(-10f, 10f)
        );
        _currentPosition = _basePosition;
        _activateTimer = index * 0.3f;
    }

    public bool IsActivated { get; private set; }
    public bool IsHidden { get; private set; } = true;

    public void Dispose()
    {
        // Nothing to dispose anymore since we removed the Sprite
    }

    public void SetParentNode(Node parent)
    {
        _parentNode = parent;
    }

    public void Activate(bool immediate = false)
    {
        _playedSfx = false;
        IsActivated = true;
        IsHidden = false;
        _activateTimer = immediate ? 0f : _index * 0.3f;
    }

    public void Deactivate()
    {
        IsActivated = false;
    }

    public void Hide()
    {
        IsHidden = true;
    }

    public Vector2 GetGlobalPosition(Vector2 parentGlobalPosition)
    {
        return parentGlobalPosition + _currentPosition;
    }

    public void Update(float delta, Vector2 parentGlobalPosition)
    {
        if (IsHidden) return;

        _bobTimer += BobSpeed * delta;
        var bobOffset = Mathf.Sin(_bobTimer) * BobAmount;
        _currentPosition = _basePosition + new Vector2(bobOffset * 2f, bobOffset * 2f);

        var globalPos = GetGlobalPosition(parentGlobalPosition);

        if (IsActivated)
        {
            _activateTimer -= delta;

            if (_activateTimer < 0f)
            {
                if (!_playedSfx)
                {
                    _playedSfx = true;
                    SpawnIgniteEffect(globalPos);
                    PlayIgniteSound();
                }

                _particleTimer -= delta;
                if (_particleTimer < 0f)
                {
                    SpawnFireEffect(globalPos);
                    _particleTimer = ParticleInterval;
                }
            }
        }
        else
        {
            _particleTimer -= delta;
            if (_particleTimer < 0f)
            {
                SpawnWeakFireEffect(globalPos);
                _particleTimer = ParticleInterval;
            }
        }
    }

    private void SpawnIgniteEffect(Vector2 pos)
    {
        var effect = GhostIgniteEffect.Create(pos.X, pos.Y);
        NCombatRoom.Instance?.CombatVfxContainer.AddChild(effect);
    }

    private void SpawnFireEffect(Vector2 pos)
    {
        var effect = GhostlyFireEffect.Create(pos.X, pos.Y);
        NCombatRoom.Instance?.CombatVfxContainer.AddChild(effect);
    }

    private void SpawnWeakFireEffect(Vector2 pos)
    {
        var effect = GhostlyWeakFireEffect.Create(pos.X, pos.Y);
        NCombatRoom.Instance?.CombatVfxContainer.AddChild(effect);
    }

    private void PlayIgniteSound()
    {
        AFTPModAudio.Play("hexaghost", GD.Randf() < 0.5f ? "ghost_orb_ignite_1" : "ghost_orb_ignite_2");
    }
}