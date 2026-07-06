using System.Runtime.InteropServices;
using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Acquires the swapchain texture, uploads pending GPU resources, and begins the render pass.
/// </summary>
[AutoImplSystem]
public partial class BeginFrameSystem
{
#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] Window window,
        [Resource] RenderContext ctx,
        [Resource] Mesh2DList meshes, [Resource] Texture2DList textures)
    {
        if (ctx.FrameActive)
        {
            return;
        }

        ctx.CommandBuffer = device.AcquireCommandBuffer();
        ctx.SwapchainTexture = device.SwapchainTexture(ctx.CommandBuffer, window,
            out var w, out var h);
        ctx.SwapchainWidth = w;
        ctx.SwapchainHeight = h;

        // Upload any pending resources BEFORE starting the render pass (copy pass cannot nest inside render pass)
        UploadPendingResources(device, ctx.CommandBuffer, meshes, textures);

        var colorTarget = new SDL.SDL_GPUColorTargetInfo
        {
            texture = ctx.SwapchainTexture,
            load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            clear_color = new SDL.SDL_FColor { r = 0.0f, g = 0.0f, b = 0.0f, a = 1.0f }
        };

        var depthTarget = new SDL.SDL_GPUDepthStencilTargetInfo
        {
            texture = IntPtr.Zero,
            load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
            stencil_load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
            stencil_store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
            cycle = false
        };
        ctx.RenderPass = SDL.SDL_BeginGPURenderPass(ctx.CommandBuffer, [colorTarget], 1, ref depthTarget);
        ctx.FrameActive = true;
    }

#endregion

#region Private Static Methods

    private static void UploadPendingResources(GpuDevice device, IntPtr cmd,
        Mesh2DList meshes, Texture2DList textures)
    {
        foreach (var mesh in meshes.All)
        {
            if (mesh is { Uploaded: false })
            {
                UploadMesh(device, cmd, mesh);
            }
        }

        foreach (var tex in textures.All)
        {
            if (tex is { Uploaded: false })
            {
                UploadTexture(device, cmd, tex);
            }
        }
    }

    private static void UploadMesh(GpuDevice device, IntPtr cmd, Mesh2D mesh)
    {
        var vertSize = (uint)mesh.VertexData.Length;
        var vertTbCi = new SDL.SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = vertSize
        };
        var vertTb = device.CreateTransferBuffer(ref vertTbCi);

        var vertPtr = device.MapTransfer(vertTb, true);
        Marshal.Copy(mesh.VertexData, 0, vertPtr, (int)vertSize);
        device.UnmapTransfer(vertTb);

        var vertBufCi = new SDL.SDL_GPUBufferCreateInfo
        {
            usage = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = vertSize
        };
        mesh.GpuVertexBuffer = device.CreateBuffer(ref vertBufCi);

        var copyPass = SDL.SDL_BeginGPUCopyPass(cmd);

        var src = new SDL.SDL_GPUTransferBufferLocation
        {
            transfer_buffer = vertTb,
            offset = 0
        };
        var dst = new SDL.SDL_GPUBufferRegion
        {
            buffer = mesh.GpuVertexBuffer,
            offset = 0,
            size = vertSize
        };
        SDL.SDL_UploadToGPUBuffer(copyPass, ref src, ref dst, true);

        if (mesh.Indexed && mesh.IndexData != null)
        {
            var idxSize = (uint)mesh.IndexData.Length;
            var idxTbCi = new SDL.SDL_GPUTransferBufferCreateInfo
            {
                usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                size = idxSize
            };
            var idxTb = device.CreateTransferBuffer(ref idxTbCi);

            var idxPtr = device.MapTransfer(idxTb, true);
            Marshal.Copy(mesh.IndexData, 0, idxPtr, (int)idxSize);
            device.UnmapTransfer(idxTb);

            var idxBufCi = new SDL.SDL_GPUBufferCreateInfo
            {
                usage = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
                size = idxSize
            };
            mesh.GpuIndexBuffer = device.CreateBuffer(ref idxBufCi);

            var idxSrc = new SDL.SDL_GPUTransferBufferLocation
            {
                transfer_buffer = idxTb,
                offset = 0
            };
            var idxDst = new SDL.SDL_GPUBufferRegion
            {
                buffer = mesh.GpuIndexBuffer,
                offset = 0,
                size = idxSize
            };
            SDL.SDL_UploadToGPUBuffer(copyPass, ref idxSrc, ref idxDst, true);

            device.ReleaseTransferBuffer(idxTb);
        }

        SDL.SDL_EndGPUCopyPass(copyPass);
        device.ReleaseTransferBuffer(vertTb);

        mesh.Uploaded = true;
    }

    private static void UploadTexture(GpuDevice device, IntPtr cmd, Texture2D tex)
    {
        var texSize = (uint)tex.Data.Length;
        var tbCi = new SDL.SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = texSize
        };
        var tb = device.CreateTransferBuffer(ref tbCi);

        var ptr = device.MapTransfer(tb, true);
        Marshal.Copy(tex.Data, 0, ptr, (int)texSize);
        device.UnmapTransfer(tb);

        var texCi = new SDL.SDL_GPUTextureCreateInfo
        {
            type = SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = tex.Format,
            width = (uint)tex.Width,
            height = (uint)tex.Height,
            layer_count_or_depth = 1,
            num_levels = 1,
            usage = SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER
        };
        tex.GpuTexture = device.CreateTexture(ref texCi);

        var copyPass = SDL.SDL_BeginGPUCopyPass(cmd);

        var src = new SDL.SDL_GPUTextureTransferInfo
        {
            transfer_buffer = tb,
            offset = 0,
            pixels_per_row = (uint)tex.Width,
            rows_per_layer = (uint)tex.Height
        };
        var dst = new SDL.SDL_GPUTextureRegion
        {
            texture = tex.GpuTexture,
            mip_level = 0,
            layer = 0,
            w = (uint)tex.Width,
            h = (uint)tex.Height,
            d = 1
        };
        SDL.SDL_UploadToGPUTexture(copyPass, ref src, ref dst, true);

        SDL.SDL_EndGPUCopyPass(copyPass);
        device.ReleaseTransferBuffer(tb);

        tex.Uploaded = true;
    }

#endregion
}
