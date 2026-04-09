#if UNITY_EDITOR || BL_DEBUG
using BovineLabs.Core;
using BovineLabs.EntityLinks;
using BovineLabs.Quill;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct EntityLinkTimelineDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DrawSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<EntityLinkTimelineDebugSystem>();
            if (!drawer.IsEnabled) return;

            state.Dependency = new DrawActiveAttachmentsJob
            {
                Drawer = drawer,
                LtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct DrawActiveAttachmentsJob : IJobEntity
        {
            public Drawer Drawer;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;

            private void Execute(in EntityLinkAttachState state, in TrackBinding binding)
            {
                if (!state.WasSuccessfullyAttached || state.ResolvedTarget == Entity.Null) return;

                var attachedEntity = binding.Value;
                var targetSocket = state.ResolvedTarget;

                if (LtwLookup.TryGetComponent(attachedEntity, out var attachedLtw) &&
                    LtwLookup.TryGetComponent(targetSocket, out var targetLtw))
                {
                    var label = EntityLinkKeys.KeyToName(state.LinkKey);
                    var color = GetColorForKey(state.LinkKey);

                    DrawCurvedTether(attachedLtw.Position, targetLtw.Position, label, color);
                }
            }

            private static Color GetColorForKey(byte key)
            {
                var h = (key * 0.618033988749895f) % 1.0f;
                return Color.HSVToRGB(h, 0.8f, 0.9f);
            }

            private void DrawCurvedTether(float3 start, float3 end, FixedString64Bytes label, Color color)
            {
                var distance = math.distance(start, end);
                var mid = (start + end) * 0.5f;
                mid.y += (distance * 0.2f);

                const int segments = 16;
                var lines = new NativeList<float3>(segments * 2, Allocator.Temp);
                var prev = start;

                for (var i = 1; i <= segments; i++)
                {
                    var t = i / (float)segments;
                    var current = math.lerp(math.lerp(start, mid, t), math.lerp(mid, end, t), t);

                    lines.Add(prev);
                    lines.Add(current);
                    prev = current;
                }

                Drawer.Lines(lines.AsArray(), color);

                var dir = math.normalize(end - lines[lines.Length - 4]);
                Drawer.Arrow(end - (dir * 0.1f), dir * 0.25f, color);

                Drawer.Text64(mid + new float3(0f, 0.2f, 0f), label, color, 11f);
                lines.Dispose();
            }
        }
    }
}
#endif