public struct Matrix3x3
{
    public float M11, M12, M13;
    public float M21, M22, M23;
    public float M31, M32, M33;

    public Matrix3x3(
        float m11, float m12, float m13,
        float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M31 = m31;
        M32 = m32;
        M33 = m33;
    }

    public static Matrix3x3 Transpose(Matrix3x3 matrix)
    {
        return new Matrix3x3(
            matrix.M11, matrix.M21, matrix.M31,
            matrix.M12, matrix.M22, matrix.M32,
            matrix.M13, matrix.M23, matrix.M33);
    }

    public static Matrix3x3 Adjugate(Matrix3x3 mat)
    {
        float a = mat.M11, b = mat.M12, c = mat.M13, d = mat.M21, e = mat.M22, f = mat.M23,
              g = mat.M31, h = mat.M32, i = mat.M33;

        Matrix3x3 dest = new Matrix3x3
        {
            M11 = e * i - f * h,
            M12 = -(b * i - h * c),
            M13 = b * f - e * c,
            M21 = -(d * i - g * f),
            M22 = a * i - c * g,
            M23 = -(a * f - d * c),
            M31 = d * h - g * e,
            M32 = -(a * h - g * b),
            M33 = a * e - b * d
        };

        return dest;
    }
}