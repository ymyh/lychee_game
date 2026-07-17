using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Configurable depth/stencil behavior for the render pass attachment and graphics pipeline.
/// Assign to <see cref="RenderContext.DepthStencil"/> before <c>BeginRenderPassSystem</c> runs.
/// </summary>
public sealed class DepthStencilSettings : IEquatable<DepthStencilSettings>
{
#region Public Properties

    /// <summary>
    /// When true, the render pass binds a depth/stencil target and pipelines declare a matching target.
    /// </summary>
    public bool AttachDepthStencil { get; init; } = true;

    /// <summary>
    /// Load operation for the depth plane.
    /// </summary>
    public SDL.SDL_GPULoadOp DepthLoadOp { get; init; } = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR;

    /// <summary>
    /// Store operation for the depth plane.
    /// </summary>
    public SDL.SDL_GPUStoreOp DepthStoreOp { get; init; } = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE;

    /// <summary>
    /// Clear depth value used when <see cref="DepthLoadOp"/> is CLEAR.
    /// </summary>
    public float ClearDepth { get; init; } = 1.0f;

    /// <summary>
    /// Load operation for the stencil plane.
    /// </summary>
    public SDL.SDL_GPULoadOp StencilLoadOp { get; init; } = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE;

    /// <summary>
    /// Store operation for the stencil plane.
    /// </summary>
    public SDL.SDL_GPUStoreOp StencilStoreOp { get; init; } = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE;

    /// <summary>
    /// Whether the depth/stencil target may be cycled by the backend.
    /// </summary>
    public bool CycleDepth { get; init; } = true;

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

#region Static Presets

    /// <summary>
    /// Default 2D settings: depth attached, cleared and stored, depth write on, depth/stencil tests off.
    /// </summary>
    public static DepthStencilSettings Default2D { get; } = new();

    /// <summary>
    /// No depth/stencil attachment and all depth/stencil pipeline features disabled.
    /// </summary>
    public static DepthStencilSettings Disabled { get; } = new()
    {
        AttachDepthStencil = false,
        DepthLoadOp = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
        DepthStoreOp = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
        DepthTest = false,
        DepthWrite = false,
        StencilTest = false,
        CycleDepth = false
    };

#endregion

#region Equality

    /// <inheritdoc/>
    public bool Equals(DepthStencilSettings? other)
    {
        if (other is null)
        {
            return false;
        }

        return AttachDepthStencil == other.AttachDepthStencil &&
               DepthLoadOp == other.DepthLoadOp &&
               DepthStoreOp == other.DepthStoreOp &&
               ClearDepth.Equals(other.ClearDepth) &&
               StencilLoadOp == other.StencilLoadOp &&
               StencilStoreOp == other.StencilStoreOp &&
               CycleDepth == other.CycleDepth &&
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
        return Equals(obj as DepthStencilSettings);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AttachDepthStencil);
        hash.Add(DepthLoadOp);
        hash.Add(DepthStoreOp);
        hash.Add(ClearDepth);
        hash.Add(StencilLoadOp);
        hash.Add(StencilStoreOp);
        hash.Add(CycleDepth);
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
