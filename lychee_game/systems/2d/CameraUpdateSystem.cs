using lychee.attributes;
using lychee_game.components._2d;
using lychee_game.resources;

namespace lychee_game.systems._2d;

/// <summary>
/// Updates the view-projection matrix from the entity with Camera2D + MainCamera components.
/// Only one entity should have MainCamera at a time (last one wins if multiple).
/// Uses swapchain pixel size so the orthographic projection matches the GPU viewport under high DPI.
/// </summary>
[SystemFilter(All = new[] { typeof(Camera2D), typeof(MainCamera) })]
[AutoImplSystem]
public partial class CameraUpdateSystem
{
#region Execute

    private static void Execute(in Camera2D cam, [Resource] RenderContext ctx)
    {
        if (ctx.SwapchainWidth == 0 || ctx.SwapchainHeight == 0)
        {
            return;
        }

        cam.CalculateViewProjection(ctx.SwapchainWidth, ctx.SwapchainHeight, out var vp);
        ctx.ViewProjection = vp;
    }

#endregion
}
