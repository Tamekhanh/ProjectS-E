using System;
using System.Collections.Generic;
using ProjectS.Data;

namespace ProjectS.Combat.Core
{
    /// <summary>Chọn chiêu và mục tiêu cho kẻ địch từ bộ chiêu cố định của nó.</summary>
    public static class EnemyBrain
    {
        /// <summary>
        /// Lên ý định cho turn kế tiếp. Chiêu được chọn xoay vòng theo bộ chiêu,
        /// nên địch có từ 2 chiêu trở lên sẽ không dùng một chiêu hai lần liên tiếp.
        /// </summary>
        public static EnemyIntent Plan(Unit enemy, IReadOnlyList<Unit> players, Random rng)
        {
            List<EnemyMove> moves = enemy.EnemyData.moves;
            if (moves.Count == 0) return null;

            int index = enemy.LastMoveIndex < 0 ? rng.Next(moves.Count) : (enemy.LastMoveIndex + 1) % moves.Count;
            enemy.LastMoveIndex = index;
            EnemyMove move = moves[index];

            return new EnemyIntent
            {
                Move = move,
                Target = NeedsTarget(move) ? PickTarget(players, rng) : null
            };
        }

        public static bool NeedsTarget(EnemyMove move)
        {
            foreach (HitData hit in move.hits)
            {
                if (hit.target == HitTarget.MainTarget) return true;
            }
            return false;
        }

        /// <summary>Chọn ngẫu nhiên một nhân vật còn sống, hoặc null nếu không còn ai.</summary>
        public static Unit PickTarget(IReadOnlyList<Unit> players, Random rng)
        {
            var alive = new List<Unit>();
            foreach (Unit player in players)
            {
                if (player.IsAlive) alive.Add(player);
            }
            return alive.Count == 0 ? null : alive[rng.Next(alive.Count)];
        }
    }
}
