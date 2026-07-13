using lychee;
using lychee.attributes;
using lychee.interfaces;
using lychee_game.components._2d;

namespace lychee_game.Demo;

/// <summary>
/// One-time startup system that creates the demo scene:
/// a main camera and a default sprite (white quad).
/// </summary>
[AutoImplSystem]
public partial class DemoSetupSystem
{
#region Execute

    private static void Execute(Commands commands)
    {
        // Create main camera entity
        var camera = commands.CreateEntity();
        camera.AddComponent(new Camera2D { Zoom = 1.0f });
        camera.AddComponent(new MainCamera());

        // Create a sprite entity with all defaults:
        // Mesh=UnitQuad[0], Material=DefaultMaterial[0], Texture=WhiteTexture[0], Tint=White
        var sprite = commands.CreateEntity();
        sprite.AddComponent(new Sprite2D());
        sprite.AddComponent(new Transform2D());
        sprite.AddComponent(new ZIndex());
    }

#endregion
}
