using lychee.attributes;
using lychee.interfaces;
using lychee_game.components._2d;
using lychee_game.resources._2d;

namespace lychee_game.systems._2d;

/// <summary>
/// Records draw commands from Sprite2D + Transform2D entities into the render queue.
/// Unpacks Material into Effect + SamplerState for the DrawRecord.
/// </summary>
[SystemFilter(All = new[] { typeof(Sprite2D), typeof(Transform2D), typeof(ZIndex) })]
[AutoImplSystem]
public partial class SpriteRecordSystem
{
#region Execute

    private static void Execute(in Sprite2D sprite, in Transform2D xform, in ZIndex z,
        [Resource] RenderQueue queue, [Resource] MaterialList materials)
    {
        xform.CalculateTransform(out var world);

        // Unpack Material: default{0,0} hits slot 0 = DefaultMaterial (invariant)
        if (!materials.TryGet(sprite.Material, out var mat) || mat is null)
        {
            return;
        }

        queue.Enqueue(new DrawRecord
        {
            Mesh = sprite.Mesh,
            Texture = sprite.Texture,
            Effect = mat.Effect,
            Sampler = mat.Sampler,
            WorldMatrix = world,
            UV = sprite.UV,
            ZIndex = z.Value,
            Tint = sprite.Tint
        });
    }

#endregion
}
