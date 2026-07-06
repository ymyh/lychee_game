using lychee_game.resources;
using lychee_game.schedules;
using lychee;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game;

/// <summary>
/// System that initializes the Time resource on startup.
/// </summary>
[AutoImplSystem]
public partial class InitBasicTimePluginSystem
{
#region Execute

    private static void Execute([Resource] Time time)
    {
        time.Start();
    }

#endregion
}

/// <summary>
/// System that updates the Time resource delta each frame.
/// </summary>
[AutoImplSystem]
public partial class UpdateTimeResourceSystem
{
#region Execute

    private static void Execute([Resource] Time time)
    {
        time.Update();
    }

#endregion
}

/// <summary>
/// Provide basic time support. Requires <see cref="LycheeGamePlugin"/>. <br/>
/// Add a <see cref="Time"/> resource to the app. <br/>
/// Add a <see cref="InitBasicTimePluginSystem"/> to the <see cref="LycheeGamePlugin.StartUp"/> schedule. <br/>
/// Add a <see cref="UpdateTimeResourceSystem"/> to the <see cref="LycheeGamePlugin.Update"/> schedule.
/// </summary>
public sealed class BasicTimePlugin : IPlugin
{
#region Public Fields

    /// <summary>
    /// System that initializes the Time resource on startup.
    /// </summary>
    public readonly InitBasicTimePluginSystem InitBasicTimePluginSystem = new();

    /// <summary>
    /// System that updates the Time resource delta each frame.
    /// </summary>
    public readonly UpdateTimeResourceSystem UpdateTimeResourceSystem = new();

#endregion

#region IPlugin Implementation

    public void Install(App app)
    {
        if (!app.HasResource<LycheeGamePlugin>()) throw new PluginRequirementException(nameof(BasicTimePlugin));

        app.AddResource(new Time());

        var startUp = app.GetSchedule<FireOnceSchedule>("StartUp")!;
        startUp.AddSystem(InitBasicTimePluginSystem);

        var update = app.GetSchedule<DefaultSchedule>("Update")!;
        update.AddSystem(UpdateTimeResourceSystem);
    }

#endregion
}
