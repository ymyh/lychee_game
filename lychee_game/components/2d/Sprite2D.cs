using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources._2d;

namespace lychee_game.components._2d;

/// <summary>
/// Component for 2D sprites. All fields default to the 0th slot of their respective resource pools.
/// Must be created via <c>new Sprite2D()</c> to ensure proper initialization.
/// Do not use <c>default(Sprite2D)</c> — it bypasses the constructor.
/// </summary>
[Component]
public partial struct Sprite2D
{
#region Public Fields

    /// <summary>
    /// Mesh reference for geometry. Default = UnitQuad (pool slot 0).
    /// </summary>
    public Mesh2DRef Mesh;

    /// <summary>
    /// Material reference (Effect + SamplerState). Default = DefaultMaterial (pool slot 0).
    /// </summary>
    public MaterialRef Material;

    /// <summary>
    /// Texture reference for sampling. Default = WhiteTexture (pool slot 0).
    /// </summary>
    public Texture2DRef Texture;

    /// <summary>
    /// Optional UV rectangle for sprite sheet cropping. Default = full image (0,0)-(1,1).
    /// </summary>
    public UVRect UV;

    /// <summary>
    /// Color tint applied to the sprite. Default = white (255,255,255,255).
    /// </summary>
    public RgbaByte Tint;

#endregion
}
