using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Describes sampler state for texture filtering and addressing.
/// Used as part of pipeline cache keys.
/// </summary>
public sealed class SamplerState
{
#region Public Properties

    /// <summary>
    /// Minification filter. Default is LINEAR.
    /// </summary>
    public SDL.SDL_GPUFilter MinFilter { get; init; } = SDL.SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;

    /// <summary>
    /// Magnification filter. Default is LINEAR.
    /// </summary>
    public SDL.SDL_GPUFilter MagFilter { get; init; } = SDL.SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;

    /// <summary>
    /// Address mode for U coordinate. Default is CLAMP_TO_EDGE.
    /// </summary>
    public SDL.SDL_GPUSamplerAddressMode AddressU { get; init; } = SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;

    /// <summary>
    /// Address mode for V coordinate. Default is CLAMP_TO_EDGE.
    /// </summary>
    public SDL.SDL_GPUSamplerAddressMode AddressV { get; init; } = SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;

#endregion

#region Equality

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not SamplerState other)
        {
            return false;
        }

        return MinFilter == other.MinFilter &&
               MagFilter == other.MagFilter &&
               AddressU == other.AddressU &&
               AddressV == other.AddressV;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(MinFilter, MagFilter, AddressU, AddressV);
    }

#endregion
}
