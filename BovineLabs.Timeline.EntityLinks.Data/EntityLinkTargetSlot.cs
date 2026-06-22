using BovineLabs.Reaction.Data.Core;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public static class EntityLinkTargetSlot
    {
        public static bool TrySet(ref Targets targets, Target slot, Entity value)
        {
            switch (slot)
            {
                case Target.Owner:
                    targets.Owner = value;
                    return true;
                case Target.Source:
                    targets.Source = value;
                    return true;
                case Target.Target:
                    targets.Target = value;
                    return true;
                case Target.Custom:
                    targets.Custom = value;
                    return true;
                default:
                    return false;
            }
        }
    }
}
