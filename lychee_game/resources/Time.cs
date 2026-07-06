using System.Diagnostics;

namespace lychee_game.resources;

/// <summary>
/// Game time tracking resource. Provides elapsed time, delta time, and time scale.
/// </summary>
public sealed class Time
{
#region Private Fields

    private readonly Stopwatch stopwatch = new();

    private readonly Stopwatch stopwatchUpdate = new();

#endregion

#region Public Properties

    /// <summary>Total elapsed time since game start.</summary>
    public TimeSpan ElapsedTime => stopwatch.Elapsed;

    /// <summary>Delta time since the last update frame.</summary>
    public TimeSpan ElapsedTimeFromLastUpdate { get; internal set; }

    /// <summary>Time scale multiplier. 1.0 = normal speed.</summary>
    public float TimeScale { get; set; } = 1.0f;

#endregion

#region Internal Methods

    internal void Start()
    {
        stopwatch.Start();
    }

    internal void Update()
    {
        ElapsedTimeFromLastUpdate = stopwatchUpdate.Elapsed;
        stopwatchUpdate.Restart();
    }

#endregion
}
