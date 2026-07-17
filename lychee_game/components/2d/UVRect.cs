using System.Numerics;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// UV rectangle for sprite sheet cropping. Defaults to full texture (0,0)-(1,1).
/// V increases upward to match the engine's Y-up world space.
/// </summary>
[Component]
public partial struct UVRect
{
#region Public Fields

    /// <summary>
    /// Minimum UV coordinates (typically bottom-left of the cropped region).
    /// </summary>
    public Vector2 Min;

    /// <summary>
    /// Maximum UV coordinates (typically top-right of the cropped region).
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

#endregion

#region Static Properties

    /// <summary>
    /// Creates a full-texture UVRect (0,0)-(1,1).
    /// </summary>
    public static UVRect Full => new(Vector2.Zero, Vector2.One);

#endregion
}
