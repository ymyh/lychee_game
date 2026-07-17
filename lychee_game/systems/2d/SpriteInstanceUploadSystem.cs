using System.Numerics;
using lychee.attributes;
using lychee.interfaces;
using lychee_game.resources;
using lychee_game.resources._2d;

namespace lychee_game.systems._2d;

/// <summary>
/// Sorts the render queue and uploads sprite instance data before the render pass begins.
/// </summary>
[AutoImplSystem]
public partial class SpriteInstanceUploadSystem
{
#region Execute

    private static void Execute([Resource] GpuDevice device, [Resource] RenderContext ctx,
        [Resource] GpuUploadBuffer upload, [Resource] RenderQueue queue,
        [Resource] SpriteInstanceBuffer instances)
    {
        if (ctx.CommandBuffer == IntPtr.Zero || ctx.SwapchainTexture == IntPtr.Zero)
        {
            return;
        }

        instances.Clear();

        if (queue.Records.Count == 0)
        {
            return;
        }

        queue.Sort();

        foreach (var r in queue.Records)
        {
            instances.Add(new SpriteInstance
            {
                World = r.WorldMatrix,
                UvRect = new Vector4(r.UV.Min.X, r.UV.Min.Y, r.UV.Max.X, r.UV.Max.Y),
                Tint = r.Tint
            });
        }

        instances.Upload(device, ctx.CommandBuffer, upload);
    }

#endregion
}
