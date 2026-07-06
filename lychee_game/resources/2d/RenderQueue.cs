using System.Numerics;
using lychee_game.components._2d;

namespace lychee_game.resources._2d;

/// <summary>
/// Represents a single draw call to be submitted to the GPU.
/// Populated by SpriteRecordSystem from Sprite2D + Material.
/// </summary>
public struct DrawRecord
{
#region Public Fields

    /// <summary>
    /// Mesh to render.
    /// </summary>
    public Mesh2DRef Mesh;

    /// <summary>
    /// Texture to sample.
    /// </summary>
    public Texture2DRef Texture;

    /// <summary>
    /// Effect (shader) to use, unpacked from Material.
    /// </summary>
    public EffectRef Effect;

    /// <summary>
    /// Sampler state for texture filtering, unpacked from Material.
    /// </summary>
    public SamplerState Sampler;

    /// <summary>
    /// World transform matrix.
    /// </summary>
    public Matrix4x4 WorldMatrix;

    /// <summary>
    /// UV rectangle for sprite sheet cropping.
    /// </summary>
    public UVRect UV;

    /// <summary>
    /// Draw order (higher renders on top).
    /// </summary>
    public int ZIndex;

    /// <summary>
    /// Color tint applied to the sprite.
    /// </summary>
    public RgbaByte Tint;

#endregion
}

/// <summary>
/// Collects and sorts draw records for batched rendering.
/// </summary>
public sealed class RenderQueue
{
#region Private Fields

    private readonly List<DrawRecord> records = [];

#endregion

#region Public Properties

    /// <summary>
    /// Read-only access to the current draw records.
    /// </summary>
    public IReadOnlyList<DrawRecord> Records => records;

#endregion

#region Public Methods

    /// <summary>
    /// Adds a draw record to the queue.
    /// </summary>
    /// <param name="r">The draw record to enqueue.</param>
    public void Enqueue(in DrawRecord r)
    {
        records.Add(r);
    }

    /// <summary>
    /// Sorts records by effect, texture, then Z-index for batched rendering.
    /// </summary>
    public void Sort()
    {
        records.Sort((a, b) =>
        {
            var cmp = a.Effect.Index.CompareTo(b.Effect.Index);
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = a.Texture.Index.CompareTo(b.Texture.Index);
            if (cmp != 0)
            {
                return cmp;
            }

            return a.ZIndex.CompareTo(b.ZIndex);
        });
    }

    /// <summary>
    /// Clears all draw records from the queue.
    /// </summary>
    public void Clear()
    {
        records.Clear();
    }

#endregion
}
