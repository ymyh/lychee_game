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

    private static void Execute([Resource] GpuDevice device, [Resource] RenderContext ctx,
        [Resource] RenderQueue queue, [Resource] SpriteInstanceBuffer instances)
    {
        if (ctx.FrameActive)
        {
            SDL.SDL_EndGPURenderPass(ctx.RenderPass);
            SDL.SDL_SubmitGPUCommandBuffer(ctx.CommandBuffer);

            Console.WriteLine($"[Render] drawCalls={ctx.DrawCallCount} sprites={queue.Records.Count}");

            ctx.FrameActive = false;
            ctx.RenderPass = IntPtr.Zero;
            ctx.CommandBuffer = IntPtr.Zero;
            ctx.SwapchainTexture = IntPtr.Zero;
        }
        else if (ctx.CommandBuffer != IntPtr.Zero)
        {
            device.Cancel(ctx.CommandBuffer);
            ctx.CommandBuffer = IntPtr.Zero;
            ctx.SwapchainTexture = IntPtr.Zero;
        }

        queue.Clear();
        instances.Clear();
    }

#endregion
}
