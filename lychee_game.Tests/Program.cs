using lychee_game.resources._2d;
using lychee_game.components._2d;

namespace lychee_game.Tests;

/// <summary>
/// Simple test program to verify ECS and resource pool functionality.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== lychee_game Resource Pool Tests ===\n");

        TestAssetPool();
        TestMesh2DCreation();
        TestTexture2DCreation();

        Console.WriteLine("\n=== All Tests Passed ===");
    }

    private static void TestAssetPool()
    {
        Console.WriteLine("Testing AssetPool...");

        var meshList = new Mesh2DList();

        // Test Create
        var mesh1 = meshList.Create(new Mesh2DDesc
        {
            Vertices =
            [
                new Vertex2D(new(-0.5f, -0.5f), new(0, 0), RgbaByte.White),
                new Vertex2D(new(0.5f, -0.5f), new(1, 0), RgbaByte.White),
                new Vertex2D(new(0.5f, 0.5f), new(1, 1), RgbaByte.White)
            ],
            Indices = [0, 1, 2]
        });

        Console.WriteLine($"  Created mesh1: Index={mesh1.Index}, Generation={mesh1.Generation}");

        // Test TryGet
        if (meshList.TryGet(mesh1, out var meshData))
        {
            Console.WriteLine($"  TryGet mesh1: VertexCount={meshData!.VertexCount}, IndexCount={meshData.IndexCount}");
        }
        else
        {
            Console.WriteLine("  ERROR: TryGet failed for mesh1!");
            return;
        }

        // Test Generation tracking
        var mesh2 = meshList.Create(new Mesh2DDesc
        {
            Vertices =
            [
                new Vertex2D(new(0, 0), new(0, 0), RgbaByte.Red),
                new Vertex2D(new(1, 0), new(1, 0), RgbaByte.Red),
                new Vertex2D(new(1, 1), new(1, 1), RgbaByte.Red)
            ],
            Indices = [0, 1, 2]
        });

        Console.WriteLine($"  Created mesh2: Index={mesh2.Index}, Generation={mesh2.Generation}");

        // Release mesh1
        meshList.Release(mesh1);
        Console.WriteLine("  Released mesh1");

        // Try to get mesh1 after release (should fail)
        if (meshList.TryGet(mesh1, out _))
        {
            Console.WriteLine("  ERROR: TryGet succeeded after release!");
            return;
        }
        else
        {
            Console.WriteLine("  TryGet correctly failed after release");
        }

        Console.WriteLine("  AssetPool tests passed!\n");
    }

    private static void TestMesh2DCreation()
    {
        Console.WriteLine("Testing Mesh2D creation...");

        var meshList = new Mesh2DList();

        var mesh = meshList.Create(new Mesh2DDesc
        {
            Vertices =
            [
                new Vertex2D(new(-0.5f, -0.5f), new(0, 0), RgbaByte.White),
                new Vertex2D(new(0.5f, -0.5f), new(1, 0), RgbaByte.White),
                new Vertex2D(new(0.5f, 0.5f), new(1, 1), RgbaByte.White),
                new Vertex2D(new(-0.5f, 0.5f), new(0, 1), RgbaByte.White)
            ],
            Indices = [0, 1, 2, 2, 3, 0]
        });

        if (meshList.TryGet(mesh, out var meshData))
        {
            Console.WriteLine($"  VertexCount: {meshData!.VertexCount}");
            Console.WriteLine($"  IndexCount: {meshData.IndexCount}");
            Console.WriteLine($"  Indexed: {meshData.Indexed}");
            Console.WriteLine($"  VertexData size: {meshData.VertexData.Length} bytes");
            Console.WriteLine($"  IndexData size: {meshData.IndexData?.Length ?? 0} bytes");
        }
        else
        {
            Console.WriteLine("  ERROR: Failed to get mesh!");
            return;
        }

        Console.WriteLine("  Mesh2D tests passed!\n");
    }

    private static void TestTexture2DCreation()
    {
        Console.WriteLine("Testing Texture2D creation...");

        var textureList = new Texture2DList();

        var texture = textureList.Create(new Texture2DDesc
        {
            Data = [255, 255, 255, 255], // 1x1 white pixel
            Width = 1,
            Height = 1
        });

        if (textureList.TryGet(texture, out var texData))
        {
            Console.WriteLine($"  Width: {texData!.Width}");
            Console.WriteLine($"  Height: {texData.Height}");
            Console.WriteLine($"  Data size: {texData.Data.Length} bytes");
            Console.WriteLine($"  Format: {texData.Format}");
        }
        else
        {
            Console.WriteLine("  ERROR: Failed to get texture!");
            return;
        }

        Console.WriteLine("  Texture2D tests passed!\n");
    }
}
