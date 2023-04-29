using System.Numerics;

namespace Equilibrium.Components;

public struct Position
{
    public Vector3 Value;
}

public struct Scale
{
    public Vector3 Value;
}

public struct Rotation
{
    public Quaternion Value;
}

public struct Transform
{
    public Matrix4x4 Value;
}

public struct Project
{
    public Matrix4x4 Value;
}