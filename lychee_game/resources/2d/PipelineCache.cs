using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Key for identifying unique graphics pipeline configurations.
/// PipelineKey includes Effect and SamplerState from Material.
/// </summary>
public sealed class PipelineKey : IEquatable<PipelineKey>
{
#region Public Properties

    /// <summary>
    /// The effect reference (from Material).
    /// </summary>
    public components._2d.EffectRef Effect { get; init; }

    /// <summary>
    /// Sampler state (from Material). Different sampler params produce different pipelines.
    /// </summary>
    public SamplerState Sampler { get; init; } = new();

    /// <summary>
    /// Vertex attribute hash for pipeline compatibility.
    /// </summary>
    public int VertexAttributeHash { get; init; }

    /// <summary>
    /// Whether alpha blending is enabled.
    /// </summary>
    public bool BlendEnabled { get; init; }

    /// <summary>
    /// Swapchain color texture format.
    /// </summary>
    public SDL.SDL_GPUTextureFormat ColorFormat { get; init; }

    /// <summary>
    /// Multisample count.
    /// </summary>
    public SDL.SDL_GPUSampleCount SampleCount { get; init; }

#endregion

#region Equality

    /// <inheritdoc/>
    public bool Equals(PipelineKey? other)
    {
        if (other is null)
        {
            return false;
        }

        return Effect.Equals(other.Effect) &&
               Equals(Sampler, other.Sampler) &&
               VertexAttributeHash == other.VertexAttributeHash &&
               BlendEnabled == other.BlendEnabled &&
               ColorFormat == other.ColorFormat &&
               SampleCount == other.SampleCount;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PipelineKey);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Effect, Sampler, VertexAttributeHash, BlendEnabled, ColorFormat, SampleCount);
    }

#endregion
}

/// <summary>
/// Caches graphics pipelines to avoid redundant creation.
/// </summary>
public sealed class PipelineCache
{
#region Private Fields

    private readonly Dictionary<PipelineKey, IntPtr> cache = [];

    private readonly GpuDevice device;

#endregion

#region Constructor

    /// <summary>
    /// Creates a PipelineCache bound to the specified GPU device.
    /// </summary>
    public PipelineCache(GpuDevice device)
    {
        this.device = device;
    }

#endregion

#region Public Methods

    /// <summary>
    /// Gets or creates a graphics pipeline for the specified configuration.
    /// </summary>
    /// <param name="key">The pipeline configuration key.</param>
    /// <param name="effect">The effect containing compiled shaders.</param>
    /// <param name="vertexInput">Vertex input state description.</param>
    /// <returns>The graphics pipeline handle.</returns>
    public IntPtr GetOrCreate(PipelineKey key, Effect effect, in SDL.SDL_GPUVertexInputState vertexInput)
    {
        if (cache.TryGetValue(key, out var pipeline))
        {
            return pipeline;
        }

        var colorTarget = new SDL.SDL_GPUColorTargetDescription
        {
            format = key.ColorFormat,
            blend_state = new SDL.SDL_GPUColorTargetBlendState
            {
                enable_blend = key.BlendEnabled,
                color_blend_op = SDL.SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                alpha_blend_op = SDL.SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                src_color_blendfactor = SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                dst_color_blendfactor = SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                src_alpha_blendfactor = SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_alpha_blendfactor = SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                color_write_mask = SDL.SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_R |
                                   SDL.SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_G |
                                   SDL.SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_B |
                                   SDL.SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_A
            }
        };

        var ci = new SDL.SDL_GPUGraphicsPipelineCreateInfo
        {
            vertex_shader = effect.VertexShader,
            fragment_shader = effect.FragmentShader,
            vertex_input_state = vertexInput,
            primitive_type = SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
            rasterizer_state = new SDL.SDL_GPURasterizerState
            {
                fill_mode = SDL.SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                cull_mode = SDL.SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                front_face = SDL.SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE
            },
            multisample_state = new SDL.SDL_GPUMultisampleState
            {
                sample_count = key.SampleCount
            },
            depth_stencil_state = new SDL.SDL_GPUDepthStencilState
            {
                enable_depth_test = false,
                enable_depth_write = false,
                enable_stencil_test = false
            }
        };

        unsafe
        {
            var colorTargets = new SDL.SDL_GPUColorTargetDescription[] { colorTarget };
            fixed (SDL.SDL_GPUColorTargetDescription* pTargets = colorTargets)
            {
                ci.target_info = new SDL.SDL_GPUGraphicsPipelineTargetInfo
                {
                    color_target_descriptions = pTargets,
                    num_color_targets = 1,
                    has_depth_stencil_target = false
                };
                pipeline = device.CreatePipeline(ref ci);
            }
        }

        cache[key] = pipeline;
        return pipeline;
    }

    /// <summary>
    /// Destroys all cached pipelines. Called during plugin shutdown.
    /// </summary>
    internal void DestroyAll()
    {
        foreach (var pipeline in cache.Values)
        {
            device.ReleasePipeline(pipeline);
        }

        cache.Clear();
    }

#endregion
}
