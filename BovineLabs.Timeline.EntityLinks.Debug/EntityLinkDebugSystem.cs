#if UNITY_EDITOR || BL_DEBUG
using BovineLabs.Core;
using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Quill;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation |
                       WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct EntityLinkDebugSystem : ISystem
    {
        private UnsafeComponentLookup<LocalToWorld> _worldSpaceLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _worldSpaceLookup = state.GetUnsafeComponentLookup<LocalToWorld>(true);
            state.RequireForUpdate<DrawSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _worldSpaceLookup.Update(ref state);
            var renderer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            state.Dependency = new RenderTransition
            {
                Renderer = renderer,
                WorldSpace = _worldSpaceLookup
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct RenderTransition : IJobEntity
        {
            public Drawer Renderer;
            [ReadOnly] public UnsafeComponentLookup<LocalToWorld> WorldSpace;

            private void Execute(Entity entity, in TrackBinding binding, in Targets targets)
            {
                if (!WorldSpace.TryGetComponent(binding.Value, out var bindingLtw)) return;

                var origin = bindingLtw.Position;

                if (targets.Target != Entity.Null && WorldSpace.TryGetComponent(targets.Target, out var targetLtw))
                    RenderManifold(origin, targetLtw.Position, 0);

                if (targets.Source != Entity.Null && WorldSpace.TryGetComponent(targets.Source, out var sourceLtw))
                    RenderManifold(origin, sourceLtw.Position, 1);
            }

            private unsafe void RenderManifold(float3 origin, float3 destination, byte domain)
            {
                var hue = domain * 0.618033988749895f % 1.0f;
                var tint = Color.HSVToRGB(hue, 0.8f, 0.9f);
                var span = math.distance(origin, destination);
                var apex = (origin + destination) * 0.5f;
                apex.y += span * 0.2f;

                const int resolution = 16;
                const int points = resolution * 2;
                var pathData = stackalloc float3[points];
                var path = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float3>(pathData, points,
                    Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref path, AtomicSafetyHandle.GetTempMemoryHandle());
#endif

                var pathLength = 0;
                var node = origin;

                for (var step = 1; step <= resolution; step++)
                {
                    var ratio = step / (float)resolution;
                    var vertex = math.lerp(math.lerp(origin, apex, ratio), math.lerp(apex, destination, ratio), ratio);

                    path[pathLength++] = node;
                    path[pathLength++] = vertex;
                    node = vertex;
                }

                Renderer.Lines(path.GetSubArray(0, pathLength), tint);
                Renderer.Point(destination, 0.05f, tint);
            }
        }
    }
}
#endif