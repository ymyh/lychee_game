using lychee_game.resources._2d;
using SDL = SDL3.SDL;

namespace lychee_game.resources;

/// <summary>
/// Wraps an SDL3 GPU device handle, providing resource creation and lifecycle management.
/// </summary>
public sealed class GpuDevice : IDisposable
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

#endregion

#region Private Fields

    private bool disposed;

    private readonly Dictionary<SamplerState, IntPtr> samplerCache = [];

#endregion

#region Constructor

    /// <summary>
    /// Creates a GPU device and claims the specified window for rendering.
    /// Tries Vulkan (SPIRV) first, falls back to D3D12 (DXIL) if unsupported.
    /// </summary>
    /// <param name="window">The window to claim for GPU rendering.</param>
    /// <param name="debugMode">Whether to enable GPU debug mode.</param>
    public GpuDevice(Window window, bool debugMode = false)
    {
        // Try Vulkan (SPIRV) first, then D3D12 (DXIL)
        var candidates = new[]
        {
            (SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV, "Vulkan"),
            (SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL, "D3D12")
        };

        foreach (var (format, name) in candidates)
        {
            if (!SDL.SDL_GPUSupportsShaderFormats(format, "lychee_game"))
            {
                continue;
            }

            Handle = SDL.SDL_CreateGPUDevice(format, debugMode, "lychee_game");
            if (Handle != IntPtr.Zero)
            {
                ShaderFormat = format;
                Console.WriteLine($"[GpuDevice] Using {name} backend");
                break;
            }
        }

        if (Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "GPU does not support SPIRV (Vulkan) or DXIL (D3D12) shader formats");
        }

        if (!SDL.SDL_ClaimWindowForGPUDevice(Handle, window.Handle))
        {
            SDL.SDL_DestroyGPUDevice(Handle);
            throw new InvalidOperationException($"Failed to claim window for GPU: {SDL.SDL_GetError()}");
        }

        SDL.SDL_SetGPUSwapchainParameters(Handle, window.Handle,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
            SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC);
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
    public IntPtr CreateShader(ref SDL.SDL_GPUShaderCreateInfo ci)
    {
        return SDL.SDL_CreateGPUShader(Handle, ref ci);
    }

    /// <summary>
    /// Creates a GPU graphics pipeline from the specified create info.
    /// </summary>
    public IntPtr CreatePipeline(ref SDL.SDL_GPUGraphicsPipelineCreateInfo ci)
    {
        return SDL.SDL_CreateGPUGraphicsPipeline(Handle, ref ci);
    }

    /// <summary>
    /// Creates a GPU sampler from the specified create info.
    /// </summary>
    public IntPtr CreateSampler(ref SDL.SDL_GPUSamplerCreateInfo ci)
    {
        return SDL.SDL_CreateGPUSampler(Handle, ref ci);
    }

    /// <summary>
    /// Creates a GPU texture from the specified create info.
    /// </summary>
    public IntPtr CreateTexture(ref SDL.SDL_GPUTextureCreateInfo ci)
    {
        return SDL.SDL_CreateGPUTexture(Handle, ref ci);
    }

    /// <summary>
    /// Creates a GPU buffer from the specified create info.
    /// </summary>
    public IntPtr CreateBuffer(ref SDL.SDL_GPUBufferCreateInfo ci)
    {
        return SDL.SDL_CreateGPUBuffer(Handle, ref ci);
    }

    /// <summary>
    /// Creates a GPU transfer buffer from the specified create info.
    /// </summary>
    public IntPtr CreateTransferBuffer(ref SDL.SDL_GPUTransferBufferCreateInfo ci)
    {
        return SDL.SDL_CreateGPUTransferBuffer(Handle, ref ci);
    }

    /// <summary>
    /// Maps a transfer buffer for CPU access.
    /// </summary>
    /// <param name="tb">The transfer buffer to map.</param>
    /// <param name="cycle">Whether to cycle the buffer.</param>
    /// <returns>A pointer to the mapped memory.</returns>
    public IntPtr MapTransfer(IntPtr tb, bool cycle)
    {
        return SDL.SDL_MapGPUTransferBuffer(Handle, tb, cycle);
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

#region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (Handle != IntPtr.Zero)
        {
            foreach (var sampler in samplerCache.Values)
            {
                SDL.SDL_ReleaseGPUSampler(Handle, sampler);
            }
            samplerCache.Clear();

            SDL.SDL_DestroyGPUDevice(Handle);
        }
    }

#endregion

}
