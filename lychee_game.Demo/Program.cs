using lychee;
using lychee_game;
using lychee_game.Demo;
using lychee_game.schedules;
using SDL = SDL3.SDL;

SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO);

var app = new App();

app.InstallPlugin<LycheeGamePlugin>();
app.InstallPlugin(new BasicRenderPlugin(new ()
{
    DebugMode = true
}));

var startUp = app.GetSchedule<FireOnceSchedule>("StartUp")!;
startUp.AddSystem<DemoSetupSystem>();

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
