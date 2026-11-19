using System;
using System.Collections.Generic;
using ProjectS.Data;

namespace ProjectS.Combat.Core
{
    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat
    }

    public enum PlayResult
    {
        Ok,
        BattleOver,
        NotPlayerTurn,
        NotInHand,
        NotEnoughAp,
        InvalidTarget
    }

    /// <summary>
    /// Toàn bộ trạng thái và luật của một trận. Không phụ thuộc phần hiển thị:
    /// màn hình chỉ gọi PlayCard, EndTurn, ExecuteEnemyTurn và nghe các sự kiện.
    /// </summary>
    public class BattleState
    {
        public const int HandSize = 5;

        private readonly List<Unit> players;
        private readonly List<Unit> enemies;
        private readonly TurnQueue queue;
        private readonly Random rng;

        public IReadOnlyList<Unit> Players => players;
        public IReadOnlyList<Unit> Enemies => enemies;
        public Unit Current { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
        public int Round => queue.Round;
        /// <summary>Tổng số lượt turn đã bắt đầu, tính cả hai bên.</summary>
        public int TurnCount { get; private set; }

        /// <summary>Số thẻ thường cả đội đã dùng trong trận. Dùng cho bộ đếm Abnormality.</summary>
        public int CardsPlayed { get; private set; }
        /// <summary>Tổng AP cả đội đã tiêu trong trận. Dùng cho bộ đếm Abnormality.</summary>
        public int ApSpent { get; private set; }

        public event Action<Unit> TurnStarted;
        public event Action<Unit> TurnEnded;
        public event Action<Unit, CardData, Unit> CardPlayed;
        public event Action<Unit, EnemyMove, Unit> EnemyMoveUsed;
        /// <summary>Người gây, người nhận, hit, và lượng thực tế (HP mất, HP hồi hoặc shield nhận).</summary>
        public event Action<Unit, Unit, HitData, int> HitResolved;
        public event Action<Unit> UnitDied;
        public event Action<BattleOutcome> BattleEnded;

        public BattleState(IEnumerable<Unit> players, IEnumerable<Unit> enemies, Random rng)
        {
            this.players = new List<Unit>(players);
            this.enemies = new List<Unit>(enemies);
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));

            var all = new List<Unit>(this.players);
            all.AddRange(this.enemies);
            queue = new TurnQueue(all);
        }

        /// <summary>Bắt đầu trận: mọi kẻ địch lên ý định, rồi unit nhanh nhất vào turn.</summary>
        public void Start()
        {
            foreach (Unit enemy in enemies) enemy.Intent = EnemyBrain.Plan(enemy, players, rng);
            AdvanceTurn();
        }

        public List<Unit> UpcomingTurns(int count)
        {
            return queue.Upcoming(count);
        }

        /// <summary>Kiểm tra thẻ có đánh được vào mục tiêu này không, chưa thay đổi gì.</summary>
        public PlayResult CanPlay(CardData card, Unit target)
        {
            if (Outcome != BattleOutcome.Ongoing) return PlayResult.BattleOver;
            if (Current == null || !Current.IsPlayer) return PlayResult.NotPlayerTurn;
            if (!Contains(Current.Deck.Hand, card)) return PlayResult.NotInHand;
            if (card.apCost > Current.Ap) return PlayResult.NotEnoughAp;
            if (card.NeedsTarget && !IsValidTarget(card, target)) return PlayResult.InvalidTarget;
            return PlayResult.Ok;
        }

        public bool IsValidTarget(CardData card, Unit target)
        {
            if (target == null || !target.IsAlive) return false;
            return card.TargetsAlly ? target.IsPlayer : !target.IsPlayer;
        }

        /// <summary>
        /// Những unit còn sống sẽ chịu tác động nếu nhân vật đang tới turn đánh thẻ này vào mục tiêu đó.
        /// Chỉ xem trước, không thay đổi gì. Dùng để tô sáng mục tiêu, kể cả với thẻ đánh nhiều unit.
        /// </summary>
        public List<Unit> PreviewReceivers(CardData card, Unit target)
        {
            var result = new List<Unit>();
            if (Current == null || card == null) return result;

            foreach (HitData hit in card.hits)
            {
                foreach (Unit receiver in Receivers(Current, target, hit.target))
                {
                    if (receiver.IsAlive && !result.Contains(receiver)) result.Add(receiver);
                }
            }
            return result;
        }

        /// <summary>Nhân vật đang tới turn đánh một thẻ từ tay vào mục tiêu.</summary>
        public PlayResult PlayCard(CardData card, Unit target)
        {
            PlayResult check = CanPlay(card, target);
            if (check != PlayResult.Ok) return check;

            Unit source = Current;
            source.Ap -= card.apCost;
            source.Deck.Discard(card);
            CardsPlayed++;
            ApSpent += card.apCost;

            CardPlayed?.Invoke(source, card, target);
            ResolveHits(source, target, card.hits);
            return PlayResult.Ok;
        }

        /// <summary>Kết thúc turn của nhân vật: bỏ toàn bộ tay bài rồi chuyển sang unit kế tiếp.</summary>
        public void EndTurn()
        {
            if (Outcome != BattleOutcome.Ongoing || Current == null || !Current.IsPlayer) return;
            Current.Deck.DiscardHand();
            FinishTurn();
        }

        /// <summary>Kẻ địch đang tới turn thực hiện ý định, lên ý định mới, rồi kết thúc turn.</summary>
        public void ExecuteEnemyTurn()
        {
            if (Outcome != BattleOutcome.Ongoing || Current == null || Current.IsPlayer) return;

            Unit enemy = Current;
            EnemyIntent intent = enemy.Intent;
            if (intent != null)
            {
                Unit target = intent.Target;
                if (EnemyBrain.NeedsTarget(intent.Move) && (target == null || !target.IsAlive))
                {
                    target = EnemyBrain.PickTarget(players, rng);
                }

                EnemyMoveUsed?.Invoke(enemy, intent.Move, target);
                ResolveHits(enemy, target, intent.Move.hits);
            }

            if (Outcome != BattleOutcome.Ongoing) return;
            enemy.Intent = EnemyBrain.Plan(enemy, players, rng);
            FinishTurn();
        }

        private void FinishTurn()
        {
            TurnEnded?.Invoke(Current);
            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            if (Outcome != BattleOutcome.Ongoing) return;

            Current = queue.Next();
            if (Current == null) return;

            TurnCount++;
            // Shield chỉ tồn tại tới đầu turn kế tiếp của chính unit đó.
            Current.ClearShield();
            if (Current.IsPlayer)
            {
                Current.Ap = Current.MaxAp;
                Current.Deck.Draw(HandSize);
            }
            TurnStarted?.Invoke(Current);
        }

        private void ResolveHits(Unit source, Unit mainTarget, List<HitData> hits)
        {
            foreach (HitData hit in hits)
            {
                foreach (Unit receiver in Receivers(source, mainTarget, hit.target))
                {
                    if (!receiver.IsAlive) continue;

                    int amount = ApplyHit(source, receiver, hit);
                    HitResolved?.Invoke(source, receiver, hit, amount);

                    if (!receiver.IsAlive)
                    {
                        UnitDied?.Invoke(receiver);
                        UpdateOutcome();
                    }
                }
                if (Outcome != BattleOutcome.Ongoing) return;
            }
        }

        private static int ApplyHit(Unit source, Unit receiver, HitData hit)
        {
            switch (hit.attribute)
            {
                case HitAttribute.Heal:
                    return receiver.Heal(hit.value);
                case HitAttribute.Defence:
                    receiver.AddShield(hit.value);
                    return hit.value;
                case HitAttribute.Support:
                    // Hiệu ứng của support chưa được định nghĩa trong thiết kế.
                    return 0;
                default:
                    return receiver.TakeDamage(DamageCalculator.Calculate(hit, source, receiver));
            }
        }

        private List<Unit> Receivers(Unit source, Unit mainTarget, HitTarget target)
        {
            var result = new List<Unit>();
            switch (target)
            {
                case HitTarget.MainTarget:
                    if (mainTarget != null) result.Add(mainTarget);
                    break;
                case HitTarget.Self:
                    result.Add(source);
                    break;
                case HitTarget.AllAllies:
                    result.AddRange(source.IsPlayer ? players : enemies);
                    break;
                case HitTarget.AllEnemies:
                    result.AddRange(source.IsPlayer ? enemies : players);
                    break;
            }
            return result;
        }

        private void UpdateOutcome()
        {
            if (Outcome != BattleOutcome.Ongoing) return;

            if (!AnyAlive(enemies)) Outcome = BattleOutcome.Victory;
            else if (!AnyAlive(players)) Outcome = BattleOutcome.Defeat;

            if (Outcome != BattleOutcome.Ongoing) BattleEnded?.Invoke(Outcome);
        }

        private static bool AnyAlive(List<Unit> units)
        {
            foreach (Unit unit in units)
            {
                if (unit.IsAlive) return true;
            }
            return false;
        }

        private static bool Contains(IReadOnlyList<CardData> cards, CardData card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == card) return true;
            }
            return false;
        }
    }
}
