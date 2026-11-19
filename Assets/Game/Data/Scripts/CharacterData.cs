using UnityEngine;

namespace ProjectS.Data
{
    [CreateAssetMenu(menuName = "ProjectS/Nhân vật", fileName = "Character_")]
    public class CharacterData : ScriptableObject
    {
        public string displayName;
        [Min(1)] public int maxHp = 30;
        [Min(0)] public int attack;
        [Min(0)] public int defence;
        [Min(1)] public int speed = 5;
        /// <summary>AP hồi đầy đầu mỗi turn. Passive có thể tăng thêm.</summary>
        [Min(0)] public int baseAp = 3;
        /// <summary>Cách nhân vật diễn đòn đánh trên sân, áp dụng cho mọi thẻ tấn công nhân vật này dùng.</summary>
        public AttackStyle attackStyle = AttackStyle.Lunge;
    }
}
