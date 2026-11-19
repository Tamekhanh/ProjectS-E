using System;

namespace ProjectS.Data
{
    /// <summary>Hit áp dụng lên ai.</summary>
    public enum HitTarget
    {
        /// <summary>Mục tiêu người chơi kéo thẻ vào.</summary>
        MainTarget,
        /// <summary>Chính người dùng thẻ.</summary>
        Self,
        AllAllies,
        AllEnemies
    }

    public enum StatusType
    {
        None,
        /// <summary>Gây sát thương ở mỗi hit của thẻ lên người dính.</summary>
        Bleed,
        /// <summary>Gây sát thương cuối turn của người dính.</summary>
        Burn
    }

    /// <summary>Một hit trong thẻ hoặc trong chiêu của địch.</summary>
    [Serializable]
    public class HitData
    {
        public HitAttribute attribute;
        /// <summary>Lượng sát thương, hồi máu hoặc shield, trước khi tính chỉ số và điểm yếu.</summary>
        public int value;
        public HitTarget target = HitTarget.MainTarget;
        public StatusType status = StatusType.None;
        /// <summary>Số stack status gây ra. Bỏ qua khi status là None.</summary>
        public int statusAmount;
    }
}
