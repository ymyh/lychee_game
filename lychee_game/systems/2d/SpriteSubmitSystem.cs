using System.Runtime.CompilerServices;
using lychee.attributes;
using lychee.interfaces;
using lychee_game.components._2d;
using lychee_game.resources;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Submits sorted draw records with state batching and GPU instancing.
/// Instance data is uploaded by <see cref="SpriteInstanceUploadSystem"/> before the render pass.
/// </summary>
[AutoImplSystem]
public partial class SpriteSubmitSystem
{
#region Static Fields

    private static readonly SDL.SDL_GPUVertexAttribute[] VertexAttributes =
    [
        new()
        {
            location = 0,
            buffer_slot = 0,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = 0
        },
        new()
        {
            location = 1,
            buffer_slot = 0,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = 8
        },
        new()
        {
            location = 2,
            buffer_slot = 0,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
            offset = 16
        },
        new()
        {
            location = 3,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 0
        },
        new()
        {
            location = 4,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 16
        },
        new()
        {
            location = 5,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 32
        },
        new()
        {
            location = 6,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 48
        },
        new()
        {
            location = 7,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            offset = 64
        },
        new()
        {
            location = 8,
            buffer_slot = 1,
            format = SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
            offset = 80
        }
    ];

    private static readonly SDL.SDL_GPUVertexBufferDescription[] VertexBindings =
    [
        new()
        {
            slot = 0,
            pitch = (uint)Unsafe.SizeOf<Vertex2D>(),
            input_rate = SDL.SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0
        },
        new()
        {
            slot = 1,
            pitch = (uint)Unsafe.SizeOf<SpriteInstance>(),
            input_rate = SDL.SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_INSTANCE,
            // SDL3 requires instance_step_rate == 0; INSTANCE rate advances one element per instance.
            instance_step_rate = 0
        }
    ];

    private static readonly int VertexAttributeHash = HashCode.Combine(
        Unsafe.SizeOf<Vertex2D>(),
        Unsafe.SizeOf<SpriteInstance>(),
        SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
        SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM);

#endregion

#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] Window window,
        [Resource] RenderContext ctx,
        [Resource] PipelineCache cache,
        [Resource] Mesh2DList meshes, [Resource] Texture2DList textures,
        [Resource] EffectList effects, [Resource] RenderQueue queue,
        [Resource] SpriteInstanceBuffer instances)
    {
        ctx.DrawCallCount = 0;

        if (!ctx.FrameActive || queue.Records.Count == 0 || instances.GpuBuffer == IntPtr.Zero)
        {
            return;
        }

        var records = queue.Records;
        var fmt = device.SwapchainFormat(window);
        var viewportSet = false;

        var i = 0;
        while (i < records.Count)
        {
            var head = records[i];
            if (!meshes.TryGet(head.Mesh, out var mesh) ||
                !effects.TryGet(head.Effect, out var effect) ||
                mesh is null || effect is null)
            {
                i++;
                continue;
            }

            var j = i + 1;
            while (j < records.Count && SameBatch(head, records[j]))
            {
                j++;
            }

            var batchCount = (uint)(j - i);

            var ds = ctx.DepthStencil;
            var key = new PipelineKey
            {
                Effect = head.Effect,
                Sampler = head.Sampler,
                VertexAttributeHash = VertexAttributeHash,
                BlendEnabled = true,
                ColorFormat = fmt,
                SampleCount = SDL.SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
                DepthFormat = ds.AttachDepthStencil
                    ? device.DepthFormat
                    : SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID,
                HasDepthStencilTarget = ds.AttachDepthStencil,
                DepthTest = ds.DepthTest,
                DepthWrite = ds.DepthWrite,
                StencilTest = ds.StencilTest,
                DepthCompareOp = ds.DepthCompareOp,
                CompareMask = ds.CompareMask,
                WriteMask = ds.WriteMask
            };

            IntPtr pipeline;
            unsafe
            {
                fixed (SDL.SDL_GPUVertexAttribute* pAttrs = VertexAttributes)
                fixed (SDL.SDL_GPUVertexBufferDescription* pBindings = VertexBindings)
                {
                    var vertexInput = new SDL.SDL_GPUVertexInputState
                    {
                        vertex_attributes = pAttrs,
                        num_vertex_attributes = (uint)VertexAttributes.Length,
                        vertex_buffer_descriptions = pBindings,
                        num_vertex_buffers = (uint)VertexBindings.Length
                    };
                    pipeline = cache.GetOrCreate(key, effect, vertexInput);
                }
            }

            SDL.SDL_BindGPUGraphicsPipeline(ctx.RenderPass, pipeline);

            if (!viewportSet)
            {
                var viewport = new SDL.SDL_GPUViewport
                {
                    x = 0,
                    y = 0,
                    w = ctx.SwapchainWidth,
                    h = ctx.SwapchainHeight,
                    min_depth = 0.0f,
                    max_depth = 1.0f
                };
                SDL.SDL_SetGPUViewport(ctx.RenderPass, ref viewport);
                viewportSet = true;
            }

            SDL.SDL_BindGPUVertexBuffers(ctx.RenderPass, 0,
            [
                new SDL.SDL_GPUBufferBinding { buffer = mesh.GpuVertexBuffer, offset = 0 },
                new SDL.SDL_GPUBufferBinding { buffer = instances.GpuBuffer, offset = 0 }
            ], 2);

            if (mesh.Indexed)
            {
                var indexBinding = new SDL.SDL_GPUBufferBinding
                {
                    buffer = mesh.GpuIndexBuffer,
                    offset = 0
                };
                SDL.SDL_BindGPUIndexBuffer(ctx.RenderPass, ref indexBinding,
                    SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT);
            }

            if (textures.TryGet(head.Texture, out var tex) && tex is { Uploaded: true })
            {
                var samplerHandle = device.GetOrCreateSampler(head.Sampler);
                var texSamplerBinding = new SDL.SDL_GPUTextureSamplerBinding
                {
                    texture = tex.GpuTexture,
                    sampler = samplerHandle
                };
                SDL.SDL_BindGPUFragmentSamplers(ctx.RenderPass, 0, [texSamplerBinding], 1);
            }

            var vp = ctx.ViewProjection;
            unsafe
            {
                SDL.SDL_PushGPUVertexUniformData(ctx.CommandBuffer, 0, (IntPtr)(&vp), 64);
            }

            var firstInstance = (uint)i;
            if (mesh.Indexed)
            {
                SDL.SDL_DrawGPUIndexedPrimitives(ctx.RenderPass,
                    (uint)mesh.IndexCount, batchCount, 0, 0, firstInstance);
            }
            else
            {
                SDL.SDL_DrawGPUPrimitives(ctx.RenderPass,
                    (uint)mesh.VertexCount, batchCount, 0, firstInstance);
            }

            ctx.DrawCallCount++;
            i = j;
        }
    }

#endregion

#region Private Static Methods

    private static bool SameBatch(in DrawRecord a, in DrawRecord b)
    {
        return a.Effect.Index == b.Effect.Index &&
               a.Effect.Generation == b.Effect.Generation &&
               a.Texture.Index == b.Texture.Index &&
               a.Texture.Generation == b.Texture.Generation &&
               a.Mesh.Index == b.Mesh.Index &&
               a.Mesh.Generation == b.Mesh.Generation &&
               Equals(a.Sampler, b.Sampler);
    }

#endregion
}
