using lychee;

namespace lychee_game.schedules;

/// <summary>
/// Execute once then idle until <see cref="Reset"/> is called.
/// </summary>
/// <param name="app">The application.</param>
/// <param name="commitPoint">The commit point.</param>
public sealed class FireOnceSchedule(
    App app,
    string name,
    BasicSchedule.ExecutionModeEnum executionMode = BasicSchedule.ExecutionModeEnum.SingleThread,
    BasicSchedule.CommitPointEnum commitPoint = BasicSchedule.CommitPointEnum.Synchronization)
    : BasicSchedule(app, name, executionMode, commitPoint)
{
#region Private Fields

    private bool fired;

#endregion

#region ISchedule Implementations

    /// <summary>
    /// Executes the schedule once, then goes idle until <see cref="Reset"/> is called.
    /// </summary>
    public override void Execute()
    {
        if (!fired)
        {
            fired = true;
            DoExecute();
        }
    }

#endregion

#region Public Methods

    /// <summary>
    /// Resets the schedule so it can fire again.
    /// </summary>
    public void Reset()
    {
        fired = false;
    }

#endregion
}
