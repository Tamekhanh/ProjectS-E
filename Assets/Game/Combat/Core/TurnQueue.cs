using System.Collections.Generic;

namespace ProjectS.Combat.Core
{
    /// <summary>
    /// Thứ tự turn theo speed. Mỗi vòng, mọi unit còn sống được đi một lần, speed cao đi trước.
    /// Thứ tự được tính lại mỗi lần lấy unit kế tiếp, nên speed đổi giữa vòng có hiệu lực ngay.
    /// Hòa speed: nhân vật của người chơi đi trước, rồi tới thứ tự vị trí.
    /// </summary>
    public class TurnQueue
    {
        private readonly List<Unit> units;
        private readonly HashSet<Unit> acted = new HashSet<Unit>();

        public int Round { get; private set; } = 1;

        public TurnQueue(IEnumerable<Unit> units)
        {
            this.units = new List<Unit>(units);
        }

        /// <summary>Unit đi kế tiếp, hoặc null nếu không còn ai sống.</summary>
        public Unit Next()
        {
            Unit next = Best(acted);
            if (next == null)
            {
                acted.Clear();
                Round++;
                next = Best(acted);
                if (next == null) return null;
            }
            acted.Add(next);
            return next;
        }

        /// <summary>Thứ tự dự kiến của các turn sắp tới, dùng cho thanh thứ tự turn.</summary>
        public List<Unit> Upcoming(int count)
        {
            var result = new List<Unit>();
            var skip = new HashSet<Unit>(acted);
            int emptyRounds = 0;
            while (result.Count < count && emptyRounds < 2)
            {
                Unit next = Best(skip);
                if (next == null)
                {
                    skip.Clear();
                    emptyRounds++;
                    continue;
                }
                emptyRounds = 0;
                skip.Add(next);
                result.Add(next);
            }
            return result;
        }

        private Unit Best(HashSet<Unit> excluded)
        {
            Unit best = null;
            foreach (Unit unit in units)
            {
                if (!unit.IsAlive || excluded.Contains(unit)) continue;
                if (best == null || GoesBefore(unit, best)) best = unit;
            }
            return best;
        }

        private static bool GoesBefore(Unit a, Unit b)
        {
            if (a.Speed != b.Speed) return a.Speed > b.Speed;
            if (a.IsPlayer != b.IsPlayer) return a.IsPlayer;
            return false;
        }
    }
}
