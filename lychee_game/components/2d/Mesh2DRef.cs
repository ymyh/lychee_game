using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources._2d;

namespace lychee_game.components._2d;

/// <summary>
/// References a mesh in the Mesh2D resource pool by index and generation.
/// </summary>
[Component]
public partial struct Mesh2DRef : IResourceRef, IEquatable<Mesh2DRef>
{
#region Public Fields

    /// <summary>
    /// Index into the Mesh2D resource pool.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Generation counter for detecting stale references.
    /// </summary>
    public uint Generation { get; set; }

#endregion

#region Equality

    /// <inheritdoc/>
    public bool Equals(Mesh2DRef other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Mesh2DRef other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <summary>
    /// Checks whether two Mesh2DRef values are equal.
    /// </summary>
    public static bool operator ==(Mesh2DRef left, Mesh2DRef right) => left.Equals(right);

    /// <summary>
    /// Checks whether two Mesh2DRef values are not equal.
    /// </summary>
    public static bool operator !=(Mesh2DRef left, Mesh2DRef right) => !left.Equals(right);

#endregion
}
