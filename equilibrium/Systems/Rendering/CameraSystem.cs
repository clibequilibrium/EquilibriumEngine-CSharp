using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Equilibrium.Components;
using MathNet.Numerics;
using static SDL2.SDL;

namespace Equilibrium.Systems.Rendering;

public partial class CameraSystem : BaseSystem<World, float>, IInputSystem
{
    private const float MIN_FOV = 10.0f;
    private const float MAX_FOV = 90.0f;

    public CameraSystem(World world) : base(world)
    {

    }

    [Query]
    [All<BgfxComponent>, None<Camera>]
    private void InitializeCamera(in Entity entity)
    {
        Camera camera = new Camera { Fov = 73.7397953f, Near = 0.741915643f, Far = 37.0957832f, Position = new Vector3(-8, 3, 0) };
        camera.Up = Vector3.UnitY;

        Vector3 target = Vector3.UnitZ;

        // model rotation
        // maps vectors to camera space (x, y, z)
        Vector3 forward = Vector3.Normalize(target - camera.Position);

        camera.Rotation = MathUtils.CreateFromVectors(forward, Vector3.UnitZ);

        // correct the up vector
        // the cross product of non-orthogonal vectors is not normalized
        Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Up, forward));  // left-handed coordinate system
        Vector3 orthup = Vector3.Cross(forward, right);
        Vector3 temp = Vector3.Normalize(Vector3.Transform(orthup, camera.Rotation));

        Quaternion upRotation = MathUtils.CreateFromVectors(temp, camera.Up);

        camera.Rotation = upRotation * camera.Rotation;
        // inverse of the model rotation
        camera.InverseRotation = Quaternion.Conjugate(camera.Rotation);

        entity.Add<Camera>(camera);
    }
    [Query]
    [All<AppWindow, Camera, Input>]
    private void UpdateCamera(in Entity entity, in AppWindow appWindow, ref Camera camera, in Input input)
    {
        float deltaTime = Data;
        const float angularVelocity = 180.0f / 600.0f; // degrees/pixel

        // Zoom
        float scroll_y = input.Mouse.Scroll.Y;
        if (scroll_y != 0)
        {
            camera.Fov -= scroll_y * 2.0f;
            camera.Fov = Math.Clamp(camera.Fov, MIN_FOV, MAX_FOV);
        }

        // Preparing axis
        float velocity = input.Keys[Keys.KEY_SHIFT].State ? 20.0f : 10.0f;

        Vector3 forward = Vector3.Transform(Vector3.UnitZ, camera.InverseRotation);
        Vector3 right = Vector3.Transform(Vector3.UnitX, camera.InverseRotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, camera.InverseRotation);

        Vector2 mouse_delta = new(input.Mouse.Rel.Y, input.Mouse.Rel.X);

        if (input.Mouse.Right.State)
        {
            if (SDL_ShowCursor(SDL_QUERY) == SDL_ENABLE)
            {
                SDL_ShowCursor(SDL_DISABLE);
                SDL_SetRelativeMouseMode(SDL_bool.SDL_TRUE);
            }

            // Rotation
            mouse_delta = -mouse_delta;
            mouse_delta *= angularVelocity;

            mouse_delta[0] = (float)Trig.DegreeToRadian(mouse_delta[0]);
            mouse_delta[1] = (float)Trig.DegreeToRadian(mouse_delta[1]);

            float dot = Vector3.Dot(camera.Up, forward);

            // limit pitch
            if ((dot < -0.99f && mouse_delta[0] < 0.0f) || // angle nearing 180 degrees
                (dot > 0.99f && mouse_delta[0] > 0.0f))    // angle nearing 0 degrees
                mouse_delta[0] = 0.0f;

            // pitch is relative to current sideways rotation
            // yaw happens independently
            // this prevents roll
            Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouse_delta[0]);
            Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, mouse_delta[1]);
            Quaternion temp_pith_yaw_mul = pitch * camera.Rotation;

            camera.Rotation = temp_pith_yaw_mul * yaw;
            camera.InverseRotation = Quaternion.Conjugate(camera.Rotation);
        }
        else
        {
            if (SDL_ShowCursor(SDL_QUERY) == SDL_DISABLE)
            {
                SDL_SetRelativeMouseMode(SDL_bool.SDL_FALSE);
                SDL_WarpMouseGlobal(appWindow.Width / 2, appWindow.Height / 2);
                SDL_ShowCursor(SDL_ENABLE);
            }
        }

        // Translation
        if (input.Keys[Keys.KEY_W].State)
        {
            camera.Position += forward * velocity * deltaTime;
        }

        if (input.Keys[Keys.KEY_S].State)
        {
            camera.Position += -forward * velocity * deltaTime;
        }

        if (input.Keys[Keys.KEY_A].State)
        {
            camera.Position += -right * velocity * deltaTime;
        }

        if (input.Keys[Keys.KEY_D].State)
        {
            camera.Position += right * velocity * deltaTime;
        }

        if (input.Keys[Keys.KEY_SPACE].State)
        {
            camera.Position += up * velocity * deltaTime;
        }

        if (input.Keys[Keys.KEY_Q].State)
        {
            camera.Position += -up * velocity * deltaTime;
        }
    }
}