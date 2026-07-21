using lychee;
using lychee_game;
using lychee_game.Demo;
using lychee_game.schedules;
using SDL = SDL3.SDL;

// ── Select which demo to run ──
// Change the value below to switch between demos:
//   Demo.InstancedDraw  – 100 randomly placed quads
//   Demo.SingleInstance – a single quad at the center
const Demo demo = Demo.SingleInstance;
// ────────────────────────────────

SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO);

var app = new App();

app.InstallPlugin<LycheeGamePlugin>();
app.InstallPlugin(new BasicRenderPlugin(new ()
{
    DebugMode = true
}));

var startUp = app.GetSchedule<FireOnceSchedule>("StartUp")!;
switch (demo)
{
    case Demo.SingleInstance:
        startUp.AddSystem<SingleInstanceDemoSystem>();
        break;

    case Demo.InstancedDraw:
        startUp.AddSystem<InstancedDrawDemoSystem>();
        break;
}

var running = true;
while (running)
{
    while (SDL.SDL_PollEvent(out var e))
    {
        if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_QUIT ||
            e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
        {
            running = false;
        }
    }

    app.Update();
}

app.Dispose();
SDL.SDL_Quit();

/// <summary>
/// Available demos that can be selected at the top of this file.
/// </summary>
internal enum Demo
{
    /// <summary>
    /// A single white quad at the center of the screen.
    /// </summary>
    SingleInstance,

    /// <summary>
    /// 100 randomly placed quads with varying sizes.
    /// </summary>
    InstancedDraw,
}
