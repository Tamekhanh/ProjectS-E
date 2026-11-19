using System;
using System.Collections.Generic;
using ProjectS.Combat.Core;
using ProjectS.Data;
using UnityEngine;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Dựng một trận thử từ dữ liệu mẫu trong Resources/GameData.
    /// TẠM: đội hình, deck và nhóm địch được chọn cứng ở đây cho tới khi có màn chuẩn bị trước trận (bước 9).
    /// </summary>
    public static class BattleSetup
    {
        public const int CopiesPerCard = 2;
        public const int TeamSize = 3;

        public static BattleState Create(System.Random rng)
        {
            CharacterData[] characters = Load<CharacterData>("GameData/Characters");
            CardData[] cards = Load<CardData>("GameData/Cards");
            EnemyData[] enemyTypes = Load<EnemyData>("GameData/Enemies");

            if (characters.Length == 0 || cards.Length == 0 || enemyTypes.Length == 0)
            {
                Debug.LogError("[ProjectS] Thiếu dữ liệu mẫu. Chạy menu ProjectS > Tạo lại dữ liệu mẫu.");
                return null;
            }

            var players = new List<Unit>();
            for (int i = 0; i < characters.Length && i < TeamSize; i++)
            {
                var deck = new List<CardData>();
                foreach (CardData card in cards)
                {
                    for (int copy = 0; copy < CopiesPerCard; copy++) deck.Add(card);
                }
                players.Add(new Unit(characters[i], deck, rng));
            }

            var enemies = new List<Unit>();
            if (enemyTypes.Length == 1)
            {
                enemies.Add(new Unit(enemyTypes[0], enemyTypes[0].displayName + " A"));
                enemies.Add(new Unit(enemyTypes[0], enemyTypes[0].displayName + " B"));
            }
            else
            {
                enemies.Add(new Unit(enemyTypes[0], enemyTypes[0].displayName + " A"));
                enemies.Add(new Unit(enemyTypes[1]));
                enemies.Add(new Unit(enemyTypes[0], enemyTypes[0].displayName + " B"));
            }

            return new BattleState(players, enemies, rng);
        }

        private static T[] Load<T>(string path) where T : ScriptableObject
        {
            T[] items = Resources.LoadAll<T>(path);
            Array.Sort(items, (a, b) => string.CompareOrdinal(a.name, b.name));
            return items;
        }
    }
}
