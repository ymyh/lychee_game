using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Ends the GPU render pass and submits the command buffer (implicit present).
/// </summary>
[AutoImplSystem]
public partial class EndFrameSystem
{
#region Execute

    private static void Execute([Resource] RenderContext ctx, [Resource] RenderQueue queue)
    {
        if (!ctx.FrameActive)
        {
            return;
        }

        SDL.SDL_EndGPURenderPass(ctx.RenderPass);
        SDL.SDL_SubmitGPUCommandBuffer(ctx.CommandBuffer);

        ctx.FrameActive = false;
        ctx.RenderPass = IntPtr.Zero;
        queue.Clear();
    }

#endregion
}
