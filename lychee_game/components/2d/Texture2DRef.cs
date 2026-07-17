using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources._2d;

namespace lychee_game.components._2d;

/// <summary>
/// References a texture in the Texture2D resource pool by index and generation.
/// </summary>
[Component]
public partial struct Texture2DRef : IResourceRef, IEquatable<Texture2DRef>
{
#region Public Fields

    /// <summary>
    /// Index into the Texture2D resource pool. Slot 0 is the default WhiteTexture.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Generation counter for detecting stale references.
    /// </summary>
    public uint Generation { get; set; }

#endregion

#region Equality

    /// <inheritdoc/>
    public bool Equals(Texture2DRef other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Texture2DRef other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <summary>
    /// Checks whether two Texture2DRef values are equal.
    /// </summary>
    public static bool operator ==(Texture2DRef left, Texture2DRef right) => left.Equals(right);

    /// <summary>
    /// Checks whether two Texture2DRef values are not equal.
    /// </summary>
    public static bool operator !=(Texture2DRef left, Texture2DRef right) => !left.Equals(right);

#endregion
}
