using System.Runtime.InteropServices;
using SDL = SDL3.SDL;

namespace lychee_game.resources._2d;

/// <summary>
/// Supported uniform data types for shader parameters.
/// </summary>
public enum UniformType
{
    /// <summary>Single float.</summary>
    Float,

    /// <summary>2-component vector.</summary>
    Vec2,

    /// <summary>3-component vector (aligned to 16 bytes in std140).</summary>
    Vec3,

    /// <summary>4-component vector.</summary>
    Vec4,

    /// <summary>4x4 matrix.</summary>
    Mat4
}

/// <summary>
/// Describes a single uniform parameter in a shader.
/// </summary>
public readonly struct UniformDesc
{
#region Public Properties

    /// <summary>
    /// Name of the uniform parameter.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Data type of the uniform.
    /// </summary>
    public UniformType Type { get; init; }

    /// <summary>
    /// Byte offset within the uniform buffer.
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    /// Size in bytes based on the uniform type.
    /// </summary>
    public int SizeBytes => Type switch
    {
        UniformType.Mat4 => 64,
        UniformType.Vec4 => 16,
        UniformType.Vec3 => 16, // std140 alignment
        UniformType.Vec2 => 8,
        UniformType.Float => 4,
        _ => 0
    };

#endregion

#region Constructor

    /// <summary>
    /// Creates a UniformDesc with the specified parameters.
    /// </summary>
    public UniformDesc(string name, UniformType type, uint offset)
    {
        Name = name;
        Type = type;
        Offset = offset;
    }

#endregion
}

/// <summary>
/// Descriptor for creating an Effect resource.
/// </summary>
public sealed class EffectDesc
{
#region Public Properties

    /// <summary>
    /// Vertex shader SPIR-V bytecode.
    /// </summary>
    public byte[] VertexSpv { get; init; } = [];

    /// <summary>
    /// Fragment shader SPIR-V bytecode.
    /// </summary>
    public byte[] FragmentSpv { get; init; } = [];

    /// <summary>
    /// Uniform parameter descriptors.
    /// </summary>
    public UniformDesc[] Uniforms { get; init; } = [];

    /// <summary>
    /// Total size of the uniform buffer in bytes.
    /// </summary>
    public uint UniformBufferSize { get; init; }

    /// <summary>
    /// Number of vertex shader samplers. Default is 0.
    /// </summary>
    public uint VertexSamplerCount { get; init; } = 0;

    /// <summary>
    /// Number of fragment shader samplers. Default is 1.
    /// </summary>
    public uint FragmentSamplerCount { get; init; } = 1;

#endregion
}

/// <summary>
/// Compiled shader effect with GPU handles and uniform metadata.
/// </summary>
public sealed class Effect
{
#region Public Properties

    /// <summary>
    /// GPU vertex shader handle.
    /// </summary>
    public IntPtr VertexShader { get; }

    /// <summary>
    /// GPU fragment shader handle.
    /// </summary>
    public IntPtr FragmentShader { get; }

    /// <summary>
    /// Uniform parameter descriptors.
    /// </summary>
    public UniformDesc[] Uniforms { get; }

    /// <summary>
    /// Total uniform buffer size in bytes.
    /// </summary>
    public uint UniformBufferSize { get; }

    /// <summary>
    /// Number of fragment shader samplers.
    /// </summary>
    public uint FragmentSamplerCount { get; }

#endregion

#region Constructor

    /// <summary>
    /// Creates an Effect by compiling shaders on the GPU device.
    /// </summary>
    internal Effect(GpuDevice device, EffectDesc desc)
    {
        Uniforms = desc.Uniforms;
        UniformBufferSize = desc.UniformBufferSize;
        FragmentSamplerCount = desc.FragmentSamplerCount;

        unsafe
        {
            fixed (byte* vertexCode = desc.VertexSpv)
            fixed (byte* fragmentCode = desc.FragmentSpv)
            {
                var entrypoint = Marshal.StringToHGlobalAnsi("main");
                try
                {
                    var vertexCi = new SDL.SDL_GPUShaderCreateInfo
                    {
                        code_size = (nuint)desc.VertexSpv.Length,
                        code = vertexCode,
                        entrypoint = (byte*)entrypoint,
                        format = device.ShaderFormat,
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
                        num_samplers = 0,
                        num_storage_textures = 0,
                        num_storage_buffers = 0,
                        num_uniform_buffers = 1
                    };
                    VertexShader = device.CreateShader(ref vertexCi);

                    var fragmentCi = new SDL.SDL_GPUShaderCreateInfo
                    {
                        code_size = (nuint)desc.FragmentSpv.Length,
                        code = fragmentCode,
                        entrypoint = (byte*)entrypoint,
                        format = device.ShaderFormat,
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                        num_samplers = desc.FragmentSamplerCount,
                        num_storage_textures = 0,
                        num_storage_buffers = 0,
                        num_uniform_buffers = 1
                    };
                    FragmentShader = device.CreateShader(ref fragmentCi);
                }
                finally
                {
                    Marshal.FreeHGlobal(entrypoint);
                }
            }
        }
    }

#endregion
}

/// <summary>
/// Resource pool for Effect assets.
/// </summary>
public sealed class EffectList : ResourcePool<Effect, components._2d.EffectRef>
{
#region Private Fields

    private readonly GpuDevice device;

#endregion

#region Constructor

    /// <summary>
    /// Creates an EffectList bound to the specified GPU device.
    /// </summary>
    public EffectList(GpuDevice device)
    {
        this.device = device;
    }

#endregion

#region Protected Methods

    /// <inheritdoc/>
    protected override components._2d.EffectRef MakeRef(int index, uint generation)
    {
        return new components._2d.EffectRef { Index = index, Generation = generation };
    }

#endregion

#region Public Methods

    /// <summary>
    /// Creates a new Effect resource from the specified descriptor.
    /// </summary>
    /// <param name="desc">The effect descriptor containing shader bytecode and uniforms.</param>
    /// <returns>A reference to the created effect.</returns>
    public components._2d.EffectRef Create(in EffectDesc desc)
    {
        return Allocate(new Effect(device, desc));
    }

    /// <summary>
    /// Tries to get the effect data for the specified reference.
    /// </summary>
    /// <param name="ref">The effect reference.</param>
    /// <param name="effect">When this method returns true, contains the effect data.</param>
    /// <returns>True if the reference is valid; otherwise, false.</returns>
    public new bool TryGet(components._2d.EffectRef @ref, out Effect? effect)
    {
        return base.TryGet(@ref, out effect);
    }

    /// <summary>
    /// Releases the effect associated with the specified reference.
    /// </summary>
    /// <param name="ref">The effect reference to release.</param>
    public new void Release(components._2d.EffectRef @ref)
    {
        base.Release(@ref);
    }

#endregion
}
