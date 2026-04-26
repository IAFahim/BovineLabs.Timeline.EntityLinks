namespace BovineLabs.Timeline.EntityLinks.Data
{
    public readonly struct EntityLinkKey
    {
        public readonly ulong Value;

        public EntityLinkKey(ulong value)
        {
            this.Value = value;
        }

        public static implicit operator ulong(EntityLinkKey key)
        {
            return key.Value;
        }

        public static implicit operator EntityLinkKey(ulong value)
        {
            return new EntityLinkKey(value);
        }
    }
}
