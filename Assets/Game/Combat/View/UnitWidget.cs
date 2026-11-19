using ProjectS.Combat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.Combat.View
{
    /// <summary>Một nhân vật hoặc kẻ địch đứng trên sân, cùng bảng thông tin nổi trên đầu.</summary>
    public class UnitWidget
    {
        public Unit Unit;

        // Phần trên sân 3D.
        public GameObject ActorRoot;
        public SpriteRenderer Body;
        public SpriteRenderer Ring;
        /// <summary>Lớp ánh sáng ôm sát viền sprite: các bản sao hình bóng của sprite, đặt lệch ra quanh nó.</summary>
        public GameObject Glow;
        public SpriteRenderer[] GlowRenderers;
        public Color GlowColor;
        public Color BaseColor;
        /// <summary>Chỗ đứng trên sân. Unit rời chỗ này khi lao vào đánh rồi quay về.</summary>
        public Vector3 HomePosition;
        public Vector3 BodyRestPosition;
        public int BodyOrder;

        // Ảnh đứng và ảnh ra đòn. AttackSprite là null nếu unit chưa có ảnh ra đòn.
        public Sprite IdleSprite;
        public Sprite AttackSprite;
        /// <summary>Ảnh bị đánh (tên dữ liệu + "_hurt"), hoặc null nếu unit chưa có.</summary>
        public Sprite HurtSprite;
        /// <summary>Tăng mỗi lần trúng đòn, để nhát trúng sau không bị nhát trước đổi ảnh về sớm.</summary>
        public int HurtToken;

        // Tư thế và clip ra đòn.
        public Animator Animator;
        public ActorPose Pose;
        /// <summary>Tên file dữ liệu của unit, dùng để tìm ảnh và clip, ví dụ Character_Linh_kiem.</summary>
        public string DataName;
        /// <summary>Ảnh và tỉ lệ chiều cao unit giữ khi không có clip nào đang phát.</summary>
        public Sprite PoseSprite;
        public float PoseRatio = 1f;
        public bool ClipPlaying;
        // Ảnh đã được canh lần gần nhất, để chỉ canh lại khi có thay đổi.
        public Sprite FittedSprite;
        public float FittedRatio;
        public bool FittedFlip;
        public float Height;
        public float Width;

        // Trạng thái đang hiển thị. Luật combat tính xong ngay, còn màn hình cập nhật theo từng hit được diễn.
        public int DisplayHp;
        public int DisplayShield;
        public bool DisplayDead;

        // Phần giao diện đi theo unit.
        public RectTransform Panel;
        public Text Text;
        public RectTransform HpFill;
        public RectTransform HitArea;
    }
}
