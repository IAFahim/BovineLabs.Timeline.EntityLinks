using BovineLabs.Reaction.Data.Core;
using Bovinelabs.Timeline.EntityLinks.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Entity = Unity.Entities.Entity;

namespace Bovinelabs.Timeline.EntityLinks
{
    public struct Graph
    {
        [ReadOnly] public ComponentLookup<Parent> Parents;
        [ReadOnly] public ComponentLookup<Targets> Targets;
        [ReadOnly] public BufferLookup<EntityLookupStoreData> Stores;

        public void Evaluate(
            in Entity origin,
            in byte key,
            in ResolveRule rule,
            in byte accentLimit,
            out Entity result
        )
        {
            if (rule.HasAny(ResolveRule.SelfTarget))
            {
                var target = Targets[origin].Target;
                if (TryEvaluateNode(target, key, rule, ResolveRule.SelfTarget, out result)) return;
            }

            if (TryEvaluateAscent(origin, key, rule, ResolveRule.Parent, accentLimit, out result)) return;
            if (TryEvaluateContext(origin, key, rule, out result)) return;
            TryEvaluateParentContext(origin, key, rule, ResolveRule.ParentsTarget, out result);
        }

        private bool TryEvaluateNode(Entity node, byte key, ResolveRule rule, ResolveRule flag, out Entity result)
        {
            result = Entity.Null;

            if (!rule.HasAny(flag) || node == Entity.Null || !Stores.TryGetBuffer(node, out var buffer)) return false;

            for (var i = 0; i < buffer.Length; i++)
                if (buffer[i].Tag == key)
                {
                    result = buffer[i].Value;
                    return true;
                }

            return false;
        }

        private bool TryEvaluateAscent(
            in Entity node,
            in byte key,
            in ResolveRule rule,
            in ResolveRule flag,
            byte accentLimit,
            out Entity result
        )
        {
            result = Entity.Null;

            if (!rule.HasAny(flag)) return false;

            var current = node;
            while (Parents.TryGetComponent(current, out var parent) && accentLimit > 0)
            {
                current = parent.Value;
                if (TryEvaluateNode(current, key, rule, flag, out result)) return true;
                accentLimit--;
            }

            return false;
        }

        private bool TryEvaluateContext(
            in Entity node,
            in byte key,
            in ResolveRule rule,
            out Entity result
        )
        {
            result = Entity.Null;

            if (!Targets.TryGetComponent(node, out var context)) return false;

            if (TryEvaluateNode(context.Owner, key, rule, ResolveRule.Owner, out result)) return true;
            if (TryEvaluateNode(context.Source, key, rule, ResolveRule.Source, out result)) return true;
            return TryEvaluateNode(context.Target, key, rule, ResolveRule.Target, out result);
        }

        private bool TryEvaluateParentContext(Entity node, byte key, ResolveRule rule, ResolveRule flag,
            out Entity result)
        {
            result = Entity.Null;

            if (!rule.HasAny(flag) || !Parents.TryGetComponent(node, out var parent) ||
                !Targets.TryGetComponent(parent.Value, out var context)) return false;

            return TryEvaluateNode(context.Target, key, rule, flag, out result);
        }
    }
}