using System.Diagnostics;
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

public partial class BaseRenderingSystem : BaseSystem<World, float>, IRenderSystem
{
    private QueryDescription frameDataQuery;
    private QueryDescription cameraQuery;

    public BaseRenderingSystem(World world) : base(world)
    {
        cameraQuery = new QueryDescription().WithAll<Camera>();
        frameDataQuery = new QueryDescription().WithAll<FrameData, AppWindow>();
    }

    [Query]
    [All<BgfxComponent>, None<FrameData>]
    private unsafe void InitializeFrameData(in Entity entity)
    {
        VertexLayout vertexLayout = default;
        FrameData frameData = default;

        vertexLayout = vertexLayout.Begin().Add(Attrib.Position, 3, AttribType.Float).End();

        frameData.BlitSampler = World.CreateUniform("s_texColor", UniformType.Sampler);
        frameData.CamPosUniform = World.CreateUniform("u_camPos", UniformType.Vec4);
        frameData.NormalMatrixUniform = World.CreateUniform("u_normalMatrix", UniformType.Mat3);
        frameData.ExposureVecUniform = World.CreateUniform("u_exposureVec", UniformType.Vec4);
        frameData.TonemappingModeVecUniform = World.CreateUniform("u_tonemappingModeVec", UniformType.Vec4);

        // triangle used for blitting
        float BOTTOM = -1.0f, TOP = 3.0f, LEFT = -1.0f, RIGHT = 3.0f;
        PosVertex* vertices = stackalloc PosVertex[] { new(LEFT, BOTTOM, 0.0f), new(RIGHT, BOTTOM, 0.0f), new(LEFT, TOP, 0.0f) };

        frameData.BlitTriangleBuffer = World.CreateVertexBuffer(new nint(vertices), sizeof(PosVertex) * 3, vertexLayout);
        World.LoadShaders(ref frameData);

        frameData.FrameBuffer = CreateFrameBuffer(true, true);
        bgfx.set_frame_buffer_name(frameData.FrameBuffer, "Render framebuffer (pre-postprocessing)", int.MaxValue);

        entity.Add(frameData);

        Console.WriteLine("Base rendering system initialized.");
    }

    public override void BeforeUpdate(in float t)
    {
        base.BeforeUpdate(t);
        OnBaseRendererUpdate();
    }

    public override void AfterUpdate(in float t)
    {
        base.AfterUpdate(t);
        BlitToScreen();
    }

    private void OnBaseRendererUpdate()
    {
        World.Query(in frameDataQuery, (in Entity entity, ref FrameData frameData) =>
        {
            var frameDataCopy = frameData;

            World.Query(in cameraQuery, (in Entity camerEntity, ref Camera camera) =>
            {
                unsafe
                {
                    Vector4 cameraPosition = new Vector4(camera.Position, 1.0f);
                    bgfx.set_uniform(frameDataCopy.CamPosUniform, &cameraPosition, ushort.MaxValue);
                }
            });
        });
    }

    private void BlitToScreen()
    {
        World.Query(in frameDataQuery, (in Entity entity, ref FrameData frameData, ref AppWindow appWindow) =>
        {
            const ushort MAX_VIEW = 254;
            ushort view = MAX_VIEW;

            bgfx.set_view_name(view, "Tonemapping");
            bgfx.set_view_clear(view, (ushort)ClearFlags.None, 255U, 1.0F, 0);
            bgfx.set_view_rect(view, 0, 0, (ushort)appWindow.Width, (ushort)appWindow.Height);

            FrameBufferHandle invalid_handle = new FrameBufferHandle { idx = BgfxConstants.InvalidHandle };
            bgfx.set_view_frame_buffer(view, invalid_handle);
            bgfx.set_state((ulong)(StateFlags.WriteRgb | StateFlags.CullCw), 0);

            TextureHandle frameBufferHandle = bgfx.get_texture(frameData.FrameBuffer, 0);
            bgfx.set_texture(0, frameData.BlitSampler, frameBufferHandle, uint.MaxValue);
            // float exposureVec[4] = {scene->loaded ? scene->camera.exposure
            // : 1.0f};

            unsafe
            {

                float* exposure_vec = stackalloc[] { 1.0f, 0, 0, 0 };
                bgfx.set_uniform(frameData.ExposureVecUniform, exposure_vec, ushort.MaxValue);
                float* tonemapping_mode_vec = stackalloc[] { (float)7, 0, 0, 0 };

                //  enum class TonemappingMode : int
                //     {
                //         NONE = 0,
                //         EXPONENTIAL,
                //         REINHARD,
                //         REINHARD_LUM,
                //         HABLE,
                //         DUIKER,
                //         ACES,
                //         ACES_LUM
                //     };

                bgfx.set_uniform(frameData.TonemappingModeVecUniform, tonemapping_mode_vec, ushort.MaxValue);
            }

            bgfx.set_vertex_buffer(0, frameData.BlitTriangleBuffer, 0, uint.MaxValue);
            bgfx.submit(view, frameData.BlitProgram, 0, (byte)DiscardFlags.All);
        });
    }

    private unsafe FrameBufferHandle CreateFrameBuffer(bool hdr, bool depth)
    {
        TextureHandle* textures = stackalloc TextureHandle[2];
        byte attachments = 0;

        ulong samplerFlags = (ulong)(SamplerFlags.MinPoint | SamplerFlags.MagPoint |
                                SamplerFlags.MipPoint | SamplerFlags.UClamp | SamplerFlags.VClamp);

        // QueryDescription query = new QueryDescription().WithAny<ForwardRenderer>();
        // if (World.CountEntities(in query) > 0)
        // {
        samplerFlags |= (ulong)TextureFlags.RtMsaaX16;
        // }

        TextureFormat format =
            hdr ? TextureFormat.RGBA16F : TextureFormat.BGRA8; // BGRA is often faster
                                                               // (internal GPU format)
        Debug.Assert(bgfx.is_texture_valid(0, false, 1, format, (ulong)TextureFlags.Rt | samplerFlags) == true);

        textures[attachments++] = World.CreateTexture2dScaled(BackbufferRatio.Equal, false, 1, format, (ulong)TextureFlags.Rt | samplerFlags);

        if (depth)
        {
            TextureFormat depthFormat = FindDepthFormat((ulong)TextureFlags.RtWriteOnly | samplerFlags, false);

            Debug.Assert(depthFormat != TextureFormat.Count);
            textures[attachments++] = World.CreateTexture2dScaled(BackbufferRatio.Equal, false, 1, depthFormat, (ulong)TextureFlags.RtWriteOnly | samplerFlags);
        }

        FrameBufferHandle frameBufferHandle = World.CreateFrameBufferFromHandles(attachments, new nint(textures));

        if (!frameBufferHandle.Valid)
            Console.Error.WriteLine("Failed to create framebuffer");

        return frameBufferHandle;
    }

    unsafe TextureFormat FindDepthFormat(ulong textureFlags, bool stencil)
    {
        TextureFormat* depthFormats = stackalloc[] { TextureFormat.D16, TextureFormat.D32 };
        TextureFormat* depthStencilFormats = stackalloc[] { TextureFormat.D24S8 };
        TextureFormat* formats = stencil ? depthStencilFormats : depthFormats;

        int count = stencil ? BgfxUtils.CountOf(depthStencilFormats, 1) : BgfxUtils.CountOf(depthFormats, 2);

        TextureFormat depthFormat = TextureFormat.Count;

        for (int i = 0; i < count; i++)
        {
            if (bgfx.is_texture_valid(0, false, 1, formats[i], textureFlags))
            {
                depthFormat = formats[i];
                break;
            }
        }

        Debug.Assert(depthFormat != TextureFormat.Count);
        return depthFormat;
    }

    public unsafe static bool RendererSupported(bool deferred)
    {
        Caps* caps = bgfx.get_caps();

        bool supported =
            // SDR color attachment
            (caps->formats[(uint)TextureFormat.BGRA8] & (uint)CapsFormatFlags.TextureFramebuffer) != 0 &&
            // HDR color attachment
            (caps->formats[(uint)TextureFormat.RGBA16F] & (uint)CapsFormatFlags.TextureFramebuffer) != 0;

        if (deferred)
        {
            supported = supported && // blitting depth texture after geometry pass
                        (caps->supported & (ulong)CapsFlags.TextureBlit) != 0 &&
                        // multiple render targets
                        // depth doesn't count as an attachment
                        caps->limits.maxFBAttachments >= (uint)GBufferAttachment.GBufferAttachmentCount - 1;

            if (!supported)
            {
                return false;
            }

            for (int i = 0; i < RendererConstants.BufferAttachmentFormats.Length; i++)
            {
                if ((caps->formats[(int)RendererConstants.BufferAttachmentFormats[i]] &
                     (uint)CapsFormatFlags.TextureFramebuffer) == 0)
                    return false;
            }

            return true;

        }
        else
        {
            return supported;
        }
    }

    public static void SetViewProjection(ushort viewId, ref Camera camera, int width, int height)
    {
        Matrix4x4 view;
        Matrix4x4 rotationMat;
        Matrix4x4 translationMat = Matrix4x4.Identity;
        Vector3 negativePos;

        negativePos = -camera.Position;
        rotationMat = Matrix4x4.CreateFromQuaternion(camera.Rotation);
        translationMat = Matrix4x4.CreateTranslation(negativePos);

        // Multiply translationMat * rotationMat (row-major order)
        view = Matrix4x4.Multiply(translationMat, rotationMat);

        unsafe
        {
            float* proj = stackalloc float[16];
            Span<float> projSpan = new Span<float>(proj, 16);
            // left handed coordinate system perspective 
            MathUtils.CreatePerspective(ref projSpan, camera.Fov, (float)width / (float)height, camera.Near, camera.Far, bgfx.get_caps()->homogeneousDepth);
            bgfx.set_view_transform(viewId, &view, proj);
        }
    }

    public static void SetNormalMatrix(in FrameData frameData, in Matrix4x4 modelMatrix)
    {
        // usually the normal matrix is based on the model view matrix
        // but shading is done in world space (not eye space) so it's just the model
        // matrix glm::mat4 modelViewMat = viewMat * modelMat;

        // if we don't do non-uniform scaling, the normal matrix is the same as the
        // model-view matrix (only the magnitude of the normal is changed, but we
        // normalize either way) glm::mat3 normalMat = glm::mat3(modelMat);

        // use adjugate instead of inverse
        // see
        // https://github.com/graphitemaster/normals_revisited#the-details-of-transforming-normals

        // cofactor is the transpose of the adjugate
        Matrix3x3 temp = modelMatrix.ToMatrix3x3();
        Matrix3x3 normal = Matrix3x3.Adjugate(temp);
        normal = Matrix3x3.Transpose(normal);

        unsafe
        {
            bgfx.set_uniform(frameData.NormalMatrixUniform, &normal, ushort.MaxValue);
        }
    }
}