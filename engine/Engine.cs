using System.Diagnostics;
using Arch.Core;
using Arch.System;
using Engine.Utils.Extensions;

namespace Engine;

internal static class Engine
{
    internal static World World = World.Create();
    internal static Group<float> InputSystems = new Group<float>();
    internal static Group<float> GameSystems = new Group<float>();
    internal static Group<float> RenderSystems = new Group<float>();

    internal static volatile bool IsRunning = true;

    private static QueryDescription quitEventQuery = new QueryDescription().WithAll<QuitEvent>();
    private static global::JobScheduler.JobScheduler jobScheduler = new JobScheduler.JobScheduler("WorkerThreads");

    internal static void Initialize()
    {
        float dt = 0;

        InputSystems.Update(in dt);
        GameSystems.Update(in dt);
        RenderSystems.Update(in dt);
    }

    internal static void Start()
    {
        const double MaxFrameTime = 1.0 / 10.0; // Maximum frame time to prevent stalls
        const double TickRate = 60.0; // Game logic and physics update rate
        const double FrameRate = 240.0; // Rendering update rate

        float FixedDeltaTime = (float)(1.0d / TickRate);
        double FrameTimeAccumulator = 0.0;
        double GameTimeAccumulator = 0.0;
        double LastFrameTime = 0.0;
        double CurrentTime = 0.0;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        while (IsRunning)
        {
            ModuleManager.Update();

            if (World.CountEntities(in quitEventQuery) > 0)
            {
                IsRunning = false;
            }

            // Calculate delta time
            double DeltaTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            DeltaTime = Math.Min(DeltaTime, MaxFrameTime);
            LastFrameTime = CurrentTime;
            CurrentTime += DeltaTime;

            // Phase 1: Input processing
            float deltaTime = (float)DeltaTime;

            InputSystems?.BeforeUpdate(in deltaTime);
            InputSystems?.Update(in deltaTime);
            InputSystems?.AfterUpdate(in deltaTime);

            // Phase 2: Game logic and physics update
            GameTimeAccumulator += DeltaTime;
            while (GameTimeAccumulator >= FixedDeltaTime)
            {
                GameSystems?.BeforeUpdate(in FixedDeltaTime);
                GameSystems?.Update(in FixedDeltaTime);
                GameSystems?.AfterUpdate(in FixedDeltaTime);
                GameTimeAccumulator -= FixedDeltaTime;
            }

            // Phase 3: Physics simulation
            // PhysicsSystems?.SimulatePhysics(in FixedDeltaTime);

            // Phase 4: Animation update
            // AnimationSystems?.UpdateAnimations(in FixedDeltaTime);

            // Phase 5: Audio processing
            // AudioSystems?.Update(in FixedDeltaTime);

            // Phase 6: Rendering
            FrameTimeAccumulator += DeltaTime;
            while (FrameTimeAccumulator >= 1.0 / FrameRate)
            {
                RenderSystems?.BeforeUpdate(in FixedDeltaTime);
                RenderSystems?.Update(in FixedDeltaTime);
                RenderSystems?.AfterUpdate(in FixedDeltaTime);

                if (RenderSystems?.Count() > 0)
                {
                    Bgfx.bgfx.frame(false);
                }

                FrameTimeAccumulator -= 1.0 / FrameRate;
            }

            // Phase 7: Audio rendering
            // AudioSystems?.Render(in FixedDeltaTime);
        }
    }

    internal static void Shutdown()
    {
        InputSystems?.Dispose();
        GameSystems?.Dispose();
        RenderSystems?.Dispose();

        if (RenderSystems?.Count() > 0)
        {
            Bgfx.bgfx.shutdown();
        }

        jobScheduler.Dispose();
        World.Destroy(World);
    }
}