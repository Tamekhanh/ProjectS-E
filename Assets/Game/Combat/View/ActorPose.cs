using UnityEngine;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Gắn trên gốc của mỗi unit trên sân, để clip animation ra đòn có chỗ điều khiển.
    /// Clip đổi ảnh của "Body" và chỉnh heightRatio; CombatScreen đọc lại rồi canh ảnh cho chân chạm sàn.
    /// </summary>
    public class ActorPose : MonoBehaviour
    {
        /// <summary>Tên sự kiện trong clip đánh dấu khoảnh khắc đòn đánh trúng.</summary>
        public const string StrikeEvent = "Strike";

        /// <summary>
        /// Chiều cao của ảnh đang hiện so với ảnh đứng. Tư thế lao thấp người thì nhỏ hơn 1.
        /// Chỉnh đường cong này trong cửa sổ Animation để nhân vật to nhỏ cho khớp giữa các ảnh.
        /// </summary>
        public float heightRatio = 1f;

        /// <summary>
        /// Nơi nhận sự kiện Strike của clip. Để trống có chủ ý: CombatScreen tự đọc thời điểm của sự kiện
        /// từ clip để gây sát thương đúng lúc. Hàm này chỉ cần tồn tại để Unity không báo thiếu nơi nhận.
        /// </summary>
        public void Strike()
        {
        }
    }
}
