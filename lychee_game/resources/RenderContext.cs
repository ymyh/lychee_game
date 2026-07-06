using System.Numerics;

namespace lychee_game.resources;

/// <summary>
/// Holds the current frame's GPU rendering context.
/// Populated by BeginFrameSystem, cleared by EndFrameSystem.
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

#endregion
}
