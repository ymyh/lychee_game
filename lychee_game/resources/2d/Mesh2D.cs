using System.Runtime.InteropServices;
using GpuDevice = lychee_game.resources.GpuDevice;

namespace lychee_game.resources._2d;

/// <summary>
/// Descriptor for creating a Mesh2D resource.
/// </summary>
public sealed class Mesh2DDesc
{
#region Public Properties

    /// <summary>
    /// Vertex data array.
    /// </summary>
    public Vertex2D[] Vertices { get; init; } = [];

    /// <summary>
    /// Index data array. Null or empty means non-indexed drawing.
    /// </summary>
    public int[] Indices { get; init; } = [];

#endregion
}

/// <summary>
/// CPU-side mesh data with serialized vertex/index buffers.
/// </summary>
public sealed class Mesh2D
{
#region Public Properties

    /// <summary>
    /// Serialized vertex data as bytes.
    /// </summary>
    public byte[] VertexData { get; }

    /// <summary>
    /// Serialized index data as bytes. Null if non-indexed.
    /// </summary>
    public byte[]? IndexData { get; }

    /// <summary>
    /// Whether this mesh uses indexed drawing.
    /// </summary>
    public bool Indexed { get; }

    /// <summary>
    /// Number of vertices.
    /// </summary>
    public int VertexCount { get; }

    /// <summary>
    /// Number of indices (0 if non-indexed).
    /// </summary>
    public int IndexCount { get; }

    /// <summary>
    /// GPU vertex buffer handle (populated after upload).
    /// </summary>
    public IntPtr GpuVertexBuffer { get; set; }

    /// <summary>
    /// GPU index buffer handle (populated after upload).
    /// </summary>
    public IntPtr GpuIndexBuffer { get; set; }

    /// <summary>
    /// Whether this mesh has been uploaded to the GPU.
    /// </summary>
    public bool Uploaded { get; set; }

#endregion

#region Constructor

    /// <summary>
    /// Creates a Mesh2D from the specified descriptor.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when vertices or indices are null.</exception>
    /// <exception cref="ArgumentException">Thrown when vertices are empty or an index is out of range.</exception>
    internal Mesh2D(Mesh2DDesc desc)
    {
        ArgumentNullException.ThrowIfNull(desc.Vertices);
        ArgumentNullException.ThrowIfNull(desc.Indices);

        if (desc.Vertices.Length == 0)
        {
            throw new ArgumentException("Mesh must contain at least one vertex.", nameof(desc));
        }

        VertexCount = desc.Vertices.Length;
        VertexData = MemoryMarshal.AsBytes(desc.Vertices.AsSpan()).ToArray();

        if (desc.Indices.Length > 0)
        {
            for (var i = 0; i < desc.Indices.Length; i++)
            {
                var index = desc.Indices[i];
                if (index < 0 || index >= VertexCount)
                {
                    throw new ArgumentException(
                        $"Mesh index out of range at Indices[{i}]={index}; " +
                        $"valid range is [0, {VertexCount}).",
                        nameof(desc));
                }
            }

            IndexCount = desc.Indices.Length;
            IndexData = MemoryMarshal.AsBytes(desc.Indices.AsSpan()).ToArray();
            Indexed = true;
        }
    }

#endregion
}

/// <summary>
/// Asset pool for Mesh2D resources.
/// </summary>
public sealed class Mesh2DList : AssetPool<Mesh2D, components._2d.Mesh2DRef>
{
#region Private Fields

    private readonly GpuDevice? device;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Mesh2DList without a GPU device (CPU-only tests).
    /// Releasing an uploaded mesh requires the device overload constructor.
    /// </summary>
    public Mesh2DList()
    {
    }

    /// <summary>
    /// Creates a Mesh2DList that releases GPU buffers when slots are recycled.
    /// </summary>
    public Mesh2DList(GpuDevice device)
    {
        this.device = device;
    }

#endregion

#region Protected Methods

    /// <inheritdoc/>
    protected override components._2d.Mesh2DRef MakeRef(int index, uint generation)
    {
        return new components._2d.Mesh2DRef { Index = index, Generation = generation };
    }

    /// <inheritdoc/>
    protected override void OnRelease(Mesh2D slot)
    {
        ReleaseMeshGpu(slot);
    }

#endregion

#region Public Methods

    /// <summary>
    /// Creates a new Mesh2D resource from the specified descriptor.
    /// </summary>
    /// <param name="desc">The mesh descriptor containing vertex and index data.</param>
    /// <returns>A reference to the created mesh.</returns>
    public components._2d.Mesh2DRef Create(in Mesh2DDesc desc)
    {
        return Allocate(new Mesh2D(desc));
    }

    /// <summary>
    /// Tries to get the mesh data for the specified reference.
    /// </summary>
    /// <param name="ref">The mesh reference.</param>
    /// <param name="mesh">When this method returns true, contains the mesh data.</param>
    /// <returns>True if the reference is valid; otherwise, false.</returns>
    public new bool TryGet(components._2d.Mesh2DRef @ref, out Mesh2D? mesh)
    {
        return base.TryGet(@ref, out mesh);
    }

    /// <summary>
    /// Releases the mesh and any uploaded GPU buffers associated with the reference.
    /// </summary>
    /// <param name="ref">The mesh reference to release.</param>
    public new void Release(components._2d.Mesh2DRef @ref)
    {
        base.Release(@ref);
    }

    /// <summary>
    /// Releases all uploaded GPU buffers for meshes in this pool.
    /// Call before destroying the GPU device.
    /// </summary>
    public void ReleaseGpuResources()
    {
        foreach (var mesh in All)
        {
            ReleaseMeshGpu(mesh);
        }
    }

#endregion

#region Private Methods

    private void ReleaseMeshGpu(Mesh2D mesh)
    {
        if (mesh.GpuVertexBuffer == IntPtr.Zero && mesh.GpuIndexBuffer == IntPtr.Zero)
        {
            mesh.Uploaded = false;
            return;
        }

        if (device == null)
        {
            throw new InvalidOperationException(
                "Mesh2DList has no GpuDevice; cannot release uploaded GPU buffers. " +
                "Construct Mesh2DList with a GpuDevice.");
        }

        if (mesh.GpuVertexBuffer != IntPtr.Zero)
        {
            device.ReleaseBuffer(mesh.GpuVertexBuffer);
            mesh.GpuVertexBuffer = IntPtr.Zero;
        }

        if (mesh.GpuIndexBuffer != IntPtr.Zero)
        {
            device.ReleaseBuffer(mesh.GpuIndexBuffer);
            mesh.GpuIndexBuffer = IntPtr.Zero;
        }

        mesh.Uploaded = false;
    }

#endregion
}
