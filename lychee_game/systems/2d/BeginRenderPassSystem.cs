using lychee.attributes;
using lychee_game.resources;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Begins the GPU render pass using color clear and <see cref="RenderContext.DepthStencil"/> settings.
/// Runs after instance data has been uploaded.
/// </summary>
[AutoImplSystem]
public partial class BeginRenderPassSystem
{
#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] RenderContext ctx)
    {
        if (ctx.FrameActive || ctx.CommandBuffer == IntPtr.Zero || ctx.SwapchainTexture == IntPtr.Zero)
        {
            return;
        }

        SDL.SDL_GPUColorTargetInfo[] colorTargets =
        [
            new()
            {
                texture = ctx.SwapchainTexture,
                load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                clear_color = new SDL.SDL_FColor { r = 0.0f, g = 0.0f, b = 0.0f, a = 1.0f }
            }
        ];

        var settings = ctx.DepthStencil;

        if (settings.AttachDepthStencil)
        {
            var depthTarget = new SDL.SDL_GPUDepthStencilTargetInfo
            {
                texture = ctx.DepthTexture,
                clear_depth = settings.ClearDepth,
                load_op = settings.DepthLoadOp,
                store_op = settings.DepthStoreOp,
                stencil_load_op = settings.StencilLoadOp,
                stencil_store_op = settings.StencilStoreOp,
                cycle = settings.CycleDepth
            };
            ctx.RenderPass = device.BeginRenderPass(ctx.CommandBuffer, colorTargets, ref depthTarget);
        }
        else
        {
            ctx.RenderPass = device.BeginRenderPass(ctx.CommandBuffer, colorTargets);
        }

        ctx.FrameActive = true;
    }

#endregion
}
