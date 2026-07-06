using System.Numerics;
using System.Runtime.InteropServices;
using lychee.attributes;
using lychee.interfaces;
using lychee_game.components._2d;
using lychee_game.resources;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.systems._2d;

/// <summary>
/// Submits sorted draw records to the GPU, handling pipeline binding and draw calls.
/// Resource uploads are handled by BeginFrameSystem before the render pass starts.
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
        }
    ];

    private static readonly SDL.SDL_GPUVertexBufferDescription[] VertexBindings =
    [
        new()
        {
            slot = 0,
            pitch = 24, // sizeof(Vertex2D)
            input_rate = SDL.SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0
        }
    ];

    private static readonly int VertexAttributeHash = HashCode.Combine(
        SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
        SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
        SDL.SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM);

#endregion

#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] Window window,
        [Resource] RenderContext ctx,
        [Resource] PipelineCache cache,
        [Resource] Mesh2DList meshes, [Resource] Texture2DList textures,
        [Resource] EffectList effects, [Resource] RenderQueue queue)
    {
        if (!ctx.FrameActive || queue.Records.Count == 0)
        {
            return;
        }

        queue.Sort();

        var fmt = device.SwapchainFormat(window);

        foreach (var r in queue.Records)
        {
            if (!meshes.TryGet(r.Mesh, out var mesh))
            {
                continue;
            }

            if (!effects.TryGet(r.Effect, out var effect))
            {
                continue;
            }

            // Get or create pipeline
            var key = new PipelineKey
            {
                Effect = r.Effect,
                Sampler = r.Sampler,
                VertexAttributeHash = VertexAttributeHash,
                BlendEnabled = true,
                ColorFormat = fmt,
                SampleCount = SDL.SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1
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
                    pipeline = cache.GetOrCreate(key, effect!, vertexInput);
                }
            }

            // Bind pipeline
            SDL.SDL_BindGPUGraphicsPipeline(ctx.RenderPass, pipeline);

            // Set viewport
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

            // Bind vertex buffer
            var vertBinding = new SDL.SDL_GPUBufferBinding
            {
                buffer = mesh!.GpuVertexBuffer,
                offset = 0
            };
            SDL.SDL_BindGPUVertexBuffers(ctx.RenderPass, 0, [vertBinding], 1);

            // Bind index buffer if indexed
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

            // Bind texture sampler (default slot 0 = WhiteTexture, always valid)
            if (textures.TryGet(r.Texture, out var tex) && tex!.Uploaded)
            {
                var samplerHandle = device.GetOrCreateSampler(r.Sampler);
                var texSamplerBinding = new SDL.SDL_GPUTextureSamplerBinding
                {
                    texture = tex.GpuTexture,
                    sampler = samplerHandle
                };
                SDL.SDL_BindGPUFragmentSamplers(ctx.RenderPass, 0, [texSamplerBinding], 1);
            }

            // Push uniform data (MVP + Tint)
            var mvp = ctx.ViewProjection * r.WorldMatrix;
            var tint = new Vector4(r.Tint.R / 255.0f, r.Tint.G / 255.0f,
                r.Tint.B / 255.0f, r.Tint.A / 255.0f);

            var uniformData = new byte[80]; // 64 (MVP) + 16 (Tint)
            MemoryMarshal.Write(uniformData.AsSpan(0, 64), in mvp);
            MemoryMarshal.Write(uniformData.AsSpan(64, 16), in tint);

            unsafe
            {
                fixed (byte* p = uniformData)
                {
                    SDL.SDL_PushGPUVertexUniformData(ctx.CommandBuffer, 0, (IntPtr)p, 80);
                }
            }

            // Draw
            if (mesh.Indexed)
            {
                SDL.SDL_DrawGPUIndexedPrimitives(ctx.RenderPass,
                    (uint)mesh.IndexCount, 1, 0, 0, 0);
            }
            else
            {
                SDL.SDL_DrawGPUPrimitives(ctx.RenderPass,
                    (uint)mesh.VertexCount, 1, 0, 0);
            }
        }
    }

#endregion
}
