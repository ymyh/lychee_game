using lychee;
using lychee_game;
using lychee_game.Demo;
using lychee_game.schedules;
using SDL = SDL3.SDL;

// Initialize SDL
SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO);

var app = new App();

// Install game framework + rendering plugin
app.InstallPlugin<LycheeGamePlugin>();
app.InstallPlugin<BasicRenderPlugin>();

// Add demo setup system to StartUp schedule (runs once)
var startUp = app.GetSchedule<FireOnceSchedule>("StartUp")!;
startUp.AddSystem<DemoSetupSystem>();

// Main game loop
var running = true;
while (running)
{
    // Poll SDL events
    while (SDL.SDL_PollEvent(out var e))
    {
        if (e.type == (uint)SDL.SDL_EventType.SDL_EVENT_QUIT ||
            e.type == (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
        {
            running = false;
        }
    }

    // Execute all ECS schedules (StartUp → ... → Last)
    app.Update();
}

// Cleanup
app.Dispose();
SDL.SDL_Quit();
