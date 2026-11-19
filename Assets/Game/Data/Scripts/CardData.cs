using System.Collections.Generic;
using UnityEngine;

namespace ProjectS.Data
{
    [CreateAssetMenu(menuName = "ProjectS/Thẻ", fileName = "Card_")]
    public class CardData : ScriptableObject
    {
        public const int MinApCost = 0;
        public const int MaxApCost = 3;

        public string displayName;
        public Rate rate;
        [Range(MinApCost, MaxApCost)]
        public int apCost = 1;
        [TextArea]
        public string description;
        public List<HitData> hits = new List<HitData>();

        /// <summary>
        /// Bật khi thẻ đòi một kiểu ra đòn nhất định, bất kể ai dùng. Ví dụ phép tầm gần buộc cả pháp sư phải lao lên.
        /// Tắt thì thẻ diễn theo kiểu ra đòn của nhân vật đang dùng nó.
        /// </summary>
        public bool forcesAttackStyle;
        public AttackStyle forcedAttackStyle = AttackStyle.Lunge;

        /// <summary>Khi thẻ được diễn theo kiểu tầm xa: đòn tới mục tiêu bằng đạn bay hay bằng hiệu ứng giáng thẳng xuống.</summary>
        public ImpactEffect impactEffect = ImpactEffect.Projectile;

        /// <summary>Kiểu ra đòn khi nhân vật có kiểu mặc định <paramref name="characterStyle"/> dùng thẻ này.</summary>
        public AttackStyle ResolveAttackStyle(AttackStyle characterStyle)
        {
            return forcesAttackStyle ? forcedAttackStyle : characterStyle;
        }

        /// <summary>Số bản trùng tối đa trong một deck, theo rate.</summary>
        public int MaxCopies => RateRules.MaxCopies(rate);

        /// <summary>Thẻ có hit nhắm mục tiêu chính thì người chơi phải kéo vào một mục tiêu.</summary>
        public bool NeedsTarget
        {
            get
            {
                foreach (HitData hit in hits)
                {
                    if (hit.target == HitTarget.MainTarget) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Thẻ được kéo vào đồng đội khi hit nhắm mục tiêu chính đầu tiên là heal, support hoặc defence.
        /// </summary>
        public bool TargetsAlly
        {
            get
            {
                foreach (HitData hit in hits)
                {
                    if (hit.target == HitTarget.MainTarget) return !hit.attribute.IsDamage();
                }
                return false;
            }
        }
    }
}
