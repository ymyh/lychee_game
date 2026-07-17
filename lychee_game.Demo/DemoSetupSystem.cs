using System.Numerics;
using lychee;
using lychee.attributes;
using lychee_game.components._2d;

namespace lychee_game.Demo;

/// <summary>
/// One-time startup system that creates the demo scene:
/// a main camera and 100 randomly placed white quads.
/// </summary>
[AutoImplSystem]
public partial class DemoSetupSystem
{
#region Execute

    private static void Execute(Commands commands)
    {
        var camera = commands.CreateEntity();
        camera.AddComponent(new Camera2D());
        camera.AddComponent(new MainCamera());

        // Camera orthographic matches window pixels (default 1280x720): visible ~[-640,640] x [-360,360].
        var rng = new Random(42);
        for (var n = 0; n < 100; n++)
        {
            var size = 16.0f + rng.NextSingle() * 32.0f;
            var sprite = commands.CreateEntity();
            sprite.AddComponent(new Sprite2D());
            sprite.AddComponent(new Transform2D
            {
                Position = new Position2D
                {
                    Value = new Vector2(
                        (rng.NextSingle() - 0.5f) * 1200.0f,
                        (rng.NextSingle() - 0.5f) * 640.0f)
                },
                Scale = new Scale2D { Value = new Vector2(size, size) }
            });
            sprite.AddComponent(new ZIndex { Value = n });
        }
    }

#endregion
}
