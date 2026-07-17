using System.Numerics;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// 2D position component.
/// </summary>
[Component]
public partial struct Position2D
{
#region Public Fields

    /// <summary>
    /// Position in 2D space.
    /// </summary>
    public Vector2 Value;

#endregion
}

/// <summary>
/// 2D rotation component (Euler angles in Vector3).
/// </summary>
[Component]
public partial struct Rotation2D
{
#region Public Fields

    /// <summary>
    /// Rotation as Euler angles (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 Value;

#endregion
}

/// <summary>
/// 2D scale component.
/// </summary>
[Component]
public partial struct Scale2D
{
#region Public Fields

    /// <summary>
    /// Scale in 2D space. Default is (1, 1) when constructed via <c>new Scale2D()</c>.
    /// </summary>
    public Vector2 Value;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Scale2D with unit scale (1, 1).
    /// </summary>
    public Scale2D()
    {
        Value = Vector2.One;
    }

#endregion
}

/// <summary>
/// Composite 2D transform with matrix calculation.
/// Must be created via <c>new Transform2D()</c> so Scale defaults to (1, 1).
/// Do not use <c>default(Transform2D)</c> — it bypasses the constructor.
/// </summary>
[Component]
public partial struct Transform2D
{
#region Public Fields

    /// <summary>
    /// Position component.
    /// </summary>
    public Position2D Position;

    /// <summary>
    /// Rotation component.
    /// </summary>
    public Rotation2D Rotation;

    /// <summary>
    /// Scale component. Default is (1, 1).
    /// </summary>
    public Scale2D Scale;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Transform2D at the origin with unit scale and zero rotation.
    /// </summary>
    public Transform2D()
    {
        Position = default;
        Rotation = default;
        Scale = new Scale2D();
    }

#endregion

#region Public Methods

    /// <summary>
    /// Computes the combined transform matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <param name="result">The resulting 4x4 transform matrix.</param>
    public void CalculateTransform(out Matrix4x4 result)
    {
        result = Matrix4x4.CreateScale(new Vector3(Scale.Value, 1.0f))
                 * Matrix4x4.CreateFromYawPitchRoll(Rotation.Value.Y, Rotation.Value.X, Rotation.Value.Z)
                 * Matrix4x4.CreateTranslation(new(Position.Value, 0.0f));
    }

#endregion
}
