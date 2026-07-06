using System.Numerics;
using System.Runtime.InteropServices;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components._2d;

/// <summary>
/// 2D position component.
/// </summary>
[Component]
public partial struct Position2D
{
#region Public fields

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
    /// Scale in 2D space.
    /// </summary>
    public Vector2 Value;

#endregion
}

/// <summary>
/// Composite 2D transform with matrix calculation.
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
    /// Scale component.
    /// </summary>
    public Scale2D Scale;

#endregion

#region Public Methods

    /// <summary>
    /// Computes the combined transform matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <param name="result">The resulting 4x4 transform matrix.</param>
    public void CalculateTransform(out Matrix4x4 result)
    {
        result = Matrix4x4.CreateScale(new Vector3(Scale.Value, 1.0f)) * Matrix4x4.CreateFromYawPitchRoll(Rotation.Value.Y, Rotation.Value.X, Rotation.Value.Z) * Matrix4x4.CreateTranslation(new(Position.Value, 0.0f));
    }

#endregion
}
