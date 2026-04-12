using BovineLabs.Core.Iterators;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    /// <summary>
    /// Burst-compatible, allocation-free graph traversal for Entity Links.
    /// </summary>
    public struct Graph
    {
        [ReadOnly] public UnsafeComponentLookup<Parent> Parents;
        [ReadOnly] public UnsafeComponentLookup<Targets> Targets;
        [ReadOnly] public UnsafeBufferLookup<EntityLookupStoreData> Stores;

        public void Evaluate(
            in Entity origin,
            in byte key,
            in ResolveRule rule,
            in byte ascentLimit,
            out Entity result)
        {
            if (rule.HasAny(ResolveRule.SelfTarget))
            {
                if (this.Targets.TryGetComponent(origin, out var target) && 
                    this.TryEvaluateNode(target.Target, key, rule, ResolveRule.SelfTarget, out result)) return;
            }

            if (this.TryEvaluateAscent(origin, key, rule, ResolveRule.Parent, ascentLimit, out result)) return;
            if (this.TryEvaluateContext(origin, key, rule, out result)) return;
            this.TryEvaluateParentContext(origin, key, rule, ResolveRule.ParentsTarget, out result);
        }

        private bool TryEvaluateNode(Entity node, byte key, ResolveRule rule, ResolveRule flag, out Entity result)
        {
            result = Entity.Null;

            if (!rule.HasAny(flag) || node == Entity.Null || !this.Stores.TryGetBuffer(node, out var buffer)) 
                return false;

            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Tag == key)
                {
                    result = buffer[i].Value;
                    return true;
                }
            }

            return false;
        }

        private bool TryEvaluateAscent(Entity node, byte key, ResolveRule rule, ResolveRule flag, byte ascentLimit, out Entity result)
        {
            result = Entity.Null;

            if (!rule.HasAny(flag)) return false;

            var current = node;
            while (this.Parents.TryGetComponent(current, out var parent) && ascentLimit > 0)
            {
                current = parent.Value;
                if (this.TryEvaluateNode(current, key, rule, flag, out result)) return true;
                ascentLimit--;
            }

            return false;
        }

        private bool TryEvaluateContext(Entity node, byte key, ResolveRule rule, out Entity result)
        {
            result = Entity.Null;

            if (!this.Targets.TryGetComponent(node, out var context)) return false;

            if (this.TryEvaluateNode(context.Owner, key, rule, ResolveRule.Owner, out result)) return true;
            if (this.TryEvaluateNode(context.Source, key, rule, ResolveRule.Source, out result)) return true;
            return this.TryEvaluateNode(context.Target, key, rule, ResolveRule.Target, out result);
        }

        private bool TryEvaluateParentContext(Entity node, byte key, ResolveRule rule, ResolveRule flag, out Entity result)
        {
            result = Entity.Null;

            if (!rule.HasAny(flag) || 
                !this.Parents.TryGetComponent(node, out var parent) ||
                !this.Targets.TryGetComponent(parent.Value, out var context)) 
                return false;

            return this.TryEvaluateNode(context.Target, key, rule, flag, out result);
        }
    }
}