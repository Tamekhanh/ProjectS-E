using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectS.Data.Editor
{
    /// <summary>
    /// Dữ liệu mẫu cho bản thử combat. Mọi tên và con số ở đây là TẠM, chỉ để có thứ mà chạy thử.
    /// Sau khi tạo, hãy chỉnh trực tiếp các file .asset trong Assets/Game/Data/Resources/GameData.
    /// </summary>
    public static class SampleDataBuilder
    {
        public const string RootFolder = "Assets/Game/Data/Resources/GameData";

        /// <summary>
        /// Phiên bản dữ liệu mẫu. Tăng số này mỗi khi thêm trường hiển thị mới vào dữ liệu mẫu
        /// (kiểu ra đòn, hiệu ứng...) hoặc thêm thẻ, nhân vật, địch mới, để máy người làm tự cập nhật.
        /// </summary>
        private const int SampleDataVersion = 3;
        private const string VersionPrefKey = "ProjectS.SampleDataVersion";

        public static List<CharacterData> BuildCharacters()
        {
            return new List<CharacterData>
            {
                Character("Linh_kiem", "Lính kiếm", maxHp: 32, attack: 3, defence: 2, speed: 6),
                Character("Phap_su", "Pháp sư", maxHp: 24, attack: 4, defence: 1, speed: 5, attackStyle: AttackStyle.Ranged),
                Character("Ho_ve", "Hộ vệ", maxHp: 40, attack: 2, defence: 4, speed: 3),
            };
        }

        public static List<CardData> BuildCards()
        {
            return new List<CardData>
            {
                Card("Chem", "Chém", Rate.Bet, 1, "Một nhát chém.",
                    Hit(HitAttribute.Slash, 6)),
                Card("Chem_doi", "Chém đôi", Rate.Gimel, 2, "Chém hai nhát liên tiếp.",
                    Hit(HitAttribute.Slash, 4), Hit(HitAttribute.Slash, 4)),
                Card("Rach", "Rạch", Rate.Bet, 1, "Chém nhẹ và gây bleed.",
                    Hit(HitAttribute.Slash, 3, status: StatusType.Bleed, statusAmount: 2)),
                // Thẻ tầm gần: ai dùng cũng phải lao lên, kể cả nhân vật tầm xa.
                Forced(AttackStyle.Lunge, Card("Dap", "Đập", Rate.Bet, 1, "Một cú đập mạnh. Phải áp sát mới đánh được.",
                    Hit(HitAttribute.Blunt, 7))),
                Card("Nen_khien", "Nện khiên", Rate.Gimel, 2, "Đập địch rồi tự dựng shield.",
                    Hit(HitAttribute.Blunt, 4), Hit(HitAttribute.Defence, 4, HitTarget.Self)),
                // Thẻ đâm: hit pierce tự lướt xuyên qua mục tiêu khi người dùng áp sát, không cần ép kiểu.
                Card("Dam", "Đâm", Rate.Bet, 1, "Một cú đâm xuyên qua mục tiêu.",
                    Hit(HitAttribute.Pierce, 6)),
                Card("Xuyen_giap", "Xuyên giáp", Rate.Dalet, 2, "Cú đâm dồn toàn lực, xuyên qua mục tiêu.",
                    Hit(HitAttribute.Pierce, 12)),
                // Thẻ trộn thuộc tính: chém trước mặt, đâm xuyên ra sau lưng, rồi quay lại chém.
                Card("Lien_hoan", "Liên hoàn kiếm", Rate.Dalet, 2, "Chém, đâm xuyên qua, rồi quay lại chém.",
                    Hit(HitAttribute.Slash, 3), Hit(HitAttribute.Pierce, 4), Hit(HitAttribute.Slash, 3)),
                // Thẻ tầm xa: ai dùng cũng đứng yên ném cầu lửa, kể cả nhân vật cận chiến.
                Forced(AttackStyle.Ranged, Card("Cau_lua", "Cầu lửa", Rate.Gimel, 2, "Phép lửa ném từ xa, gây burn.",
                    Hit(HitAttribute.Magic, 5, status: StatusType.Burn, statusAmount: 3))),
                // Phép giáng thẳng xuống mục tiêu: ai dùng cũng đứng yên gọi sét, không có đạn bay.
                Lightning(Forced(AttackStyle.Ranged, Card("Set_danh", "Sét đánh", Rate.Gimel, 2, "Gọi sét giáng xuống một kẻ địch.",
                    Hit(HitAttribute.Magic, 8)))),
                Card("Bang_bo", "Băng bó", Rate.Bet, 1, "Hồi máu cho một đồng đội.",
                    Hit(HitAttribute.Heal, 6)),
                Card("Dung_khien", "Dựng khiên", Rate.Bet, 1, "Tạo shield cho một đồng đội.",
                    Hit(HitAttribute.Defence, 8)),
            };
        }

        public static List<EnemyData> BuildEnemies()
        {
            EnemyData skeleton = Enemy("Bo_xuong", "Bộ xương", maxHp: 22, attack: 2, defence: 1, speed: 4);
            skeleton.weaknesses.Add(Weakness(HitAttribute.Blunt, 1.5f));
            skeleton.weaknesses.Add(Weakness(HitAttribute.Slash, 0.5f));
            skeleton.weaknesses.Add(Weakness(HitAttribute.Pierce, 0.5f));
            skeleton.moves.Add(Move("Chém xương", AttackStyle.InPlace, Hit(HitAttribute.Slash, 5)));
            skeleton.moves.Add(Move("Thủ thế", Hit(HitAttribute.Defence, 5, HitTarget.Self)));

            EnemyData slime = Enemy("Nhay", "Nhầy", maxHp: 28, attack: 1, defence: 0, speed: 2);
            slime.weaknesses.Add(Weakness(HitAttribute.Magic, 1.5f));
            slime.weaknesses.Add(Weakness(HitAttribute.Pierce, 1.5f));
            slime.weaknesses.Add(Weakness(HitAttribute.Blunt, 0.5f));
            slime.moves.Add(Move("Húc", Hit(HitAttribute.Blunt, 4)));
            slime.moves.Add(Move("Phun axit", AttackStyle.Ranged,
                Hit(HitAttribute.Magic, 3, status: StatusType.Burn, statusAmount: 2)));

            return new List<EnemyData> { skeleton, slime };
        }

        [MenuItem("ProjectS/Tạo lại dữ liệu mẫu")]
        public static void SaveAllFromMenu()
        {
            bool overwrite = !AssetDatabase.IsValidFolder(RootFolder) || EditorUtility.DisplayDialog(
                "Tạo lại dữ liệu mẫu",
                "Các file dữ liệu mẫu trùng tên sẽ bị ghi đè, mọi chỉnh sửa của bạn trên chúng sẽ mất. Tiếp tục?",
                "Ghi đè", "Hủy");
            if (overwrite)
            {
                SaveAll();
                EditorPrefs.SetInt(VersionPrefKey, SampleDataVersion);
            }
        }

        /// <summary>
        /// Tự tạo dữ liệu mẫu một lần khi thư mục dữ liệu chưa tồn tại.
        /// Nếu đã có dữ liệu cũ thì chỉ cập nhật phần hiển thị và thêm mục còn thiếu, giữ nguyên chỉ số người làm đã chỉnh.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CreateOnFirstLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.IsValidFolder(RootFolder))
                {
                    SaveAll();
                    EditorPrefs.SetInt(VersionPrefKey, SampleDataVersion);
                }
                else if (EditorPrefs.GetInt(VersionPrefKey, 1) < SampleDataVersion)
                {
                    UpgradeExisting();
                }
            };
        }

        /// <summary>
        /// Cập nhật dữ liệu mẫu cũ lên phiên bản hiện tại mà không ghi đè cả file:
        /// thêm thẻ, nhân vật, địch còn thiếu, và chép các trường chỉ dùng cho hiển thị
        /// (kiểu ra đòn, ép kiểu của thẻ, hiệu ứng trúng đòn) từ bản mẫu sang. HP, sát thương, AP và các chỉ số khác giữ nguyên.
        /// </summary>
        public static void UpgradeExisting()
        {
            int changed = 0;
            changed += Sync(BuildCharacters(), "Characters", "Character_",
                (sample, data) => data.attackStyle = sample.attackStyle);
            changed += Sync(BuildCards(), "Cards", "Card_", (sample, data) =>
            {
                data.forcesAttackStyle = sample.forcesAttackStyle;
                data.forcedAttackStyle = sample.forcedAttackStyle;
                data.impactEffect = sample.impactEffect;
            });
            changed += Sync(BuildEnemies(), "Enemies", "Enemy_", (sample, data) =>
            {
                // Chiêu được ghép theo tên hiển thị, để chiêu người làm tự thêm không bị đụng tới.
                foreach (EnemyMove move in data.moves)
                {
                    EnemyMove match = sample.moves.Find(m => m.displayName == move.displayName);
                    if (match != null) move.attackStyle = match.attackStyle;
                }
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetInt(VersionPrefKey, SampleDataVersion);
            Debug.Log("[ProjectS] Đã cập nhật dữ liệu mẫu lên phiên bản " + SampleDataVersion + " (" + changed + " file).");
        }

        /// <summary>Thêm mục còn thiếu và chép phần hiển thị sang mục đã có. Trả về số file đã đụng tới.</summary>
        private static int Sync<T>(List<T> samples, string subFolder, string prefix, Action<T, T> copyVisuals) where T : ScriptableObject
        {
            string folder = RootFolder + "/" + subFolder;
            Directory.CreateDirectory(folder);
            int changed = 0;
            foreach (T sample in samples)
            {
                string path = folder + "/" + prefix + sample.name + ".asset";
                T existing = AssetDatabase.LoadAssetAtPath<T>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(sample, path);
                    Debug.Log("[ProjectS] Đã thêm dữ liệu mẫu mới " + path);
                }
                else
                {
                    copyVisuals(sample, existing);
                    EditorUtility.SetDirty(existing);
                    UnityEngine.Object.DestroyImmediate(sample);
                }
                changed++;
            }
            return changed;
        }

        public static void SaveAll()
        {
            Save(BuildCharacters(), "Characters", "Character_");
            Save(BuildCards(), "Cards", "Card_");
            Save(BuildEnemies(), "Enemies", "Enemy_");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectS] Đã tạo dữ liệu mẫu trong " + RootFolder);
        }

        private static void Save<T>(List<T> items, string subFolder, string prefix) where T : ScriptableObject
        {
            string folder = RootFolder + "/" + subFolder;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            foreach (T item in items)
            {
                string path = folder + "/" + prefix + item.name + ".asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(item, path);
            }
        }

        private static CharacterData Character(string id, string displayName, int maxHp, int attack, int defence, int speed,
            AttackStyle attackStyle = AttackStyle.Lunge)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.name = id;
            data.displayName = displayName;
            data.maxHp = maxHp;
            data.attack = attack;
            data.defence = defence;
            data.speed = speed;
            data.attackStyle = attackStyle;
            return data;
        }

        private static CardData Card(string id, string displayName, Rate rate, int apCost, string description, params HitData[] hits)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.name = id;
            data.displayName = displayName;
            data.rate = rate;
            data.apCost = apCost;
            data.description = description;
            data.hits = new List<HitData>(hits);
            return data;
        }

        /// <summary>Đánh dấu thẻ đòi một kiểu ra đòn nhất định, bất kể nhân vật nào dùng.</summary>
        private static CardData Forced(AttackStyle style, CardData card)
        {
            card.forcesAttackStyle = true;
            card.forcedAttackStyle = style;
            return card;
        }

        /// <summary>Đánh dấu thẻ tầm xa dùng hiệu ứng sét giáng xuống mục tiêu thay cho đạn bay.</summary>
        private static CardData Lightning(CardData card)
        {
            card.impactEffect = ImpactEffect.Lightning;
            return card;
        }

        private static EnemyData Enemy(string id, string displayName, int maxHp, int attack, int defence, int speed)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.name = id;
            data.displayName = displayName;
            data.maxHp = maxHp;
            data.attack = attack;
            data.defence = defence;
            data.speed = speed;
            return data;
        }

        private static HitData Hit(HitAttribute attribute, int value, HitTarget target = HitTarget.MainTarget,
            StatusType status = StatusType.None, int statusAmount = 0)
        {
            return new HitData
            {
                attribute = attribute,
                value = value,
                target = target,
                status = status,
                statusAmount = statusAmount
            };
        }

        private static WeaknessEntry Weakness(HitAttribute attribute, float multiplier)
        {
            return new WeaknessEntry { attribute = attribute, multiplier = multiplier };
        }

        private static EnemyMove Move(string displayName, params HitData[] hits)
        {
            return Move(displayName, AttackStyle.Lunge, hits);
        }

        private static EnemyMove Move(string displayName, AttackStyle attackStyle, params HitData[] hits)
        {
            return new EnemyMove { displayName = displayName, attackStyle = attackStyle, hits = new List<HitData>(hits) };
        }
    }
}
