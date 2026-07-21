using lychee;
using lychee.attributes;
using lychee_game.components._2d;

namespace lychee_game.Demo;

/// <summary>
/// One-time startup system for the simplest demo:
/// a main camera and a single white quad at the center of the screen.
/// </summary>
[AutoImplSystem]
public partial class SingleInstanceDemoSystem
{
#region Execute

    private static void Execute(Commands commands)
    {
        var camera = commands.CreateEntity();
        camera.AddComponent(new Camera2D());
        camera.AddComponent(new MainCamera());

        var sprite = commands.CreateEntity();
        sprite.AddComponent(new Sprite2D());
        sprite.AddComponent(new Transform2D
        {
            Scale = new Scale2D
            {
                Value = new System.Numerics.Vector2(100.0f, 100.0f)
            }
        });
        sprite.AddComponent(new ZIndex { Value = 0 });
    }

#endregion
}
