
using System.Numerics;
using System.Runtime.CompilerServices;
using MathNet.Numerics;

public static class MathUtils
{
    public enum Handness
    {
        Left,
        Right
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float truncbx(float a) { return (float)((int)(a)); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float fract(float a) { return a - truncbx(a); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float floorbx(float a)
    {
        if (a < 0.0f)
        {
            float fr = fract(-a);
            float result = -a - fr;

            return -(0.0f != fr ? result + 1.0f : result);
        }

        return a - fract(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Mod(float a, float b) => a - b * floorbx(a / b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(this Vector3 vector)
    {
        float max;

        max = vector[0];
        if (vector[1] > max)
            max = vector[1];
        if (vector[2] > max)
            max = vector[2];

        return max;
    }

    public static Quaternion CreateFromVectors(Vector3 a, Vector3 b)
    {
        Vector3 axis;
        float cos_theta;
        float cos_half_theta;

        cos_theta = Vector3.Dot(a, b);
        if (cos_theta >= 1.0f - float.Epsilon)
        {  /*  a ∥ b  */
            return Quaternion.Identity;
        }
        if (cos_theta < -1.0f + float.Epsilon)
        {  /*  angle(a, b) = π  */
            axis = Vector3Ortho(a);
            cos_half_theta = 0f;                    /*  cos π/2 */
        }
        else
        {
            axis = Vector3.Cross(a, b);
            cos_half_theta = 1.0f + cos_theta;       /*  cos 0 + cos θ  */
        }

        Quaternion result = new Quaternion(axis.X, axis.Y, axis.Z, cos_half_theta);
        return Quaternion.Normalize(result);
    }

    public static Vector3 Vector3Ortho(Vector3 vector)
    {
        float ignore;
        float f = Modf(Math.Abs(vector.X) + 0.5f, out ignore);

        return new Vector3(-vector.Y, vector.X - f * vector.Z, f * vector.Y);
    }

    public static float Modf(float value, out float integerPart)
    {
        integerPart = (float)Math.Truncate(value);
        return value - integerPart;
    }

    //  Generates a left-handed perspective projection matrix that does not normalize the depth range of the projected points.
    public static void CreatePerspective(ref Span<float> result, float fovy, float aspect, float nearZ, float farZ, byte homogenousNdc, Handness handness = Handness.Left)
    {
        float height = 1.0f / MathF.Tan((float)Trig.DegreeToRadian(fovy) * 0.5f);
        float width = height * 1.0f / aspect;
        float diff = farZ - nearZ;
        float aa = homogenousNdc == 1 ? (farZ + nearZ) / diff : farZ / diff;
        float bb = homogenousNdc == 1 ? (2.0f * farZ * nearZ) / diff : nearZ * aa;
        result.Clear();
        result[0] = width;
        result[5] = height;
        result[8] = (Handness.Right == handness) ? 0 : -0;
        result[9] = (Handness.Right == handness) ? 0 : -0;
        result[10] = (Handness.Right == handness) ? -aa : aa;
        result[11] = (Handness.Right == handness) ? -1.0f : 1.0f;
        result[14] = -bb;
    }

    public static Matrix3x3 ToMatrix3x3(this Matrix4x4 mat)
    {
        return new Matrix3x3
        {
            M11 = mat.M11,
            M12 = mat.M12,
            M13 = mat.M13,
            M21 = mat.M21,
            M22 = mat.M22,
            M23 = mat.M23,
            M31 = mat.M31,
            M32 = mat.M32,
            M33 = mat.M33
        };
    }
}