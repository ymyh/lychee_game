using lychee.attributes;
using lychee.interfaces;
using lychee_game.components._2d;
using lychee_game.resources;

namespace lychee_game.systems._2d;

/// <summary>
/// Updates the view-projection matrix from the entity with Camera2D + MainCamera components.
/// Only one entity should have MainCamera at a time (last one wins if multiple).
/// </summary>
[SystemFilter(All = new[] { typeof(Camera2D), typeof(MainCamera) })]
[AutoImplSystem]
public partial class CameraUpdateSystem
{
#region Execute

    private static void Execute(in Camera2D cam, [Resource] RenderContext ctx,
        [Resource] Window window)
    {
        cam.CalculateViewProjection(window.Width, window.Height, out var vp);
        ctx.ViewProjection = vp;
    }

#endregion
}
