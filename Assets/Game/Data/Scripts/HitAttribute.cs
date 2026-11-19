namespace ProjectS.Data
{
    /// <summary>Thuộc tính của một hit. Mỗi thuộc tính có icon và animation riêng.</summary>
    public enum HitAttribute
    {
        Slash,
        Blunt,
        Pierce,
        Magic,
        Heal,
        Support,
        Defence
    }

    public static class HitAttributeExtensions
    {
        /// <summary>Slash, blunt, pierce và magic gây sát thương và chịu hệ số điểm yếu.</summary>
        public static bool IsDamage(this HitAttribute attribute)
        {
            return attribute == HitAttribute.Slash
                || attribute == HitAttribute.Blunt
                || attribute == HitAttribute.Pierce
                || attribute == HitAttribute.Magic;
        }
    }
}
