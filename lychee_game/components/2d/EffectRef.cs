using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources._2d;

namespace lychee_game.components._2d;

/// <summary>
/// References an effect in the Effect resource pool by index and generation.
/// </summary>
[Component]
public partial struct EffectRef : IResourceRef, IEquatable<EffectRef>
{
#region Public Fields

    /// <summary>
    /// Index into the Effect resource pool.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Generation counter for detecting stale references.
    /// </summary>
    public uint Generation { get; set; }

#endregion

#region Equality

    /// <inheritdoc/>
    public bool Equals(EffectRef other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EffectRef other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <summary>
    /// Checks whether two EffectRef values are equal.
    /// </summary>
    public static bool operator ==(EffectRef left, EffectRef right) => left.Equals(right);

    /// <summary>
    /// Checks whether two EffectRef values are not equal.
    /// </summary>
    public static bool operator !=(EffectRef left, EffectRef right) => !left.Equals(right);

#endregion
}
