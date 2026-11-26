using Godot;

namespace ActsFromThePast;

public partial class IntenseZoomEffect : NSts1Effect
{
    private const int ParticleCount = 10;
    private bool _isBlack;
    private bool _spawned;

    private Vector2 _targetPosition;

    public static IntenseZoomEffect Create(Vector2 position, bool isBlack = false)
    {
        var effect = new IntenseZoomEffect();
        effect._targetPosition = position;
        effect._isBlack = isBlack;
        effect.Position = position;
        effect.Setup();
        return effect;
    }

    protected override void Initialize()
    {
        Duration = 0f;
        StartingDuration = 0f;
        SpawnEffects();
        IsDone = true;
    }

    protected override void Update(float delta)
    {
    }

    private void SpawnEffects()
    {
        var parent = GetParent();
        if (parent == null) return;

        if (_isBlack)
            BorderFlashEffect.Play(Colors.Black);
        else
            BorderFlashEffect.PlayGold();

        for (var i = 0; i < ParticleCount; i++)
        {
            var particle = IntenseZoomParticle.Create(_targetPosition, _isBlack);
            parent.AddChild(particle);
        }
    }
}