#if UNITY_EDITOR || BL_DEBUG
using BovineLabs.Core;
using BovineLabs.Quill;
using BovineLabs.Timeline.Data;
using Bovinelabs.Timeline.Entity.Links.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Bovinelabs.Timeline.Entity.Links.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct EntityLinkDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DrawSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var renderer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<EntityLinkDebugSystem>();
            if (!renderer.IsEnabled)
            {
                return;
            }

            state.Dependency = new RenderTransition
            {
                Renderer = renderer,
                WorldSpace = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct RenderTransition : IJobEntity
        {
            public Drawer Renderer;
            [ReadOnly] public ComponentLookup<LocalToWorld> WorldSpace;

            private void Execute(in EntityLinkAttachState state, in EntityLinkAttachConfig config, in TrackBinding binding)
            {
                if (!state.IsAttached || state.ResolvedTarget == Unity.Entities.Entity.Null)
                {
                    return;
                }

                if (this.WorldSpace.TryGetComponent(binding.Value, out var origin) && this.WorldSpace.TryGetComponent(state.ResolvedTarget, out var destination))
                {
                    this.RenderManifold(origin.Position, destination.Position, config.LinkKey);
                }
            }

            private void RenderManifold(float3 origin, float3 destination, byte domain)
            {
                var hue = (domain * 0.618033988749895f) % 1.0f;
                var tint = Color.HSVToRGB(hue, 0.8f, 0.9f);
                var span = math.distance(origin, destination);
                var apex = (origin + destination) * 0.5f;
                apex.y += span * 0.2f;

                var resolution = 16;
                var path = new NativeList<float3>(resolution * 2, Allocator.Temp);
                var node = origin;

                for (var step = 1; step <= resolution; step++)
                {
                    var ratio = step / (float)resolution;
                    var vertex = math.lerp(math.lerp(origin, apex, ratio), math.lerp(apex, destination, ratio), ratio);

                    path.Add(node);
                    path.Add(vertex);
                    node = vertex;
                }

                this.Renderer.Lines(path.AsArray(), tint);
                this.Renderer.Point(destination, 0.05f, tint);

                path.Dispose();
            }
        }
    }
}
#endif
