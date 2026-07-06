using System.Diagnostics;
using lychee;

namespace lychee_game.schedules;

/// <summary>
/// Executes at a fixed time interval. It will try to catch up if the interval is missed.
/// </summary>
/// <param name="app">The application.</param>
/// <param name="commitPoint">The commit point.</param>
/// <param name="fixedUpdateInterval">The interval in milliseconds, default is 20.</param>
/// <param name="catchUpCount">The catch-up attempt count in each execute, default is 5.</param>
public sealed class FixedIntervalSchedule(
    App app,
    string name,
    BasicSchedule.ExecutionModeEnum executionMode = BasicSchedule.ExecutionModeEnum.SingleThread,
    BasicSchedule.CommitPointEnum commitPoint = BasicSchedule.CommitPointEnum.Synchronization,
    int fixedUpdateInterval = 20,
    int catchUpCount = 5)
    : BasicSchedule(app, name, executionMode, commitPoint)
{
#region Private Fields

    private long accErr = fixedUpdateInterval;

    private readonly Stopwatch stopwatch = new();

#endregion

#region Public Properties

    /// <summary>
    /// Execution interval in milliseconds. Default is 20ms (50Hz).
    /// </summary>
    public int FixedUpdateInterval { get; set; } = fixedUpdateInterval;

    /// <summary>
    /// Maximum catch-up iterations when the game lags behind.
    /// </summary>
    public int CatchUpCount { get; set; } = catchUpCount;

#endregion

#region ISchedule Implementations

    /// <summary>
    /// Executes the schedule at fixed intervals with drift correction and catch-up support.
    /// </summary>
    public override void Execute()
    {
        var now = stopwatch.ElapsedMilliseconds + accErr;

        if (now >= FixedUpdateInterval)
        {
            now -= FixedUpdateInterval;
            accErr = now;
            stopwatch.Restart();

            DoExecute();
        }

        var i = 0;
        while (now >= FixedUpdateInterval && (i < CatchUpCount || CatchUpCount < 0))
        {
            now -= FixedUpdateInterval;
            accErr = now;
            i++;

            DoExecute();
        }
    }

#endregion
}
