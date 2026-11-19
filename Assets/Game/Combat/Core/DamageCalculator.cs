using System;
using ProjectS.Data;

namespace ProjectS.Combat.Core
{
    /// <summary>
    /// Công thức sát thương. Đây là công thức TẠM, đổi ở đây là đổi cho toàn bộ game:
    /// (giá trị hit + attack của người đánh - defence của người nhận) x hệ số điểm yếu, tối thiểu 1.
    /// </summary>
    public static class DamageCalculator
    {
        public const int MinDamage = 1;

        public static int Calculate(HitData hit, Unit attacker, Unit target)
        {
            int raw = hit.value + attacker.Attack - target.Defence;
            double scaled = raw * target.GetMultiplier(hit.attribute);
            int rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            return Math.Max(MinDamage, rounded);
        }
    }
}
