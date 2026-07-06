using lychee_game.components._2d;

namespace lychee_game.resources._2d;

/// <summary>
/// Pooled, shareable rendering configuration: Effect + SamplerState.
/// Does not contain Texture (texture is per-sprite in the component layer).
/// Multiple sprites can reference the same Material via MaterialRef.
/// </summary>
public sealed class Material
{
#region Public Properties

    /// <summary>
    /// The effect (shader) to use.
    /// </summary>
    public EffectRef Effect { get; init; }

    /// <summary>
    /// Sampler state for texture filtering and addressing.
    /// </summary>
    public SamplerState Sampler { get; init; } = new();

#endregion

#region Equality

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Material other)
        {
            return false;
        }

        return Effect.Equals(other.Effect) &&
               Sampler.Equals(other.Sampler);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Effect, Sampler);
    }

#endregion
}
