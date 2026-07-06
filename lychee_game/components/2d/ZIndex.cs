using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// Draw order component for 2D entities. Higher values render on top.
/// </summary>
[Component]
public partial struct ZIndex
{
#region Public Fields

    /// <summary>
    /// The draw order value. Higher values are rendered last (on top).
    /// </summary>
    public int Value;

#endregion
}
