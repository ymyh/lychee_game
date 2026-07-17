using System.Numerics;
using System.Runtime.InteropServices;

namespace lychee_game.resources._2d;

/// <summary>
/// RGBA color with byte components.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RgbaByte
{
#region Public Fields

    /// <summary>Red component (0-255).</summary>
    public byte R;

    /// <summary>Green component (0-255).</summary>
    public byte G;

    /// <summary>Blue component (0-255).</summary>
    public byte B;

    /// <summary>Alpha component (0-255).</summary>
    public byte A;

#endregion

#region Constructor

    /// <summary>
    /// Creates an RgbaByte with the specified components.
    /// </summary>
    public RgbaByte(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

#endregion

#region Static Properties

    /// <summary>White color (255, 255, 255, 255).</summary>
    public static RgbaByte White => new(255, 255, 255, 255);

    /// <summary>Black color (0, 0, 0, 255).</summary>
    public static RgbaByte Black => new(0, 0, 0, 255);

    /// <summary>Red color (255, 0, 0, 255).</summary>
    public static RgbaByte Red => new(255, 0, 0, 255);

    /// <summary>Green color (0, 255, 0, 255).</summary>
    public static RgbaByte Green => new(0, 255, 0, 255);

    /// <summary>Blue color (0, 0, 255, 255).</summary>
    public static RgbaByte Blue => new(0, 0, 255, 255);

#endregion
}

/// <summary>
/// Default 2D vertex with position, texture coordinates, and color.
/// Layout: Position(FLOAT2) + TexCoord(FLOAT2) + Color(UBYTE4_NORM), pitch = 20 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vertex2D
{
#region Public Fields

    /// <summary>
    /// Vertex position in 2D space.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Texture coordinates for sampling.
    /// </summary>
    public Vector2 TexCoord;

    /// <summary>
    /// Vertex color tint.
    /// </summary>
    public RgbaByte Color;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Vertex2D with the specified attributes.
    /// </summary>
    public Vertex2D(Vector2 position, Vector2 texCoord, RgbaByte color)
    {
        Position = position;
        TexCoord = texCoord;
        Color = color;
    }

#endregion
}
