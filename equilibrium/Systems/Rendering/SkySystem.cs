using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Bgfx;
using Equilibrium.Components;
using MathNet.Numerics;
using static Bgfx.bgfx;

namespace Equilibrium.Systems.Rendering;

public partial class SkySystem : BaseSystem<World, float>, IRenderSystem
{
    private DynamicValueController sunLuminanceXYZController;
    private DynamicValueController skyLuminanceXYZController;

    public SkySystem(World world) : base(world)
    {
        sunLuminanceXYZController = new DynamicValueController(SkyConstants.SunLuminanceXYZTable);
        skyLuminanceXYZController = new DynamicValueController(SkyConstants.SkyLuminanceXYZTable);
    }

    public override void Initialize()
    {
        base.Initialize();

        InitializeSky();
    }

    [Query]
    [All<SkyData, Sun>]
    private void DrawSky(ref SkyData skyData, ref Sun sun)
    {
        float deltaTime = Data;
        skyData.Time += skyData.TimeScale * deltaTime;
        skyData.Time = MathUtils.Mod(skyData.Time, 24f);

        UpdateSun(ref sun, ref skyData, skyData.Time);

        ColorVector3 sunLuminanceXYZ = sunLuminanceXYZController.GetValue(skyData.Time);
        ColorVector3 sunLuminanceRGB = SkyConstants.XyzToRgb(sunLuminanceXYZ);

        ColorVector3 skyLuminanceXYZ = skyLuminanceXYZController.GetValue(skyData.Time);
        ColorVector3 skyLuminanceRGB = SkyConstants.XyzToRgb(skyLuminanceXYZ);

        unsafe
        {
            bgfx.set_uniform(skyData.USunLuminance, &sunLuminanceRGB, ushort.MaxValue);
            bgfx.set_uniform(skyData.USkyLuminanceXYZ, &skyLuminanceXYZ, ushort.MaxValue);
            bgfx.set_uniform(skyData.USkyLuminance, &skyLuminanceRGB, ushort.MaxValue);

            Vector3 sunDirection = sun.SunDir;
            bgfx.set_uniform(skyData.USunDirection, &sunDirection, ushort.MaxValue);

            float* exposition = stackalloc float[] { 0.02f, 3.0f, 0.1f, skyData.Time };

            bgfx.set_uniform(skyData.UParameters, exposition, ushort.MaxValue);

            float* perezCoeff = stackalloc float[4 * 5];

            ComputePerezCoeff(skyData.Turbidity, perezCoeff);
            bgfx.set_uniform(skyData.UPerezCoeff, perezCoeff, 5);
        }

        // Draw
        bgfx.set_state((ulong)(StateFlags.WriteRgb | StateFlags.DepthTestEqual), 0);
        bgfx.set_index_buffer(skyData.Ibh, 0, uint.MaxValue);
        bgfx.set_vertex_buffer(0, skyData.Vbh, 0, uint.MaxValue);

        // bgfx_view_id_t viewId = ecs_count(it->world, DeferredRenderer) > 0 ? 3 : 0;
        bgfx.submit(0, skyData.SkyProgram, 0, (byte)DiscardFlags.All);
    }

    private unsafe void InitializeSky()
    {
        SkyData skyData = default;
        Sun sun = default;

        sun.Latitude = 50.0f;
        sun.Month = Month.June;
        sun.EclipticObliquity = (float)Trig.DegreeToRadian(23.4f);
        sun.Delta = 0.0f;

        sun.NorthDir = Vector3.UnitX;
        sun.SunDir = -Vector3.UnitY;
        sun.UpDir = Vector3.UnitY;

        VertexLayout screenPosVertex = default;
        screenPosVertex = screenPosVertex.Begin().Add(Attrib.Position, 2, AttribType.Float).End();

        const int vertical_count = 32;
        const int horizontal_count = 32;
        int vertices_size = vertical_count * horizontal_count * sizeof(ScreenPosVertex);
        int indices_size = (vertical_count - 1) * (horizontal_count - 1) * 6 * sizeof(ushort);

        ScreenPosVertex* vertices = stackalloc ScreenPosVertex[vertices_size];
        ushort* indices = stackalloc ushort[indices_size];

        for (int i = 0; i < vertical_count; i++)
        {
            for (int j = 0; j < horizontal_count; j++)
            {
                ScreenPosVertex* v = &(vertices[i * vertical_count + j]);
                v->X = (float)(j) / (horizontal_count - 1) * 2.0f - 1.0f;
                v->Y = (float)(i) / (vertical_count - 1) * 2.0f - 1.0f;
            }
        }

        int k = 0;
        for (int i = 0; i < vertical_count - 1; i++)
        {
            for (int j = 0; j < horizontal_count - 1; j++)
            {
                indices[k++] = (ushort)(j + 0 + horizontal_count * (i + 0));
                indices[k++] = (ushort)(j + 1 + horizontal_count * (i + 0));
                indices[k++] = (ushort)(j + 0 + horizontal_count * (i + 1));

                indices[k++] = (ushort)(j + 1 + horizontal_count * (i + 0));
                indices[k++] = (ushort)(j + 1 + horizontal_count * (i + 1));
                indices[k++] = (ushort)(j + 0 + horizontal_count * (i + 1));
            }
        }

        UpdateSun(ref sun, ref skyData, 0);

        skyData.Vbh = World.CreateVertexBuffer(new nint(vertices), sizeof(ScreenPosVertex) * vertical_count * horizontal_count, screenPosVertex);
        skyData.Ibh = World.CreateIndexBuffer(new nint(indices), sizeof(ushort) * k);

        skyData.Time = 17.0f;
        skyData.TimeScale = 0.0f;

        skyData.USunLuminance = World.CreateUniform("u_sunLuminance", UniformType.Vec4);
        skyData.USkyLuminanceXYZ = World.CreateUniform("u_skyLuminanceXYZ", UniformType.Vec4);
        skyData.USkyLuminance = World.CreateUniform("u_skyLuminance", UniformType.Vec4);
        skyData.USunDirection = World.CreateUniform("u_sunDirection", UniformType.Vec4);
        skyData.UParameters = World.CreateUniform("u_parameters", UniformType.Vec4);
        skyData.UPerezCoeff = World.CreateUniform("u_perezCoeff", UniformType.Vec4, 5);

        skyData.Turbidity = 2.15f;

        var entity = World.Create(new Name { Value = "SkySystemData" });
        World.LoadShaders(in entity, ref skyData);

        entity.Add(skyData);
        entity.Add(sun);
    }

    private static void UpdateSun(ref Sun sun, ref SkyData sky_data, float hour)
    {
        hour -= 12.0f;

        // Calculate sun orbit
        float day = 30.0f * (float)((int)sun.Month) + 15.0f;
        float lambda = 280.46f + 0.9856474f * day;
        lambda = (float)Trig.DegreeToRadian(lambda);
        sun.Delta = (float)Math.Asin(Math.Sin(sun.EclipticObliquity) * Math.Sin(lambda));

        // Update sun position
        float latitude = (float)Trig.DegreeToRadian(sun.Latitude);
        float hh = hour * MathF.PI / 12.0f;

        float azimuth =
            (float)Math.Atan2(Math.Sin(hh), Math.Cos(hh) * Math.Sin(latitude) - Math.Tan(sun.Delta) * Math.Cos(latitude));
        float altitude =
            (float)Math.Asin(Math.Sin(latitude) * Math.Sin(sun.Delta) + Math.Cos(latitude) * Math.Cos(sun.Delta) * Math.Cos(hh));

        Quaternion rot0 = Quaternion.Identity;
        rot0 = Quaternion.CreateFromAxisAngle(sun.UpDir, azimuth);

        Vector3 dir = Vector3.Transform(sun.NorthDir, rot0);
        Vector3 uxd = Vector3.Cross(sun.UpDir, dir);

        Quaternion rot1 = Quaternion.Identity;
        rot1 = Quaternion.CreateFromAxisAngle(uxd, -altitude);

        sun.SunDir = Vector3.Transform(dir, rot1);

        if (sun.Latitude == 50.0f && (int)(sky_data.Time) == 13 && sun.Month == Month.June &&
            (sun.SunDir.Y < 0.0f || sun.SunDir.Z > 0.0f))
        {
            Console.Error.WriteLine("Sun dir is incorrect.");
        }
    }

    private static unsafe void ComputePerezCoeff(float turbidity, float* outPerezCoeff)
    {
        Vector3 turbidityVector = new(turbidity, turbidity, turbidity);
        for (uint ii = 0; ii < 5; ++ii)
        {
            Vector3 tmp;
            Vector3 mulResult;

            Vector3 a = new(SkyConstants.ABCDE_t[ii].Value.X, SkyConstants.ABCDE_t[ii].Value.Y, SkyConstants.ABCDE_t[ii].Value.Z);
            Vector3 b = new(SkyConstants.ABCDE[ii].Value.X, SkyConstants.ABCDE[ii].Value.Y, SkyConstants.ABCDE[ii].Value.Z);

            mulResult = Vector3.Multiply(a, turbidityVector);
            tmp = Vector3.Add(mulResult, b);

            Vector4* value = (Vector4*)(outPerezCoeff + 4 * ii);
            value->X = tmp.X;
            value->Y = tmp.Y;
            value->Z = tmp.Z;
            value->W = 0f;
        }
    }
}