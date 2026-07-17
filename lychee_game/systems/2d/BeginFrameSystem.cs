using System.Runtime.InteropServices;
using lychee.attributes;
using lychee_game.resources;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Acquires the command buffer and swapchain, and uploads pending mesh/texture resources.
/// Does not begin the render pass (that is <see cref="BeginRenderPassSystem"/>).
/// </summary>
[AutoImplSystem]
public partial class BeginFrameSystem
{
#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] Window window,
        [Resource] RenderContext ctx, [Resource] GpuUploadBuffer upload,
        [Resource] Mesh2DList meshes, [Resource] Texture2DList textures)
    {
        if (ctx.FrameActive || ctx.CommandBuffer != IntPtr.Zero)
        {
            return;
        }

        ctx.CommandBuffer = device.AcquireCommandBuffer();
        if (ctx.CommandBuffer == IntPtr.Zero)
        {
            return;
        }

        ctx.SwapchainTexture = device.SwapchainTexture(ctx.CommandBuffer, window,
            out var w, out var h);
        ctx.SwapchainWidth = w;
        ctx.SwapchainHeight = h;

        if (ctx.SwapchainTexture == IntPtr.Zero)
        {
            device.Cancel(ctx.CommandBuffer);
            ctx.CommandBuffer = IntPtr.Zero;
            return;
        }

        EnsureDepthTexture(device, ctx, w, h);

        // Copy passes cannot nest inside a render pass.
        UploadPendingResources(device, ctx.CommandBuffer, upload, meshes, textures);
    }

#endregion

#region Private Static Methods

    private static void EnsureDepthTexture(GpuDevice device, RenderContext ctx, uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        if (ctx.DepthTexture != IntPtr.Zero && ctx.DepthWidth == width && ctx.DepthHeight == height)
        {
            return;
        }

        if (ctx.DepthTexture != IntPtr.Zero)
        {
            device.ReleaseTexture(ctx.DepthTexture);
            ctx.DepthTexture = IntPtr.Zero;
        }

        var ci = new SDL.SDL_GPUTextureCreateInfo
        {
            type = SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = device.DepthFormat,
            width = width,
            height = height,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL.SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
            usage = SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET
        };
        ctx.DepthTexture = device.CreateTexture(ref ci);
        ctx.DepthWidth = width;
        ctx.DepthHeight = height;
    }

    private static void UploadPendingResources(GpuDevice device, IntPtr cmd, GpuUploadBuffer upload,
        Mesh2DList meshes, Texture2DList textures)
    {
        var totalBytes = 0u;
        var pendingMeshes = 0;
        var pendingTextures = 0;

        foreach (var mesh in meshes.All)
        {
            if (mesh is { Uploaded: false })
            {
                pendingMeshes++;
                totalBytes = Align4(totalBytes + (uint)mesh.VertexData.Length);
                if (mesh.Indexed && mesh.IndexData != null)
                {
                    totalBytes = Align4(totalBytes + (uint)mesh.IndexData.Length);
                }
            }
        }

        foreach (var tex in textures.All)
        {
            if (tex is { Uploaded: false })
            {
                pendingTextures++;
                totalBytes = Align4(totalBytes + (uint)tex.Data.Length);
            }
        }

        if (totalBytes == 0)
        {
            return;
        }

        var bufferUploads = new List<BufferUpload>(pendingMeshes * 2);
        var textureUploads = new List<TextureUpload>(pendingTextures);

        upload.EnsureCapacity(device, totalBytes);
        var mapped = upload.Map(device);
        var offset = 0u;

        try
        {
            foreach (var mesh in meshes.All)
            {
                if (mesh is not { Uploaded: false })
                {
                    continue;
                }

                var vertSize = (uint)mesh.VertexData.Length;
                Marshal.Copy(mesh.VertexData, 0, mapped + (nint)offset, (int)vertSize);

                var vertBufCi = new SDL.SDL_GPUBufferCreateInfo
                {
                    usage = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
                    size = vertSize
                };
                mesh.GpuVertexBuffer = device.CreateBuffer(ref vertBufCi);
                bufferUploads.Add(new BufferUpload(offset, vertSize, mesh.GpuVertexBuffer));
                offset = Align4(offset + vertSize);

                if (mesh.Indexed && mesh.IndexData != null)
                {
                    var idxSize = (uint)mesh.IndexData.Length;
                    Marshal.Copy(mesh.IndexData, 0, mapped + (nint)offset, (int)idxSize);

                    var idxBufCi = new SDL.SDL_GPUBufferCreateInfo
                    {
                        usage = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
                        size = idxSize
                    };
                    mesh.GpuIndexBuffer = device.CreateBuffer(ref idxBufCi);
                    bufferUploads.Add(new BufferUpload(offset, idxSize, mesh.GpuIndexBuffer));
                    offset = Align4(offset + idxSize);
                }

                mesh.Uploaded = true;
            }

            foreach (var tex in textures.All)
            {
                if (tex is not { Uploaded: false })
                {
                    continue;
                }

                var texSize = (uint)tex.Data.Length;
                Marshal.Copy(tex.Data, 0, mapped + (nint)offset, (int)texSize);

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
                textureUploads.Add(new TextureUpload(offset, (uint)tex.Width, (uint)tex.Height,
                    tex.GpuTexture));
                offset = Align4(offset + texSize);

                tex.Uploaded = true;
            }
        }
        finally
        {
            upload.Unmap(device);
        }

        var copyPass = SDL.SDL_BeginGPUCopyPass(cmd);

        foreach (var u in bufferUploads)
        {
            var src = new SDL.SDL_GPUTransferBufferLocation
            {
                transfer_buffer = upload.Handle,
                offset = u.SrcOffset
            };
            var dst = new SDL.SDL_GPUBufferRegion
            {
                buffer = u.DstBuffer,
                offset = 0,
                size = u.Size
            };
            SDL.SDL_UploadToGPUBuffer(copyPass, ref src, ref dst, true);
        }

        foreach (var u in textureUploads)
        {
            var src = new SDL.SDL_GPUTextureTransferInfo
            {
                transfer_buffer = upload.Handle,
                offset = u.SrcOffset,
                pixels_per_row = u.Width,
                rows_per_layer = u.Height
            };
            var dst = new SDL.SDL_GPUTextureRegion
            {
                texture = u.DstTexture,
                mip_level = 0,
                layer = 0,
                w = u.Width,
                h = u.Height,
                d = 1
            };
            SDL.SDL_UploadToGPUTexture(copyPass, ref src, ref dst, true);
        }

        SDL.SDL_EndGPUCopyPass(copyPass);
    }

    private static uint Align4(uint value)
    {
        return (value + 3u) & ~3u;
    }

#endregion

#region Nested Types

    private readonly struct BufferUpload
    {
        public readonly uint SrcOffset;

        public readonly uint Size;

        public readonly IntPtr DstBuffer;

        public BufferUpload(uint srcOffset, uint size, IntPtr dstBuffer)
        {
            SrcOffset = srcOffset;
            Size = size;
            DstBuffer = dstBuffer;
        }
    }

    private readonly struct TextureUpload
    {
        public readonly uint SrcOffset;

        public readonly uint Width;

        public readonly uint Height;

        public readonly IntPtr DstTexture;

        public TextureUpload(uint srcOffset, uint width, uint height, IntPtr dstTexture)
        {
            SrcOffset = srcOffset;
            Width = width;
            Height = height;
            DstTexture = dstTexture;
        }
    }

#endregion
}
