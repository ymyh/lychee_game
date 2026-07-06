using System.Diagnostics.CodeAnalysis;

namespace lychee_game.resources._2d;

/// <summary>
/// Abstract base class for generation-tracked resource pools.
/// Provides slot allocation, reuse, and generation validation.
/// </summary>
/// <typeparam name="TSlot">The resource data type stored in each slot.</typeparam>
/// <typeparam name="TRef">The reference type used to access slots.</typeparam>
public abstract class ResourcePool<TSlot, TRef>
    where TSlot : class
    where TRef : struct, IResourceRef
{
#region Private Fields

    private readonly List<TSlot?> slots = [];

    private readonly List<uint> generations = [];

    private readonly Queue<int> freeIndices = [];

#endregion

#region Protected Methods

    /// <summary>
    /// Creates a new reference with the specified index and generation.
    /// </summary>
    protected abstract TRef MakeRef(int index, uint generation);

    /// <summary>
    /// Allocates a slot for the given data, reusing freed slots when available.
    /// </summary>
    /// <param name="data">The resource data to store.</param>
    /// <returns>A reference to the allocated slot.</returns>
    protected TRef Allocate(TSlot data)
    {
        int index;
        uint generation;

        if (freeIndices.Count > 0)
        {
            index = freeIndices.Dequeue();
            generation = generations[index] + 1;
            generations[index] = generation;
            slots[index] = data;
        }
        else
        {
            index = slots.Count;
            generation = 0;
            slots.Add(data);
            generations.Add(0);
        }

        return MakeRef(index, generation);
    }

    /// <summary>
    /// Attempts to get the resource data for the given reference.
    /// </summary>
    /// <param name="ref">The resource reference to look up.</param>
    /// <param name="slot">When this method returns true, contains the resource data.</param>
    /// <returns>True if the reference is valid and the resource exists; otherwise, false.</returns>
    protected bool TryGet(TRef @ref, [NotNullWhen(true)] out TSlot? slot)
    {
        slot = null;

        if (@ref.Index < 0 || @ref.Index >= slots.Count)
        {
            return false;
        }

        if (generations[@ref.Index] != @ref.Generation)
        {
            return false;
        }

        slot = slots[@ref.Index];
        return slot != null;
    }

    /// <summary>
    /// Releases the slot associated with the given reference, invalidating old references.
    /// </summary>
    /// <param name="ref">The resource reference to release.</param>
    protected void Release(TRef @ref)
    {
        if (@ref.Index < 0 || @ref.Index >= slots.Count)
        {
            return;
        }

        if (generations[@ref.Index] != @ref.Generation)
        {
            return;
        }

        slots[@ref.Index] = null;
        generations[@ref.Index]++;
        freeIndices.Enqueue(@ref.Index);
    }

    /// <summary>
    /// Enumerates all live (non-null) slots in the pool.
    /// </summary>
    public IEnumerable<TSlot> All
    {
        get
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    yield return slot;
                }
            }
        }
    }

#endregion
}
