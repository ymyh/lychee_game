using System.Reflection;
using lychee_game.components._2d;
using lychee_game.resources;
using lychee_game.resources._2d;
using lychee_game.schedules;
using lychee_game.systems._2d;
using lychee;
using lychee.interfaces;
using SDL = SDL3.SDL;

namespace lychee_game;

/// <summary>
/// Configuration for the basic 2D rendering plugin.
/// </summary>
public sealed class BasicRenderPluginDescriptor
{
#region Public Properties

    /// <summary>
    /// Whether to enable GPU debug mode.
    /// </summary>
    public bool DebugMode { get; init; } = false;

    /// <summary>
    /// Present mode for the swapchain. Default is VSYNC.
    /// </summary>
    public SDL.SDL_GPUPresentMode PresentMode { get; init; } = SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;

#endregion
}

/// <summary>
/// Provides basic 2D rendering capabilities with SDL3 GPU.
/// Requires <see cref="LycheeGamePlugin"/>.
///
/// Default resources occupy slot 0 of each pool (invariant):
/// Mesh2DList[0] = UnitQuad, Texture2DList[0] = WhiteTexture,
/// EffectList[0] = DefaultEffect, MaterialList[0] = DefaultMaterial.
/// </summary>
public sealed class BasicRenderPlugin(BasicRenderPluginDescriptor desc) : IPlugin, IDisposable
{
#region Private Fields

    private GpuDevice? gpuDevice;

    private PipelineCache? pipelineCache;

#endregion

#region Public Fields

    /// <summary>
    /// System that acquires swapchain and begins render pass.
    /// </summary>
    public readonly BeginFrameSystem BeginFrameSystem = new();

    /// <summary>
    /// System that ends render pass and submits command buffer.
    /// </summary>
    public readonly EndFrameSystem EndFrameSystem = new();

    /// <summary>
    /// System that updates the camera view-projection matrix.
    /// </summary>
    public readonly CameraUpdateSystem CameraUpdateSystem = new();

    /// <summary>
    /// System that records sprite draw commands.
    /// </summary>
    public readonly SpriteRecordSystem SpriteRecordSystem = new();

    /// <summary>
    /// System that submits draw commands to the GPU.
    /// </summary>
    public readonly SpriteSubmitSystem SpriteSubmitSystem = new();

#endregion

#region Public Properties

    /// <summary>
    /// Default unit quad mesh reference (pool slot 0).
    /// </summary>
    public Mesh2DRef UnitQuad { get; private set; }

    /// <summary>
    /// Default 1x1 white texture reference (pool slot 0).
    /// </summary>
    public Texture2DRef WhiteTexture { get; private set; }

    /// <summary>
    /// Default shader effect reference (pool slot 0).
    /// </summary>
    public EffectRef DefaultEffect { get; private set; }

    /// <summary>
    /// Default material reference (pool slot 0). Contains DefaultEffect + default LINEAR sampler.
    /// </summary>
    public MaterialRef DefaultMaterial { get; private set; }

#endregion

#region Constructor

    /// <summary>
    /// Creates a BasicRenderPlugin with default settings.
    /// </summary>
    public BasicRenderPlugin() : this(new())
    {
    }

#endregion

#region IPlugin Implementation

    /// <inheritdoc/>
    public void Install(App app)
    {
        if (!app.HasResource<LycheeGamePlugin>())
        {
            throw new PluginRequirementException(nameof(BasicRenderPlugin));
        }

        var window = app.GetResource<Window>();
        gpuDevice = new GpuDevice(window, desc.DebugMode);
        pipelineCache = new PipelineCache(gpuDevice);

        // Register resources
        app.AddResource(gpuDevice);
        app.AddResource<RenderContext>();
        app.AddResource(pipelineCache);

        var meshList = new Mesh2DList();
        var textureList = new Texture2DList();
        var effectList = new EffectList(gpuDevice);
        var materialList = new MaterialList();

        app.AddResource(meshList);
        app.AddResource(textureList);
        app.AddResource(effectList);
        app.AddResource(materialList);
        app.AddResource<RenderQueue>();

        // Get render schedule and add systems
        var render = app.GetSchedule<DefaultSchedule>("Render")!;
        render.AddSystems<(BeginFrameSystem, CameraUpdateSystem,
            SpriteRecordSystem, SpriteSubmitSystem)>();

        // Add EndFrameSystem to RenderUI schedule (runs after Render)
        var renderUI = app.GetSchedule<DefaultSchedule>("RenderUI")!;
        renderUI.AddSystem(EndFrameSystem);

        // Create default resources in fixed order to occupy slot 0 of each pool (invariant)
        CreateDefaultResources(meshList, textureList, effectList, materialList);
    }

#endregion

#region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        pipelineCache?.DestroyAll();
        gpuDevice?.Dispose();
    }

#endregion

#region Private Methods

    private void CreateDefaultResources(Mesh2DList meshes, Texture2DList textures,
        EffectList effects, MaterialList materials)
    {
        // Order is fixed: Mesh→Texture→Effect→Material, each occupies slot 0

        UnitQuad = meshes.Create(new Mesh2DDesc
        {
            Vertices =
            [
                new Vertex2D(new(-0.5f, -0.5f), new(0.0f, 1.0f), RgbaByte.White),
                new Vertex2D(new(0.5f, -0.5f), new(1.0f, 1.0f), RgbaByte.White),
                new Vertex2D(new(0.5f, 0.5f), new(1.0f, 0.0f), RgbaByte.White),
                new Vertex2D(new(-0.5f, 0.5f), new(0.0f, 0.0f), RgbaByte.White)
            ],
            Indices = [0, 1, 2, 2, 3, 0]
        });

        WhiteTexture = textures.Create(new Texture2DDesc
        {
            Data = [255, 255, 255, 255],
            Width = 1,
            Height = 1,
            Format = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM
        });

        DefaultEffect = effects.Create(new EffectDesc
        {
            VertexSpv = DefaultShaders.DefaultVertexShader,
            FragmentSpv = DefaultShaders.DefaultFragmentShader,
            Uniforms =
            [
                new UniformDesc("MVP", UniformType.Mat4, 0),
                new UniformDesc("Tint", UniformType.Vec4, 64)
            ],
            UniformBufferSize = 80,
            FragmentSamplerCount = 1
        });

        DefaultMaterial = materials.Create(new Material
        {
            Effect = DefaultEffect,
            Sampler = new SamplerState()
        });
    }

#endregion
}

/// <summary>
/// Built-in SPIR-V shader bytecode for default rendering.
/// Loads from embedded resources compiled from GLSL sources.
/// </summary>
internal static class DefaultShaders
{
    internal static byte[] DefaultVertexShader => LoadEmbeddedShader("default_vert.spv");

    internal static byte[] DefaultFragmentShader => LoadEmbeddedShader("default_frag.spv");

    private static byte[] LoadEmbeddedShader(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullName = $"lychee_game.shaders.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded shader '{fullName}' not found. " +
                "Run shaders/compile.bat to compile GLSL to SPIR-V.");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
