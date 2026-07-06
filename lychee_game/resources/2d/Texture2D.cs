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
    internal Texture2D(Texture2DDesc desc)
    {
        Data = desc.Data;
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;
    }

#endregion
}

/// <summary>
/// Resource pool for Texture2D assets.
/// </summary>
public sealed class Texture2DList : ResourcePool<Texture2D, components._2d.Texture2DRef>
{
#region Protected Methods

    /// <inheritdoc/>
    protected override components._2d.Texture2DRef MakeRef(int index, uint generation)
    {
        return new components._2d.Texture2DRef { Index = index, Generation = generation };
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
    /// Releases the texture associated with the specified reference.
    /// </summary>
    /// <param name="ref">The texture reference to release.</param>
    public new void Release(components._2d.Texture2DRef @ref)
    {
        base.Release(@ref);
    }

#endregion
}
