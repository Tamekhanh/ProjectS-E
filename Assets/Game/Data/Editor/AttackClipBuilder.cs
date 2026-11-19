using System.IO;
using ProjectS.Combat.View;
using UnityEditor;
using UnityEngine;

namespace ProjectS.Data.Editor
{
    /// <summary>
    /// Tạo các clip animation ra đòn mẫu từ ảnh trong thư mục Portraits.
    /// Clip chỉ được tạo khi chưa có, nên sau khi tạo bạn cứ mở bằng cửa sổ Animation mà chỉnh: sẽ không bị ghi đè.
    /// Muốn tạo lại từ đầu thì dùng menu ProjectS > Tạo lại clip ra đòn mẫu.
    ///
    /// Mỗi clip diễn MỘT nhát đánh: ảnh lấy đà, rồi tới sự kiện Strike thì đổi sang ảnh ra đòn.
    /// Thẻ nhiều hit sẽ phát clip nhiều lần. Việc lao tới mục tiêu do CombatScreen lo, không nằm trong clip.
    /// Tên clip: tên file dữ liệu của unit + "_" + khóa. Khóa là thuộc tính của hit cho đòn cận chiến
    /// (slash, pierce, blunt...) hoặc "ranged" cho đòn tầm xa. Ví dụ Character_Linh_kiem_pierce, Character_Phap_su_ranged.
    /// Mỗi hit của một thẻ tìm clip theo thuộc tính của chính nó, nên thẻ slash, pierce, slash sẽ chém, đâm, rồi chém.
    /// </summary>
    public static class AttackClipBuilder
    {
        public const string PortraitFolder = SampleDataBuilder.RootFolder + "/Portraits";
        public const string AnimationFolder = SampleDataBuilder.RootFolder + "/Animations";
        public const string CardArtFolder = SampleDataBuilder.RootFolder + "/CardArt";

        // Tên đối tượng con mang ảnh của unit, do CombatScreen đặt.
        private const string BodyPath = "Body";
        private const float FrameRate = 60f;

        /// <summary>Tự tạo các clip còn thiếu mỗi khi Unity nạp lại code. Clip đã có thì để nguyên.</summary>
        [InitializeOnLoadMethod]
        private static void CreateMissingOnLoad()
        {
            EditorApplication.delayCall += () => BuildAll(false);
        }

        [MenuItem("ProjectS/Tạo lại clip ra đòn mẫu")]
        public static void RebuildFromMenu()
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Tạo lại clip ra đòn mẫu",
                "Các clip ra đòn mẫu sẽ bị ghi đè, mọi chỉnh sửa của bạn trên chúng sẽ mất. Tiếp tục?",
                "Ghi đè", "Hủy");
            if (overwrite) BuildAll(true);
        }

        public static void BuildAll(bool overwrite)
        {
            NormalizePortraitImports();

            // Lính kiếm, hit pierce (chùng gối lấy đà rồi đâm xuyên qua mục tiêu): lấy đà 0,22 giây rồi đâm, giữ tư thế đâm tới hết 0,5 giây.
            // Hai tỉ lệ chiều cao là ước lượng bằng mắt: người chùng gối thấp hơn đứng, người lao dài còn thấp hơn nữa.
            BuildStrikeClip("Character_Linh_kiem", "pierce",
                windUpSuffix: "_pierce_ready", windUpHeightRatio: 0.88f,
                strikeSuffix: "_pierce", strikeHeightRatio: 0.76f,
                strikeTime: 0.22f, length: 0.5f, overwrite: overwrite);

            // Lính kiếm, hit slash: vặn người kéo kiếm ra sau, rồi chém ngang bằng ảnh _attack.
            BuildStrikeClip("Character_Linh_kiem", "slash",
                windUpSuffix: "_slash_ready", windUpHeightRatio: 0.9f,
                strikeSuffix: "_attack", strikeHeightRatio: 0.95f,
                strikeTime: 0.2f, length: 0.5f, overwrite: overwrite);

            // Pháp sư, kiểu tầm xa: kéo gậy ra sau tụ phép, rồi chĩa gậy tới phóng phép. Đạn bay ra đúng lúc Strike.
            BuildStrikeClip("Character_Phap_su", "ranged",
                windUpSuffix: "_cast_ready", windUpHeightRatio: 0.95f,
                strikeSuffix: "_cast", strikeHeightRatio: 0.95f,
                strikeTime: 0.25f, length: 0.55f, overwrite: overwrite);

            // Hộ vệ, hit blunt: giơ chùy qua đầu (cao hơn lúc đứng), rồi bổ xuống trong tư thế chúi người.
            BuildStrikeClip("Character_Ho_ve", "blunt",
                windUpSuffix: "_blunt_ready", windUpHeightRatio: 1.05f,
                strikeSuffix: "_blunt", strikeHeightRatio: 0.85f,
                strikeTime: 0.28f, length: 0.6f, overwrite: overwrite);

            // Bộ xương, hit slash (chiêu Chém xương, đứng yên ra đòn): giơ kiếm qua đầu, rồi chém xuống.
            BuildStrikeClip("Enemy_Bo_xuong", "slash",
                windUpSuffix: "_slash_ready", windUpHeightRatio: 1.1f,
                strikeSuffix: "_slash", strikeHeightRatio: 0.9f,
                strikeTime: 0.25f, length: 0.55f, overwrite: overwrite);

            // Nhầy, kiểu tầm xa (chiêu Phun axit): phồng người hít vào, rồi xẹp xuống phun axit. Đạn bay ra đúng lúc Strike.
            BuildStrikeClip("Enemy_Nhay", "ranged",
                windUpSuffix: "_spit_ready", windUpHeightRatio: 1.1f,
                strikeSuffix: "_spit", strikeHeightRatio: 0.75f,
                strikeTime: 0.3f, length: 0.6f, overwrite: overwrite);

            // Nhầy, hit blunt (chiêu Húc): co người lấy đà 0,25 giây rồi bật tới húc.
            // Lúc co thì dẹt xuống thấp hơn lúc đứng. Ảnh húc là một khối nằm ngang dài gấp đôi chiều cao,
            // nên phải để thấp hẳn, nếu không bề ngang sẽ to quá cỡ.
            BuildStrikeClip("Enemy_Nhay", "blunt",
                windUpSuffix: "_blunt_ready", windUpHeightRatio: 0.7f,
                strikeSuffix: "_blunt", strikeHeightRatio: 0.6f,
                strikeTime: 0.25f, length: 0.55f, overwrite: overwrite);

            // Lính kiếm, hit blunt: kéo kiếm ra sau như lúc chém, rồi lao vai tới thúc chuôi kiếm.
            BuildStrikeClip("Character_Linh_kiem", "blunt",
                windUpSuffix: "_slash_ready", windUpHeightRatio: 0.9f,
                strikeSuffix: "_blunt", strikeHeightRatio: 0.85f,
                strikeTime: 0.2f, length: 0.5f, overwrite: overwrite);

            // Hộ vệ, hit slash: giơ chùy như lúc bổ, rồi quét ngang chùy.
            BuildStrikeClip("Character_Ho_ve", "slash",
                windUpSuffix: "_blunt_ready", windUpHeightRatio: 1.05f,
                strikeSuffix: "_slash", strikeHeightRatio: 0.9f,
                strikeTime: 0.28f, length: 0.6f, overwrite: overwrite);

            // Hộ vệ, hit pierce: thủ thế bằng ảnh đứng, rồi núp sau khiên lao xuyên qua mục tiêu.
            BuildStrikeClip("Character_Ho_ve", "pierce",
                windUpSuffix: "", windUpHeightRatio: 1f,
                strikeSuffix: "_pierce", strikeHeightRatio: 0.8f,
                strikeTime: 0.2f, length: 0.5f, overwrite: overwrite);

            // Pháp sư, hit blunt khi bị ép đánh gần (thẻ Đập): kéo gậy ra sau, rồi quật gậy xuống.
            BuildStrikeClip("Character_Phap_su", "blunt",
                windUpSuffix: "_cast_ready", windUpHeightRatio: 0.95f,
                strikeSuffix: "_blunt", strikeHeightRatio: 0.9f,
                strikeTime: 0.25f, length: 0.55f, overwrite: overwrite);

            // Bộ xương, hit defence (chiêu Thủ thế, tự dựng shield): đứng như thường, rồi khom người giơ khiên.
            BuildStrikeClip("Enemy_Bo_xuong", "defence",
                windUpSuffix: "", windUpHeightRatio: 1f,
                strikeSuffix: "_defence", strikeHeightRatio: 0.85f,
                strikeTime: 0.2f, length: 0.6f, overwrite: overwrite);

            AssetDatabase.SaveAssets();
        }

        private static void BuildStrikeClip(string dataName, string clipKey,
            string windUpSuffix, float windUpHeightRatio,
            string strikeSuffix, float strikeHeightRatio,
            float strikeTime, float length, bool overwrite)
        {
            string clipPath = AnimationFolder + "/" + dataName + "_" + clipKey + ".anim";
            bool exists = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null;
            if (exists && !overwrite) return;

            Sprite windUp = LoadSprite(PortraitFolder + "/" + dataName + windUpSuffix + ".png");
            Sprite strike = LoadSprite(PortraitFolder + "/" + dataName + strikeSuffix + ".png");
            if (windUp == null || strike == null)
            {
                Debug.LogWarning("[ProjectS] Chưa tạo được clip " + clipPath + " vì thiếu ảnh "
                    + dataName + windUpSuffix + ".png hoặc " + dataName + strikeSuffix + ".png trong " + PortraitFolder);
                return;
            }

            var clip = new AnimationClip { frameRate = FrameRate };

            // Đổi ảnh của Body: ảnh lấy đà, rồi ảnh ra đòn giữ tới hết clip.
            EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(BodyPath, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = windUp },
                new ObjectReferenceKeyframe { time = strikeTime, value = strike },
                new ObjectReferenceKeyframe { time = length, value = strike }
            });

            // Chiều cao của từng ảnh so với ảnh đứng, đổi cùng lúc với ảnh. Khóa kiểu bậc thang để không bị nội suy.
            var ratioCurve = new AnimationCurve(
                new Keyframe(0f, windUpHeightRatio),
                new Keyframe(strikeTime, strikeHeightRatio),
                new Keyframe(length, strikeHeightRatio));
            for (int i = 0; i < ratioCurve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(ratioCurve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(ratioCurve, i, AnimationUtility.TangentMode.Constant);
            }
            EditorCurveBinding ratioBinding = EditorCurveBinding.FloatCurve("", typeof(ActorPose), nameof(ActorPose.heightRatio));
            AnimationUtility.SetEditorCurve(clip, ratioBinding, ratioCurve);

            // Khoảnh khắc đòn đánh trúng. Kéo sự kiện này trong cửa sổ Animation để đổi nhịp.
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent { time = strikeTime, functionName = ActorPose.StrikeEvent }
            });

            Directory.CreateDirectory(AnimationFolder);
            AssetDatabase.Refresh();
            if (exists) AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log("[ProjectS] Đã tạo clip ra đòn " + clipPath);
        }

        /// <summary>
        /// Đưa mọi ảnh trong Portraits và CardArt về cùng một cách nhập: sprite kiểu Single, tức cả file là một sprite.
        /// Ảnh nhập kiểu Multiple giữ một khung cắt cố định trong file .meta; khi file ảnh bị thay bằng ảnh cỡ khác
        /// thì khung đó cắt sai, còn ảnh mới chưa có khung nào thì không ra sprite. Kiểu Single không có hai lỗi này.
        /// Chỉ ảnh chưa đúng kiểu mới bị nhập lại, nên chạy nhiều lần không tốn gì.
        /// </summary>
        private static void NormalizePortraitImports()
        {
            // Ảnh minh họa của thẻ cũng phải là sprite thì giao diện mới nạp được.
            var folders = new System.Collections.Generic.List<string>();
            if (AssetDatabase.IsValidFolder(PortraitFolder)) folders.Add(PortraitFolder);
            if (AssetDatabase.IsValidFolder(CardArtFolder)) folders.Add(CardArtFolder);
            if (folders.Count == 0) return;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log("[ProjectS] Đã chuyển ảnh sang sprite kiểu Single: " + path);
            }
        }

        /// <summary>Lấy sprite đầu tiên của một file ảnh, dù ảnh được nhập kiểu Single hay Multiple.</summary>
        private static Sprite LoadSprite(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }
    }
}
