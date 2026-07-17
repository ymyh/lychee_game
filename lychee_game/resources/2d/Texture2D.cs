using GpuDevice = lychee_game.resources.GpuDevice;
using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Descriptor for creating a Texture2D resource.
/// </summary>
public sealed class Texture2DDesc
{
#region Public Properties

    /// <summary>
    /// Raw pixel data (RGBA8 format by default).
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    /// Texture width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Texture height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// GPU texture format. Default is R8G8B8A8_UNORM.
    /// </summary>
    public SDL.SDL_GPUTextureFormat Format { get; init; } = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;

#endregion
}

/// <summary>
/// CPU-side texture data.
/// </summary>
public sealed class Texture2D
{
#region Public Properties

    /// <summary>
    /// Raw pixel data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Texture width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Texture height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// GPU texture format.
    /// </summary>
    public SDL.SDL_GPUTextureFormat Format { get; }

    /// <summary>
    /// GPU texture handle (populated after upload).
    /// </summary>
    public IntPtr GpuTexture { get; set; }

    /// <summary>
    /// Whether this texture has been uploaded to the GPU.
    /// </summary>
    public bool Uploaded { get; set; }

#endregion

#region Constructor

    /// <summary>
    /// Creates a Texture2D from the specified descriptor.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="Texture2DDesc.Data"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is not positive.</exception>
    /// <exception cref="ArgumentException">Thrown when data length does not match dimensions/format.</exception>
    internal Texture2D(Texture2DDesc desc)
    {
        ArgumentNullException.ThrowIfNull(desc.Data);

        if (desc.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), desc.Width,
                "Texture width must be positive.");
        }

        if (desc.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), desc.Height,
                "Texture height must be positive.");
        }

        var bytesPerPixel = BytesPerPixel(desc.Format);
        var expectedLength = (long)desc.Width * desc.Height * bytesPerPixel;
        if (desc.Data.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Texture data length mismatch: Width={desc.Width}, Height={desc.Height}, " +
                $"Format={desc.Format} ({bytesPerPixel} bytes/pixel) expects {expectedLength} bytes, " +
                $"but Data.Length={desc.Data.Length}.",
                nameof(desc));
        }

        Data = desc.Data;
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;
    }

#endregion

#region Private Static Methods

    private static int BytesPerPixel(SDL.SDL_GPUTextureFormat format)
    {
        return format switch
        {
            SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM => 4,
            SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM => 4,
            SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM => 1,
            SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_A8_UNORM => 1,
            _ => throw new ArgumentException(
                $"Unsupported texture format for CPU upload validation: {format}.",
                nameof(format))
        };
    }

#endregion
}

/// <summary>
/// Asset pool for Texture2D resources.
/// </summary>
public sealed class Texture2DList : AssetPool<Texture2D, components._2d.Texture2DRef>
{
#region Private Fields

    private readonly GpuDevice? device;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Texture2DList without a GPU device (CPU-only tests).
    /// Releasing an uploaded texture requires the device overload constructor.
    /// </summary>
    public Texture2DList()
    {
    }

    /// <summary>
    /// Creates a Texture2DList that releases GPU textures when slots are recycled.
    /// </summary>
    public Texture2DList(GpuDevice device)
    {
        this.device = device;
    }

#endregion

#region Protected Methods

    /// <inheritdoc/>
    protected override components._2d.Texture2DRef MakeRef(int index, uint generation)
    {
        return new components._2d.Texture2DRef { Index = index, Generation = generation };
    }

    /// <inheritdoc/>
    protected override void OnRelease(Texture2D slot)
    {
        ReleaseTextureGpu(slot);
    }

#endregion

#region Public Methods

    /// <summary>
    /// Creates a new Texture2D resource from the specified descriptor.
    /// </summary>
    /// <param name="desc">The texture descriptor containing pixel data and format.</param>
    /// <returns>A reference to the created texture.</returns>
    public components._2d.Texture2DRef Create(in Texture2DDesc desc)
    {
        return Allocate(new Texture2D(desc));
    }

    /// <summary>
    /// Tries to get the texture data for the specified reference.
    /// </summary>
    /// <param name="ref">The texture reference.</param>
    /// <param name="texture">When this method returns true, contains the texture data.</param>
    /// <returns>True if the reference is valid; otherwise, false.</returns>
    public new bool TryGet(components._2d.Texture2DRef @ref, out Texture2D? texture)
    {
        return base.TryGet(@ref, out texture);
    }

    /// <summary>
    /// Releases the texture and any uploaded GPU texture associated with the reference.
    /// </summary>
    /// <param name="ref">The texture reference to release.</param>
    public new void Release(components._2d.Texture2DRef @ref)
    {
        base.Release(@ref);
    }

    /// <summary>
    /// Releases all uploaded GPU textures in this pool.
    /// Call before destroying the GPU device.
    /// </summary>
    public void ReleaseGpuResources()
    {
        foreach (var texture in All)
        {
            ReleaseTextureGpu(texture);
        }
    }

#endregion

#region Private Methods

    private void ReleaseTextureGpu(Texture2D texture)
    {
        if (texture.GpuTexture == IntPtr.Zero)
        {
            texture.Uploaded = false;
            return;
        }

        if (device == null)
        {
            throw new InvalidOperationException(
                "Texture2DList has no GpuDevice; cannot release uploaded GPU textures. " +
                "Construct Texture2DList with a GpuDevice.");
        }

        device.ReleaseTexture(texture.GpuTexture);
        texture.GpuTexture = IntPtr.Zero;
        texture.Uploaded = false;
    }

#endregion
}
