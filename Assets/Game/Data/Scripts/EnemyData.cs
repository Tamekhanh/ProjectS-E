using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectS.Data
{
    [Serializable]
    public class WeaknessEntry
    {
        public HitAttribute attribute;
        /// <summary>Ví dụ 1.5 là yếu, 0.5 là kháng. Thuộc tính không có trong bảng thì là 1.</summary>
        public float multiplier = 1f;
    }

    /// <summary>Một chiêu trong bộ chiêu cố định của địch.</summary>
    [Serializable]
    public class EnemyMove
    {
        public string displayName;
        /// <summary>Cách địch diễn chiêu này trên sân. Mỗi chiêu chọn riêng, vì một con quái có thể vừa húc vừa phun.</summary>
        public AttackStyle attackStyle = AttackStyle.Lunge;
        public List<HitData> hits = new List<HitData>();
    }

    [CreateAssetMenu(menuName = "ProjectS/Kẻ địch", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public string displayName;
        [Min(1)] public int maxHp = 20;
        [Min(0)] public int attack;
        [Min(0)] public int defence;
        [Min(1)] public int speed = 4;
        public List<WeaknessEntry> weaknesses = new List<WeaknessEntry>();
        public List<EnemyMove> moves = new List<EnemyMove>();

        /// <summary>
        /// Hệ số sát thương theo thuộc tính. Heal, support và defence không bị ảnh hưởng nên luôn là 1.
        /// </summary>
        public float GetMultiplier(HitAttribute attribute)
        {
            if (!attribute.IsDamage()) return 1f;
            foreach (WeaknessEntry entry in weaknesses)
            {
                if (entry.attribute == attribute) return entry.multiplier;
            }
            return 1f;
        }
    }
}
