using System;
using System.Collections.Generic;
using ProjectS.Data;

namespace ProjectS.Combat.Core
{
    /// <summary>Ý định của một kẻ địch cho turn sắp tới, hiện trước cho người chơi.</summary>
    public class EnemyIntent
    {
        public EnemyMove Move;
        /// <summary>Mục tiêu chính của chiêu. Null nếu chiêu không nhắm vào ai.</summary>
        public Unit Target;
    }

    /// <summary>Một nhân vật hoặc kẻ địch trong trận.</summary>
    public class Unit
    {
        public string Name { get; }
        public bool IsPlayer { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int Shield { get; private set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Speed { get; set; }
        public int MaxAp { get; set; }
        public int Ap { get; set; }

        /// <summary>Deck của nhân vật. Null với kẻ địch.</summary>
        public Deck Deck { get; }
        /// <summary>Dữ liệu gốc của kẻ địch. Null với nhân vật.</summary>
        public EnemyData EnemyData { get; }
        public CharacterData CharacterData { get; }

        public EnemyIntent Intent { get; set; }
        /// <summary>Chỉ số chiêu địch dùng lần gần nhất, để không lặp một chiêu liên tục.</summary>
        public int LastMoveIndex { get; set; } = -1;

        public bool IsAlive => Hp > 0;

        public Unit(CharacterData data, IEnumerable<CardData> deckCards, Random rng)
        {
            CharacterData = data ?? throw new ArgumentNullException(nameof(data));
            Name = data.displayName;
            IsPlayer = true;
            MaxHp = data.maxHp;
            Hp = data.maxHp;
            Attack = data.attack;
            Defence = data.defence;
            Speed = data.speed;
            MaxAp = data.baseAp;
            Deck = new Deck(deckCards, rng);
        }

        /// <param name="name">Tên hiển thị riêng, dùng khi một trận có nhiều kẻ địch cùng loại.</param>
        public Unit(EnemyData data, string name = null)
        {
            EnemyData = data ?? throw new ArgumentNullException(nameof(data));
            Name = string.IsNullOrEmpty(name) ? data.displayName : name;
            IsPlayer = false;
            MaxHp = data.maxHp;
            Hp = data.maxHp;
            Attack = data.attack;
            Defence = data.defence;
            Speed = data.speed;
        }

        /// <summary>Hệ số điểm yếu. Nhân vật của người chơi hiện chưa có điểm yếu nên luôn là 1.</summary>
        public float GetMultiplier(HitAttribute attribute)
        {
            return EnemyData != null ? EnemyData.GetMultiplier(attribute) : 1f;
        }

        /// <summary>Shield chặn trước, phần còn lại trừ vào HP. Trả về lượng HP thực mất.</summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int absorbed = Math.Min(Shield, amount);
            Shield -= absorbed;
            int hpLoss = Math.Min(Hp, amount - absorbed);
            Hp -= hpLoss;
            return hpLoss;
        }

        /// <summary>Hồi máu, không vượt quá HP tối đa. Trả về lượng HP thực hồi.</summary>
        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int healed = Math.Min(amount, MaxHp - Hp);
            Hp += healed;
            return healed;
        }

        public void AddShield(int amount)
        {
            if (amount > 0 && IsAlive) Shield += amount;
        }

        public void ClearShield()
        {
            Shield = 0;
        }
    }
}
