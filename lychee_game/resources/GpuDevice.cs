using System.Runtime.InteropServices;
using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.resources;

/// <summary>
/// Wraps an SDL3 GPU device handle, providing resource creation and lifecycle management.
/// </summary>
public sealed class GpuDevice
{
#region Public Properties

    /// <summary>
    /// The native SDL3 GPU device handle.
    /// </summary>
    public IntPtr Handle { get; }

    /// <summary>
    /// The shader format supported by this device.
    /// </summary>
    public SDL.SDL_GPUShaderFormat ShaderFormat { get; }

    /// <summary>
    /// Depth/stencil texture format used for render pass depth targets.
    /// D16_UNORM is the 2D convention (saves memory vs D32, sufficient precision).
    /// </summary>
    public SDL.SDL_GPUTextureFormat DepthFormat { get; } =
        SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM;

#endregion

#region Private Fields

    private bool disposed;

    private IntPtr windowHandle;

    private readonly Dictionary<SamplerState, IntPtr> samplerCache = [];

#endregion

#region Constructor

    /// <summary>
    /// Creates a GPU device and claims the specified window for rendering.
    /// Uses <see cref="Window.Backend"/> to select the SDL GPU driver and shader format.
    /// </summary>
    /// <param name="window">The window to claim for GPU rendering.</param>
    /// <param name="debugMode">Whether to enable GPU debug mode.</param>
    /// <param name="presentMode">Swapchain present mode.</param>
    public GpuDevice(Window window, bool debugMode = true,
        SDL.SDL_GPUPresentMode presentMode = SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC)
    {
        var (format, name) = ResolveBackend(window.Backend);

        if (!SDL.SDL_GPUSupportsShaderFormats(format, name))
        {
            throw new InvalidOperationException(
                $"GPU backend '{name}' (from Window.Backend={window.Backend}) is not supported on this system.");
        }

        Handle = SDL.SDL_CreateGPUDevice(format, debugMode, name);
        if (Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to create GPU device for backend '{name}': {SDL.SDL_GetError()}");
        }

        ShaderFormat = format;

        if (!SDL.SDL_ClaimWindowForGPUDevice(Handle, window.Handle))
        {
            SDL.SDL_DestroyGPUDevice(Handle);
            throw new InvalidOperationException($"Failed to claim window for GPU: {SDL.SDL_GetError()}");
        }

        windowHandle = window.Handle;

        SDL.SDL_SetGPUSwapchainParameters(Handle, window.Handle,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
            presentMode);
    }

#endregion

#region Private Static Methods

    private static (SDL.SDL_GPUShaderFormat Format, string Name) ResolveBackend(RenderingBackend backend)
    {
        return backend switch
        {
            RenderingBackend.Vulkan => (SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV, "Vulkan"),
            RenderingBackend.D3D12 => (SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL, "D3D12"),
            RenderingBackend.Metal => (SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL, "Metal"),
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported rendering backend.")
        };
    }

#endregion

#region Public Methods

    /// <summary>
    /// Acquires a command buffer for the current frame.
    /// </summary>
    public IntPtr AcquireCommandBuffer()
    {
        return SDL.SDL_AcquireGPUCommandBuffer(Handle);
    }

    /// <summary>
    /// Submits a command buffer for execution (includes implicit present).
    /// </summary>
    /// <param name="commandBuffer">The command buffer to submit.</param>
    public void Submit(IntPtr commandBuffer)
    {
        SDL.SDL_SubmitGPUCommandBuffer(commandBuffer);
    }

    /// <summary>
    /// Cancels an acquired command buffer without submitting (e.g. when swapchain acquire fails).
    /// </summary>
    /// <param name="commandBuffer">The command buffer to cancel.</param>
    public void Cancel(IntPtr commandBuffer)
    {
        SDL.SDL_CancelGPUCommandBuffer(commandBuffer);
    }

    /// <summary>
    /// Begins a GPU render pass with a depth/stencil target.
    /// </summary>
    /// <param name="commandBuffer">The current command buffer.</param>
    /// <param name="colorTargets">Color target infos.</param>
    /// <param name="depthTarget">Depth/stencil target info.</param>
    /// <returns>The render pass handle.</returns>
    public IntPtr BeginRenderPass(IntPtr commandBuffer, SDL.SDL_GPUColorTargetInfo[] colorTargets,
        ref SDL.SDL_GPUDepthStencilTargetInfo depthTarget)
    {
        return SDL.SDL_BeginGPURenderPass(commandBuffer, colorTargets, (uint)colorTargets.Length,
            ref depthTarget);
    }

    /// <summary>
    /// Begins a GPU render pass with color targets only (no depth/stencil attachment).
    /// </summary>
    /// <param name="commandBuffer">The current command buffer.</param>
    /// <param name="colorTargets">Color target infos.</param>
    /// <returns>The render pass handle.</returns>
    public IntPtr BeginRenderPass(IntPtr commandBuffer, SDL.SDL_GPUColorTargetInfo[] colorTargets)
    {
        // SDL3-CS only exposes a ref depth parameter; pass NULL via a matching native signature.
        return BeginGPURenderPassNoDepth(commandBuffer, colorTargets, (uint)colorTargets.Length,
            IntPtr.Zero);
    }

    /// <summary>
    /// Acquires the swapchain texture for rendering.
    /// </summary>
    /// <param name="commandBuffer">The current command buffer.</param>
    /// <param name="window">The window to acquire the swapchain from.</param>
    /// <param name="w">Output: swapchain width.</param>
    /// <param name="h">Output: swapchain height.</param>
    /// <returns>The swapchain texture handle.</returns>
    public IntPtr SwapchainTexture(IntPtr commandBuffer, Window window, out uint w, out uint h)
    {
        SDL.SDL_AcquireGPUSwapchainTexture(commandBuffer, window.Handle, out var tex, out w, out h);
        return tex;
    }

    /// <summary>
    /// Gets the swapchain texture format for the specified window.
    /// </summary>
    /// <param name="window">The window to query.</param>
    /// <returns>The texture format of the swapchain.</returns>
    public SDL.SDL_GPUTextureFormat SwapchainFormat(Window window)
    {
        return SDL.SDL_GetGPUSwapchainTextureFormat(Handle, window.Handle);
    }

    /// <summary>
    /// Creates a GPU shader from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreateShader(ref SDL.SDL_GPUShaderCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUShader(Handle, ref ci), "SDL_CreateGPUShader");
    }

    /// <summary>
    /// Creates a GPU graphics pipeline from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreatePipeline(ref SDL.SDL_GPUGraphicsPipelineCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUGraphicsPipeline(Handle, ref ci),
            "SDL_CreateGPUGraphicsPipeline");
    }

    /// <summary>
    /// Creates a GPU sampler from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreateSampler(ref SDL.SDL_GPUSamplerCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUSampler(Handle, ref ci), "SDL_CreateGPUSampler");
    }

    /// <summary>
    /// Creates a GPU texture from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreateTexture(ref SDL.SDL_GPUTextureCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUTexture(Handle, ref ci), "SDL_CreateGPUTexture");
    }

    /// <summary>
    /// Creates a GPU buffer from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreateBuffer(ref SDL.SDL_GPUBufferCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUBuffer(Handle, ref ci), "SDL_CreateGPUBuffer");
    }

    /// <summary>
    /// Creates a GPU transfer buffer from the specified create info.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null handle.</exception>
    public IntPtr CreateTransferBuffer(ref SDL.SDL_GPUTransferBufferCreateInfo ci)
    {
        return CheckHandle(SDL.SDL_CreateGPUTransferBuffer(Handle, ref ci),
            "SDL_CreateGPUTransferBuffer");
    }

    /// <summary>
    /// Maps a transfer buffer for CPU access.
    /// </summary>
    /// <param name="tb">The transfer buffer to map.</param>
    /// <param name="cycle">Whether to cycle the buffer.</param>
    /// <returns>A pointer to the mapped memory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when SDL returns a null pointer.</exception>
    public IntPtr MapTransfer(IntPtr tb, bool cycle)
    {
        return CheckHandle(SDL.SDL_MapGPUTransferBuffer(Handle, tb, cycle),
            "SDL_MapGPUTransferBuffer");
    }

    /// <summary>
    /// Unmaps a previously mapped transfer buffer.
    /// </summary>
    /// <param name="tb">The transfer buffer to unmap.</param>
    public void UnmapTransfer(IntPtr tb)
    {
        SDL.SDL_UnmapGPUTransferBuffer(Handle, tb);
    }

    /// <summary>
    /// Releases a GPU shader resource.
    /// </summary>
    public void ReleaseShader(IntPtr shader)
    {
        SDL.SDL_ReleaseGPUShader(Handle, shader);
    }

    /// <summary>
    /// Releases a GPU texture resource.
    /// </summary>
    public void ReleaseTexture(IntPtr texture)
    {
        SDL.SDL_ReleaseGPUTexture(Handle, texture);
    }

    /// <summary>
    /// Releases a GPU buffer resource.
    /// </summary>
    public void ReleaseBuffer(IntPtr buffer)
    {
        SDL.SDL_ReleaseGPUBuffer(Handle, buffer);
    }

    /// <summary>
    /// Releases a GPU sampler resource.
    /// </summary>
    public void ReleaseSampler(IntPtr sampler)
    {
        SDL.SDL_ReleaseGPUSampler(Handle, sampler);
    }

    /// <summary>
    /// Releases a GPU transfer buffer resource.
    /// </summary>
    public void ReleaseTransferBuffer(IntPtr tb)
    {
        SDL.SDL_ReleaseGPUTransferBuffer(Handle, tb);
    }

    /// <summary>
    /// Releases a GPU graphics pipeline resource.
    /// </summary>
    public void ReleasePipeline(IntPtr pipeline)
    {
        SDL.SDL_ReleaseGPUGraphicsPipeline(Handle, pipeline);
    }

    /// <summary>
    /// Gets or creates a cached GPU sampler for the specified sampler state.
    /// </summary>
    /// <param name="state">The sampler state configuration.</param>
    /// <returns>The GPU sampler handle.</returns>
    public IntPtr GetOrCreateSampler(SamplerState state)
    {
        if (samplerCache.TryGetValue(state, out var existing))
        {
            return existing;
        }

        var ci = new SDL.SDL_GPUSamplerCreateInfo
        {
            min_filter = state.MinFilter,
            mag_filter = state.MagFilter,
            address_mode_u = state.AddressU,
            address_mode_v = state.AddressV
        };
        var sampler = CreateSampler(ref ci);
        samplerCache[state] = sampler;
        return sampler;
    }

#endregion

#region Private Methods

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_BeginGPURenderPass")]
    private static extern IntPtr BeginGPURenderPassNoDepth(IntPtr commandBuffer,
        SDL.SDL_GPUColorTargetInfo[] colorTargets, uint numColorTargets, IntPtr depthStencilTargetInfo);

    private static IntPtr CheckHandle(IntPtr handle, string operation)
    {
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"{operation} failed: {SDL.SDL_GetError()}");
        }

        return handle;
    }

#endregion

#region Public Methods

    /// <summary>
    /// Releases samplers, detaches the window, and destroys the GPU device.
    /// Must only be called after all device-owned GPU resources have been released.
    /// </summary>
    public void Destroy()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (Handle == IntPtr.Zero)
        {
            return;
        }

        foreach (var sampler in samplerCache.Values)
        {
            SDL.SDL_ReleaseGPUSampler(Handle, sampler);
        }

        samplerCache.Clear();

        if (windowHandle != IntPtr.Zero)
        {
            SDL.SDL_ReleaseWindowFromGPUDevice(Handle, windowHandle);
            windowHandle = IntPtr.Zero;
        }

        SDL.SDL_DestroyGPUDevice(Handle);
    }

#endregion

}
