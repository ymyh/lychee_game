using System.Numerics;
using System.Runtime.InteropServices;

namespace lychee_game.resources._2d;

/// <summary>
/// Per-instance sprite data for GPU instanced drawing.
/// Layout: World(mat4) + UvRect(vec4) + Tint(ubyte4) + pad = 96 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 96)]
public struct SpriteInstance
{
#region Public Fields

    /// <summary>
    /// World transform matrix (64 bytes).
    /// </summary>
    public Matrix4x4 World;

    /// <summary>
    /// UV rectangle as (Min.X, Min.Y, Max.X, Max.Y).
    /// </summary>
    public Vector4 UvRect;

    /// <summary>
    /// Per-instance color tint.
    /// </summary>
    public RgbaByte Tint;

#endregion
}
