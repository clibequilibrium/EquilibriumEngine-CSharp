
using System.Numerics;
using System.Runtime.CompilerServices;

public static class MathUtils
{
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
    public static Matrix4x4 CreatePerspective(float fovy, float aspect, float nearZ, float farZ)
    {
        float f, fn;

        Matrix4x4 dest = new Matrix4x4();

        f = 1.0f / (float)Math.Tan(fovy * 0.5f);
        fn = 1.0f / (nearZ - farZ);

        dest.M11 = f / aspect;
        dest.M22 = f;
        dest.M33 = -(nearZ + farZ) * fn;
        dest.M34 = 1.0f;
        dest.M43 = 2.0f * nearZ * farZ * fn;

        return dest;
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