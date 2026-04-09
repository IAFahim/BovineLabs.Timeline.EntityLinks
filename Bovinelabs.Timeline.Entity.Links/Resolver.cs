using BovineLabs.Reaction.Data.Core;
using Bovinelabs.Timeline.Entity.Links.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Bovinelabs.Timeline.Entity.Links
{
    public struct Graph
    {
        [ReadOnly] public ComponentLookup<Parent> Parents;
        [ReadOnly] public ComponentLookup<Targets> Targets;
        [ReadOnly] public BufferLookup<EntityLookupStoreData> Stores;

        public Unity.Entities.Entity Evaluate(Unity.Entities.Entity origin, byte key, ResolveRule rule)
        {
            var target = Unity.Entities.Entity.Null;
            target = Select(target, EvaluateNode(origin, key, rule, ResolveRule.SelfTarget));
            target = Select(target, EvaluateAscent(origin, key, rule, ResolveRule.Parent));
            target = Select(target, EvaluateContext(origin, key, rule));
            target = Select(target, EvaluateParentContext(origin, key, rule, ResolveRule.ParentsTarget));
            return target;
        }

        private Unity.Entities.Entity EvaluateNode(Unity.Entities.Entity node, byte key, ResolveRule rule, ResolveRule flag)
        {
            if (!rule.HasAny(flag) || node == Unity.Entities.Entity.Null || !this.Stores.TryGetBuffer(node, out var buffer))
            {
                return Unity.Entities.Entity.Null;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Tag == key)
                {
                    return buffer[i].Value;
                }
            }

            return Unity.Entities.Entity.Null;
        }

        private Unity.Entities.Entity EvaluateAscent(Unity.Entities.Entity node, byte key, ResolveRule rule, ResolveRule flag)
        {
            if (!rule.HasAny(flag))
            {
                return Unity.Entities.Entity.Null;
            }

            var limit = 64;
            var current = node;

            while (this.Parents.TryGetComponent(current, out var parent) && limit > 0)
            {
                current = parent.Value;
                var match = EvaluateNode(current, key, rule, flag);
                if (match != Unity.Entities.Entity.Null)
                {
                    return match;
                }

                limit--;
            }

            return Unity.Entities.Entity.Null;
        }

        private Unity.Entities.Entity EvaluateContext(Unity.Entities.Entity node, byte key, ResolveRule rule)
        {
            if (!this.Targets.TryGetComponent(node, out var context))
            {
                return Unity.Entities.Entity.Null;
            }

            var target = Unity.Entities.Entity.Null;
            target = Select(target, EvaluateNode(context.Owner, key, rule, ResolveRule.Owner));
            target = Select(target, EvaluateNode(context.Source, key, rule, ResolveRule.Source));
            target = Select(target, EvaluateNode(context.Target, key, rule, ResolveRule.Target));
            return target;
        }

        private Unity.Entities.Entity EvaluateParentContext(Unity.Entities.Entity node, byte key, ResolveRule rule, ResolveRule flag)
        {
            if (!rule.HasAny(flag) || !this.Parents.TryGetComponent(node, out var parent) || !this.Targets.TryGetComponent(parent.Value, out var context))
            {
                return Unity.Entities.Entity.Null;
            }

            return EvaluateNode(context.Target, key, rule, flag);
        }

        private static Unity.Entities.Entity Select(Unity.Entities.Entity primary, Unity.Entities.Entity fallback)
        {
            return primary != Unity.Entities.Entity.Null ? primary : fallback;
        }
    }
}
