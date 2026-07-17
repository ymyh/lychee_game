using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Key for identifying unique graphics pipeline configurations.
/// PipelineKey includes Effect and SamplerState from Material, plus depth/stencil state.
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

    /// <summary>
    /// Depth/stencil texture format of the render pass target.
    /// Must match the depth texture bound in BeginGPURenderPass when
    /// <see cref="HasDepthStencilTarget"/> is true; otherwise INVALID.
    /// </summary>
    public SDL.SDL_GPUTextureFormat DepthFormat { get; init; }

    /// <summary>
    /// Whether the pipeline declares a depth/stencil render target.
    /// Must match whether the current pass attaches a depth/stencil texture.
    /// </summary>
    public bool HasDepthStencilTarget { get; init; } = true;

    /// <summary>
    /// Enables depth testing in the graphics pipeline.
    /// </summary>
    public bool DepthTest { get; init; }

    /// <summary>
    /// Enables depth writes in the graphics pipeline.
    /// </summary>
    public bool DepthWrite { get; init; } = true;

    /// <summary>
    /// Enables stencil testing in the graphics pipeline.
    /// </summary>
    public bool StencilTest { get; init; }

    /// <summary>
    /// Depth comparison operation when <see cref="DepthTest"/> is enabled.
    /// </summary>
    public SDL.SDL_GPUCompareOp DepthCompareOp { get; init; } = SDL.SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS;

    /// <summary>
    /// Stencil compare mask.
    /// </summary>
    public byte CompareMask { get; init; } = 0xFF;

    /// <summary>
    /// Stencil write mask.
    /// </summary>
    public byte WriteMask { get; init; } = 0xFF;

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
               SampleCount == other.SampleCount &&
               DepthFormat == other.DepthFormat &&
               HasDepthStencilTarget == other.HasDepthStencilTarget &&
               DepthTest == other.DepthTest &&
               DepthWrite == other.DepthWrite &&
               StencilTest == other.StencilTest &&
               DepthCompareOp == other.DepthCompareOp &&
               CompareMask == other.CompareMask &&
               WriteMask == other.WriteMask;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PipelineKey);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Effect);
        hash.Add(Sampler);
        hash.Add(VertexAttributeHash);
        hash.Add(BlendEnabled);
        hash.Add(ColorFormat);
        hash.Add(SampleCount);
        hash.Add(DepthFormat);
        hash.Add(HasDepthStencilTarget);
        hash.Add(DepthTest);
        hash.Add(DepthWrite);
        hash.Add(StencilTest);
        hash.Add(DepthCompareOp);
        hash.Add(CompareMask);
        hash.Add(WriteMask);
        return hash.ToHashCode();
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
                enable_depth_test = key.DepthTest,
                enable_depth_write = key.DepthWrite,
                enable_stencil_test = key.StencilTest,
                compare_op = key.DepthCompareOp,
                compare_mask = key.CompareMask,
                write_mask = key.WriteMask
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
                    depth_stencil_format = key.HasDepthStencilTarget
                        ? key.DepthFormat
                        : SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID,
                    has_depth_stencil_target = key.HasDepthStencilTarget
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
