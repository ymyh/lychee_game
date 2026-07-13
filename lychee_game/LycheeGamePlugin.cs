using lychee_game.resources;
using lychee_game.schedules;
using lychee;
using lychee.interfaces;

namespace lychee_game;

/// <summary>
/// Configuration for BasicGamePlugin: fixed update interval and catch-up count.
/// </summary>
public sealed class DefaultPluginDescriptor
{
#region Public Properties

    /// <summary>
    /// FixedUpdate schedule execution interval, in millisecond, default is 20.
    /// </summary>
    public int FixedUpdateInterval { get; set; } = 20;

    /// <summary>
    /// FixedUpdate schedule maximum catch up attempt count, default is 5.
    /// </summary>
    public int FixedUpdateCatchUpCount { get; set; } = 5;

#endregion
}

/// <summary>
/// A plugin that provides the game loop pipeline with 10 schedules: StartUp through Last.
/// <list type="bullet">
///     <item>
///         <description>
///             <see cref="StartUp"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="First"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="Input"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="FixedUpdate"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="Update"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="PostUpdate"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="Render"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="RenderTransparency"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="RenderUI"/>
///         </description>
///     </item>
///     <item>
///         <description>
///             <see cref="Last"/>
///         </description>
///     </item>
/// </list>
/// </summary>
public sealed class LycheeGamePlugin(DefaultPluginDescriptor desc) : IPlugin
{
#region Public Properties

    /// <summary>The one-time startup schedule. Fires exactly once.</summary>
    public FireOnceSchedule StartUp { get; private set; } = null!;

    /// <summary>The first-frame schedule.</summary>
    public DefaultSchedule First { get; private set; } = null!;

    /// <summary>The input processing schedule.</summary>
    public DefaultSchedule Input { get; private set; } = null!;

    /// <summary>Fixed-interval schedule for physics/logic (default 50Hz).</summary>
    public FixedIntervalSchedule FixedUpdate { get; private set; } = null!;

    /// <summary>The main per-frame update schedule.</summary>
    public DefaultSchedule Update { get; private set; } = null!;

    /// <summary>Post-update schedule, runs after Update.</summary>
    public DefaultSchedule PostUpdate { get; private set; } = null!;

    /// <summary>Opaque geometry rendering schedule.</summary>
    public DefaultSchedule Render { get; private set; } = null!;

    /// <summary>Transparent geometry rendering schedule.</summary>
    public DefaultSchedule RenderTransparency { get; private set; } = null!;

    /// <summary>UI rendering schedule.</summary>
    public DefaultSchedule RenderUI { get; private set; } = null!;

    /// <summary>End-of-frame cleanup schedule.</summary>
    public DefaultSchedule Last { get; private set; } = null!;

#endregion

#region Constructor

    /// <summary>Creates a BasicGamePlugin with default settings.</summary>
    public LycheeGamePlugin() : this(new())
    {
    }

#endregion

#region IPlugin Implementation

    public void Install(App app)
    {
        InitSchedules(app);
        app.AddResource<Window>();
    }

#endregion

#region Private Methods

    private void InitSchedules(App app)
    {
        StartUp = new(app, nameof(StartUp));
        app.AddSchedule(StartUp);

        Input = new(app, nameof(Input));
        app.AddSchedule(Input);

        FixedUpdate = new(app, nameof(FixedUpdate))
        {
            FixedUpdateInterval = desc.FixedUpdateInterval,
            CatchUpCount = desc.FixedUpdateCatchUpCount
        };
        app.AddSchedule(FixedUpdate);

        Update = new(app, nameof(Update));
        app.AddSchedule(Update);

        PostUpdate = new(app, nameof(PostUpdate));
        app.AddSchedule(PostUpdate);

        Render = new(app, nameof(Render));
        app.AddSchedule(Render);

        RenderTransparency = new(app, nameof(RenderTransparency));
        app.AddSchedule(RenderTransparency);

        RenderUI = new(app, nameof(RenderUI));
        app.AddSchedule(RenderUI);
    }

#endregion
}
