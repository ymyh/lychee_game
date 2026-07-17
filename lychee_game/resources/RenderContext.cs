using System.Numerics;
using lychee_game.resources._2d;

namespace lychee_game.resources;

/// <summary>
/// Holds the current frame's GPU rendering context.
/// Command buffer is acquired by BeginFrameSystem; the render pass is begun by BeginRenderPassSystem
/// and ended by EndFrameSystem.
/// </summary>
public sealed class RenderContext
{
#region Public Properties

    /// <summary>
    /// The current frame's command buffer.
    /// </summary>
    public IntPtr CommandBuffer { get; internal set; }

    /// <summary>
    /// The current swapchain texture for rendering.
    /// </summary>
    public IntPtr SwapchainTexture { get; internal set; }

    /// <summary>
    /// The swapchain width in pixels.
    /// </summary>
    public uint SwapchainWidth { get; internal set; }

    /// <summary>
    /// The swapchain height in pixels.
    /// </summary>
    public uint SwapchainHeight { get; internal set; }

    /// <summary>
    /// The current frame's depth/stencil texture handle (recreated on swapchain resize).
    /// </summary>
    public IntPtr DepthTexture { get; internal set; }

    /// <summary>
    /// The current depth texture width in pixels.
    /// </summary>
    public uint DepthWidth { get; internal set; }

    /// <summary>
    /// The current depth texture height in pixels.
    /// </summary>
    public uint DepthHeight { get; internal set; }

    /// <summary>
    /// The active render pass handle (non-zero between Begin and End).
    /// </summary>
    public IntPtr RenderPass { get; internal set; }

    /// <summary>
    /// The combined view-projection matrix for the current camera.
    /// </summary>
    public Matrix4x4 ViewProjection { get; set; }

    /// <summary>
    /// Whether a frame is currently active (between BeginFrame and EndFrame).
    /// </summary>
    public bool FrameActive { get; internal set; }

    /// <summary>
    /// Number of GPU draw calls issued during the current frame's submit.
    /// </summary>
    public int DrawCallCount { get; set; }

    /// <summary>
    /// Depth/stencil attachment and pipeline settings for the current frame's render pass.
    /// Change before <c>BeginRenderPassSystem</c> runs. Defaults to <see cref="DepthStencilSettings.Default2D"/>.
    /// </summary>
    public DepthStencilSettings DepthStencil { get; set; } = DepthStencilSettings.Default2D;

#endregion
}
