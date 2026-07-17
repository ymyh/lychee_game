using System.Runtime.CompilerServices;
using GpuDevice = lychee_game.resources.GpuDevice;
using GpuUploadBuffer = lychee_game.resources.GpuUploadBuffer;
using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// CPU staging + GPU vertex buffer for sprite instance data.
/// Filled after SpriteRecordSystem, uploaded before the render pass begins.
/// </summary>
public sealed class SpriteInstanceBuffer
{
#region Private Fields

    private SpriteInstance[] staging = new SpriteInstance[64];

    private int count;

    private IntPtr gpuBuffer;

    private uint capacityBytes;

#endregion

#region Public Properties

    /// <summary>
    /// Number of instances staged for the current frame.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// GPU instance buffer handle (valid after Upload).
    /// </summary>
    public IntPtr GpuBuffer => gpuBuffer;

#endregion

#region Public Methods

    /// <summary>
    /// Clears staged instances for a new frame.
    /// </summary>
    public void Clear()
    {
        count = 0;
    }

    /// <summary>
    /// Appends an instance to the CPU staging buffer.
    /// </summary>
    public void Add(in SpriteInstance instance)
    {
        if (count >= staging.Length)
        {
            Array.Resize(ref staging, staging.Length * 2);
        }

        staging[count++] = instance;
    }

    /// <summary>
    /// Uploads staged instances to the GPU via a recycled transfer buffer and a single copy pass.
    /// Must be called outside of a render pass.
    /// </summary>
    public void Upload(GpuDevice device, IntPtr commandBuffer, GpuUploadBuffer upload)
    {
        if (count == 0)
        {
            return;
        }

        var bytes = (uint)(count * Unsafe.SizeOf<SpriteInstance>());
        EnsureGpuCapacity(device, bytes);
        upload.EnsureCapacity(device, bytes);

        var ptr = upload.Map(device);
        unsafe
        {
            fixed (SpriteInstance* src = staging)
            {
                Buffer.MemoryCopy(src, (void*)ptr, bytes, bytes);
            }
        }

        upload.Unmap(device);

        var copyPass = SDL.SDL_BeginGPUCopyPass(commandBuffer);
        var srcLoc = new SDL.SDL_GPUTransferBufferLocation
        {
            transfer_buffer = upload.Handle,
            offset = 0
        };
        var dstRegion = new SDL.SDL_GPUBufferRegion
        {
            buffer = gpuBuffer,
            offset = 0,
            size = bytes
        };
        SDL.SDL_UploadToGPUBuffer(copyPass, ref srcLoc, ref dstRegion, true);
        SDL.SDL_EndGPUCopyPass(copyPass);
    }

    /// <summary>
    /// Releases the GPU instance buffer.
    /// </summary>
    public void Destroy(GpuDevice device)
    {
        if (gpuBuffer != IntPtr.Zero)
        {
            device.ReleaseBuffer(gpuBuffer);
            gpuBuffer = IntPtr.Zero;
            capacityBytes = 0;
        }
    }

#endregion

#region Private Methods

    private void EnsureGpuCapacity(GpuDevice device, uint requiredBytes)
    {
        if (capacityBytes >= requiredBytes && gpuBuffer != IntPtr.Zero)
        {
            return;
        }

        if (gpuBuffer != IntPtr.Zero)
        {
            device.ReleaseBuffer(gpuBuffer);
            gpuBuffer = IntPtr.Zero;
        }

        var stride = (uint)Unsafe.SizeOf<SpriteInstance>();
        var cap = Math.Max(requiredBytes, 64u * stride);
        while (cap < requiredBytes)
        {
            if (cap > uint.MaxValue / 2)
            {
                cap = requiredBytes;
                break;
            }

            cap *= 2;
        }

        var ci = new SDL.SDL_GPUBufferCreateInfo
        {
            usage = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = cap
        };
        gpuBuffer = device.CreateBuffer(ref ci);
        capacityBytes = cap;
    }

#endregion
}
