using System.Numerics;
using System.Runtime.InteropServices;
using lychee.attributes;
using lychee.interfaces;

namespace lychee_game.components;

/// <summary>
/// 3D position component.
/// </summary>
[Component]
public partial struct Position
{
#region Public Fields

    /// <summary>The position value in 3D space.</summary>
    public Vector3 Value;

#endregion
}

/// <summary>
/// 3D rotation component (Euler angles).
/// </summary>
[Component]
public partial struct Rotation
{
#region Public Fields

    /// <summary>The rotation value in Euler angles.</summary>
    public Vector3 Value;

#endregion
}

/// <summary>
/// 3D scale component.
/// </summary>
[Component]
public partial struct Scale
{
#region Public Fields

    /// <summary>The scale value in 3D space.</summary>
    public Vector3 Value;

#endregion
}

/// <summary>
/// Composite 3D transform component combining position, rotation, and scale with matrix calculation.
/// </summary>
[Component]
public partial struct Transform
{
#region Public Fields

    /// <summary>The position portion of the transform.</summary>
    public Position Position;

    /// <summary>The rotation portion of the transform.</summary>
    public Rotation Rotation;

    /// <summary>The scale portion of the transform.</summary>
    public Scale Scale;

#endregion

#region Public Methods

    /// <summary>
    /// Calculates the combined transformation matrix from position, rotation, and scale.
    /// </summary>
    /// <param name="result">The resulting 4x4 transformation matrix.</param>
    public void CalculateTransform(out Matrix4x4 result)
    {
        result = Matrix4x4.CreateScale(Scale.Value) * Matrix4x4.CreateFromYawPitchRoll(Rotation.Value.Y, Rotation.Value.X, Rotation.Value.Z) * Matrix4x4.CreateTranslation(Position.Value);
    }

#endregion
}
