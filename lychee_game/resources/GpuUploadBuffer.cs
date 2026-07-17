using SDL = SDL3.SDL;

namespace lychee_game.resources;

/// <summary>
/// Growable, recycled GPU transfer buffer for staging uploads within a frame.
/// Map with cycling so the same buffer can be reused across copy passes in one command buffer.
/// </summary>
public sealed class GpuUploadBuffer
{
#region Private Fields

    private IntPtr transfer;

    private uint capacityBytes;

#endregion

#region Public Properties

    /// <summary>
    /// Native transfer buffer handle, or zero if not yet allocated.
    /// </summary>
    public IntPtr Handle => transfer;

    /// <summary>
    /// Current capacity in bytes.
    /// </summary>
    public uint CapacityBytes => capacityBytes;

#endregion

#region Public Methods

    /// <summary>
    /// Ensures the transfer buffer can hold at least <paramref name="requiredBytes"/>.
    /// Grows by doubling when reallocation is needed.
    /// </summary>
    public void EnsureCapacity(GpuDevice device, uint requiredBytes)
    {
        if (requiredBytes == 0 || (transfer != IntPtr.Zero && capacityBytes >= requiredBytes))
        {
            return;
        }

        if (transfer != IntPtr.Zero)
        {
            device.ReleaseTransferBuffer(transfer);
            transfer = IntPtr.Zero;
            capacityBytes = 0;
        }

        var cap = Math.Max(requiredBytes, 64u * 1024u);
        while (cap < requiredBytes)
        {
            if (cap > uint.MaxValue / 2)
            {
                cap = requiredBytes;
                break;
            }

            cap *= 2;
        }

        var ci = new SDL.SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = cap
        };
        transfer = device.CreateTransferBuffer(ref ci);
        capacityBytes = cap;
    }

    /// <summary>
    /// Maps the transfer buffer for CPU writes. Uses cycling so prior copy commands remain valid.
    /// </summary>
    /// <returns>Pointer to the mapped staging memory.</returns>
    public IntPtr Map(GpuDevice device)
    {
        if (transfer == IntPtr.Zero)
        {
            throw new InvalidOperationException("GpuUploadBuffer has no transfer buffer; call EnsureCapacity first.");
        }

        return device.MapTransfer(transfer, cycle: true);
    }

    /// <summary>
    /// Unmaps the transfer buffer after CPU writes.
    /// </summary>
    public void Unmap(GpuDevice device)
    {
        if (transfer == IntPtr.Zero)
        {
            return;
        }

        device.UnmapTransfer(transfer);
    }

    /// <summary>
    /// Releases the transfer buffer. Call before destroying the GPU device.
    /// </summary>
    public void Destroy(GpuDevice device)
    {
        if (transfer == IntPtr.Zero)
        {
            return;
        }

        device.ReleaseTransferBuffer(transfer);
        transfer = IntPtr.Zero;
        capacityBytes = 0;
    }

#endregion
}
