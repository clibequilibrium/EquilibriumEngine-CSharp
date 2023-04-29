using System.Numerics;
using static Bgfx.bgfx;

namespace Equilibrium.Components;

public struct ColorVector3
{
    public Vector3 Value;

    public ColorVector3(float r, float g, float b)
    {
        Value = new Vector3(r, g, b);
    }
}

public struct ScreenPosVertex
{
    public float X;
    public float Y;
}

public enum Month
{
    January,
    February,
    March,
    April,
    May,
    June,
    July,
    August,
    September,
    October,
    November,
    December
}

public struct Sun
{
    public Vector3 NorthDir;
    public Vector3 SunDir;
    public Vector3 UpDir;
    public float Latitude;
    public Month Month;

    public float EclipticObliquity;
    public float Delta;
}

public struct SkyData
{
    public VertexBufferHandle Vbh;
    public IndexBufferHandle Ibh;
    [Shader("vs_sky", "fs_sky")]
    public ProgramHandle SkyProgram;

    public UniformHandle USunLuminance;
    public UniformHandle USkyLuminanceXYZ;
    public UniformHandle USkyLuminance;
    public UniformHandle USunDirection;
    public UniformHandle UParameters;
    public UniformHandle UPerezCoeff;

    public float Time;
    public float TimeScale;
    public float Turbidity;
}

public struct SkyConstants
{
    public static readonly Dictionary<float, ColorVector3> SunLuminanceXYZTable = new Dictionary<float, ColorVector3>
    {
        { 5.0f,  new (  0.000000f, 0.000000f, 0.000000f ) },
        { 7.0f,  new ( 12.703322f, 12.989393f,  9.100411f ) },
        { 8.0f,  new ( 13.202644f, 13.597814f, 11.524929f ) },
        { 9.0f,  new(13.192974f, 13.597458f, 12.264488f ) },
        { 10.0f, new(13.132943f, 13.535914f, 12.560032f ) },
        { 11.0f, new(13.088722f, 13.489535f, 12.692996f ) },
        { 12.0f, new(13.067827f, 13.467483f, 12.745179f ) },
        { 13.0f, new(13.069653f, 13.469413f, 12.740822f ) },
        { 14.0f, new(13.094319f, 13.495428f, 12.678066f ) },
        { 15.0f, new(13.142133f, 13.545483f, 12.526785f ) },
        { 16.0f, new(13.201734f, 13.606017f, 12.188001f ) },
        { 17.0f, new(13.182774f, 13.572725f, 11.311157f ) },
        { 18.0f, new(12.448635f, 12.672520f, 8.267771f ) },
        { 20.0f, new (0.000000f, 0.000000f, 0.000000f ) },
    };

    public static readonly Dictionary<float, ColorVector3> SkyLuminanceXYZTable = new Dictionary<float, ColorVector3>
    {
        {  0.0f, new( 0.308f,    0.308f,    0.411f    ) },
        {  1.0f, new( 0.308f,    0.308f,    0.410f    ) },
        { 2.0f, new(0.301f, 0.301f, 0.402f) },
        { 3.0f, new(0.287f, 0.287f, 0.382f) },
        { 4.0f, new(0.258f, 0.258f, 0.344f) },
        { 5.0f, new(0.258f, 0.258f, 0.344f) },
        { 7.0f, new(0.962851f, 1.000000f, 1.747835f) },
        { 8.0f, new(0.967787f, 1.000000f, 1.776762f) },
        { 9.0f, new(0.970173f, 1.000000f, 1.788413f) },
        { 10.0f, new(0.971431f, 1.000000f, 1.794102f) },
        { 11.0f, new(0.972099f, 1.000000f, 1.797096f) },
        { 12.0f, new(0.972385f, 1.000000f, 1.798389f) },
        { 13.0f, new(0.972361f, 1.000000f, 1.798278f) },
        { 14.0f, new(0.972020f, 1.000000f, 1.796740f) },
        { 15.0f, new(0.971275f, 1.000000f, 1.793407f) },
        { 16.0f, new(0.969885f, 1.000000f, 1.787078f) },
        { 17.0f, new(0.967216f, 1.000000f, 1.773758f) },
        { 18.0f, new(0.961668f, 1.000000f, 1.739891f) },
        { 20.0f, new(0.264f, 0.264f, 0.352f) },
        { 21.0f, new(0.264f, 0.264f, 0.352f) },
        { 22.0f, new(0.290f, 0.290f, 0.386f) },
        { 23.0f, new(0.303f, 0.303f, 0.404f) },
    };

    // Turbidity tables. Taken from:
    // A. J. Preetham, P. Shirley, and B. Smits. A Practical Analytic Model for Daylight. SIGGRAPH '99
    // Coefficients correspond to xyY colorspace.
    public static ColorVector3[] ABCDE = new ColorVector3[]
    {
        new ( -0.2592f, -0.2608f, -1.4630f ),
        new (  0.0008f,  0.0092f,  0.4275f ),
        new (  0.2125f,  0.2102f,  5.3251f ),
        new ( -0.8989f, -1.6537f, -2.5771f ),
        new (  0.0452f,  0.0529f,  0.3703f ),
    };

    public static ColorVector3[] ABCDE_t = new ColorVector3[]
    {
        new ( -0.0193f, -0.0167f,  0.1787f ),
        new ( -0.0665f, -0.0950f, -0.3554f ),
        new ( -0.0004f, -0.0079f, -0.0227f ),
        new ( -0.0641f, -0.0441f,  0.1206f ),
        new ( -0.0033f, -0.0109f, -0.0670f ),
    };

    // HDTV rec. 709 matrix.
    public static float[] M_XYZ2RGB = new float[]
    {
        3.240479f, -0.969256f,  0.055648f,
        -1.53715f,   1.875991f, -0.204043f,
        -0.49853f,   0.041556f,  1.057311f,
    };

    public static ColorVector3 XyzToRgb(ColorVector3 xyz)
    {
        ColorVector3 rgb = default;
        rgb.Value.X = M_XYZ2RGB[0] * xyz.Value.X + M_XYZ2RGB[3] * xyz.Value.Y + M_XYZ2RGB[6] * xyz.Value.Z;
        rgb.Value.Y = M_XYZ2RGB[1] * xyz.Value.X + M_XYZ2RGB[4] * xyz.Value.Y + M_XYZ2RGB[7] * xyz.Value.Z;
        rgb.Value.Z = M_XYZ2RGB[2] * xyz.Value.X + M_XYZ2RGB[5] * xyz.Value.Y + M_XYZ2RGB[8] * xyz.Value.Z;
        return rgb;
    }
}