using lychee_game.components._2d;

namespace lychee_game.resources._2d;

/// <summary>
/// Asset pool for Material resources (Effect + SamplerState combinations).
/// </summary>
public sealed class MaterialList : AssetPool<Material, MaterialRef>
{
#region Protected Methods

    /// <inheritdoc/>
    protected override MaterialRef MakeRef(int index, uint generation)
    {
        return new MaterialRef { Index = index, Generation = generation };
    }

#endregion

#region Public Methods

    /// <summary>
    /// Creates a new Material resource.
    /// </summary>
    /// <param name="material">The material to store in the pool.</param>
    /// <returns>A reference to the created material.</returns>
    public MaterialRef Create(Material material)
    {
        return Allocate(material);
    }

    /// <summary>
    /// Tries to get the material data for the specified reference.
    /// </summary>
    /// <param name="ref">The material reference.</param>
    /// <param name="material">When this method returns true, contains the material data.</param>
    /// <returns>True if the reference is valid; otherwise, false.</returns>
    public new bool TryGet(MaterialRef @ref, out Material? material)
    {
        return base.TryGet(@ref, out material);
    }

    /// <summary>
    /// Releases the material associated with the specified reference.
    /// </summary>
    /// <param name="ref">The material reference to release.</param>
    public new void Release(MaterialRef @ref)
    {
        base.Release(@ref);
    }

#endregion
}
