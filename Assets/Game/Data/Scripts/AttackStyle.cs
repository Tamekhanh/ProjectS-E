namespace ProjectS.Data
{
    /// <summary>
    /// Cách một unit diễn đòn đánh trên sân. Chỉ ảnh hưởng tới hiển thị, không đổi luật combat.
    /// Giá trị 0 là Lunge, nên dữ liệu cũ chưa có trường này vẫn lao tới đánh như trước.
    /// </summary>
    public enum AttackStyle
    {
        /// <summary>Lao tới trước mặt mục tiêu, đánh, rồi quay về.</summary>
        Lunge = 0,
        /// <summary>Đứng yên tại chỗ và bắn đạn tới mục tiêu, mỗi hit một viên.</summary>
        Ranged = 1,
        /// <summary>Lao xuyên qua mục tiêu ở hit đầu, rồi đánh các hit còn lại từ phía sau lưng nó.</summary>
        DashThrough = 2,
        /// <summary>Đứng yên tại chỗ ra đòn (vung tay, quật), không lao tới. Hợp với quái quá to.</summary>
        InPlace = 3
    }
}
