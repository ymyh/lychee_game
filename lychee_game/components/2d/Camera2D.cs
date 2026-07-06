using System.Numerics;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// 2D camera component for world-space rendering with orthographic projection.
/// Origin is centered, Y-axis points up.
/// </summary>
[Component]
public partial struct Camera2D
{
#region Public Fields

    /// <summary>
    /// Camera position in world space.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Camera rotation in radians.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Camera zoom level. Default is 1.0.
    /// </summary>
    public float Zoom;

#endregion

#region Public Methods

    /// <summary>
    /// Calculates the view-projection matrix for the given viewport dimensions.
    /// </summary>
    /// <param name="viewportW">Viewport width in pixels.</param>
    /// <param name="viewportH">Viewport height in pixels.</param>
    /// <param name="vp">The resulting view-projection matrix.</param>
    public void CalculateViewProjection(float viewportW, float viewportH, out Matrix4x4 vp)
    {
        var halfW = viewportW / 2.0f / Zoom;
        var halfH = viewportH / 2.0f / Zoom;

        var projection = Matrix4x4.CreateOrthographicOffCenter(
            -halfW, halfW,
            -halfH, halfH,
            -1.0f, 1.0f);

        var view = Matrix4x4.CreateRotationZ(-Rotation) *
                   Matrix4x4.CreateTranslation(new Vector3(-Position, 0.0f));

        vp = view * projection;
    }

#endregion
}
