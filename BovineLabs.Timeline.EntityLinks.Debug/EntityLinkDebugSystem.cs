#if UNITY_EDITOR || BL_DEBUG
using System.Diagnostics.CodeAnalysis;
using BovineLabs.Core;
using BovineLabs.Core.ConfigVars;
using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Quill;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Core.Debug;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks.Debug
{
    [Configurable]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:Element parameters should be documented",
        Justification = "Using see cref")]
    public static class EntityLinkDebugSystemConfig
    {
        [ConfigVar("bovinelabs.entitylinkdebugsystem.draw-enabled", false,
            "Enable the Entity Link debug drawer in the editor.")]
        public static readonly SharedStatic<bool> Enabled =
            SharedStatic<bool>.GetOrCreate<Tags.Enabled>();

        private struct Tags
        {
            public struct Enabled
            {
            }
        }
    }

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

            if (!TimelineDebugUtility.TryGetDrawer<EntityLinkDebugSystem>(
                    ref state, EntityLinkDebugSystemConfig.Enabled.Data, out var drawer,
                    out var viewer, out var hasViewer))
                return;

            state.Dependency = new RenderTransition
            {
                Renderer = drawer,
                Viewer = viewer,
                HasViewer = hasViewer,
                WorldSpace = _worldSpaceLookup
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct RenderTransition : IJobEntity
        {
            public Drawer Renderer;
            public float3 Viewer;
            public bool HasViewer;
            [ReadOnly] public UnsafeComponentLookup<LocalToWorld> WorldSpace;

            private void Execute(Entity entity, in TrackBinding binding, in Targets targets)
            {
                if (!WorldSpace.TryGetComponent(binding.Value, out var bindingLtw)) return;

                var origin = bindingLtw.Position;
                var tier = TimelineDebugTier.Resolve(origin, Viewer, HasViewer);

                if (targets.Target != Entity.Null && WorldSpace.TryGetComponent(targets.Target, out var targetLtw))
                    RenderManifold(origin, targetLtw.Position, 0, tier);

                if (targets.Source != Entity.Null && WorldSpace.TryGetComponent(targets.Source, out var sourceLtw))
                    RenderManifold(origin, sourceLtw.Position, 1, tier);
            }

            private unsafe void RenderManifold(float3 origin, float3 destination, byte domain, DebugTier tier)
            {
                var tint = domain == 0 ? TimelineDebugColors.TargetLink : TimelineDebugColors.SourceLink;
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

                if (tier >= DebugTier.Mid)
                {
                    var label = domain == 0 ? (FixedString32Bytes)"Target" : (FixedString32Bytes)"Source";
                    Renderer.Text32(apex + new float3(0, 0.15f, 0), label, tint, 11f);
                }

                if (tier == DebugTier.Close)
                {
                    var readout = new FixedString128Bytes();
                    readout.Append(domain == 0 ? (FixedString32Bytes)"Target " : (FixedString32Bytes)"Source ");
                    readout.Append(span);
                    readout.Append((FixedString32Bytes)"m");
                    Renderer.Text128(destination + new float3(0, 0.25f, 0), readout, TimelineDebugColors.Label, 10f);
                }
            }
        }
    }
}
#endif