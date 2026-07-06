namespace lychee_game.resources._2d;

/// <summary>
/// Interface for resource references that track index and generation for safe pool access.
/// </summary>
public interface IResourceRef
{
#region Properties

    /// <summary>
    /// Index into the resource pool.
    /// </summary>
    int Index { get; set; }

    /// <summary>
    /// Generation counter for detecting stale references.
    /// </summary>
    uint Generation { get; set; }

#endregion
}
