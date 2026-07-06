using System.Numerics;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// UV rectangle for sprite sheet cropping. Defaults to full texture (0,0)-(1,1).
/// </summary>
[Component]
public partial struct UVRect
{
#region Public Fields

    /// <summary>
    /// Minimum UV coordinates (top-left corner).
    /// </summary>
    public Vector2 Min;

    /// <summary>
    /// Maximum UV coordinates (bottom-right corner).
    /// </summary>
    public Vector2 Max;

#endregion

#region Constructor

    /// <summary>
    /// Creates a UVRect with the specified bounds.
    /// </summary>
    public UVRect(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a full-texture UVRect (0,0)-(1,1).
    /// </summary>
    public static UVRect Full => new(Vector2.Zero, Vector2.One);

#endregion
}
