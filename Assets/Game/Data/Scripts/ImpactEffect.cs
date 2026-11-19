namespace ProjectS.Data
{
    /// <summary>
    /// Hiệu ứng của một đòn tầm xa khi tới mục tiêu. Chỉ ảnh hưởng tới hiển thị, không đổi luật combat.
    /// Giá trị 0 là Projectile, nên dữ liệu cũ chưa có trường này vẫn bắn đạn bay tới như trước.
    /// </summary>
    public enum ImpactEffect
    {
        /// <summary>Một viên đạn bay từ người dùng tới mục tiêu, ví dụ cầu lửa.</summary>
        Projectile = 0,
        /// <summary>Sét giáng từ trên cao xuống thẳng mục tiêu, không có gì bay ngang sân.</summary>
        Lightning = 1
    }
}
