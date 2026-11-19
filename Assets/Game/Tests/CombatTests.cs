using System.Collections.Generic;
using NUnit.Framework;
using ProjectS.Combat.Core;
using ProjectS.Data;
using ProjectS.Data.Editor;
using UnityEngine;

namespace ProjectS.Tests
{
    public class CombatTests
    {
        private static CardData Card(int apCost, params HitData[] hits)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.displayName = "Thẻ thử";
            card.apCost = apCost;
            card.hits = new List<HitData>(hits);
            return card;
        }

        private static HitData Hit(HitAttribute attribute, int value, HitTarget target = HitTarget.MainTarget)
        {
            return new HitData { attribute = attribute, value = value, target = target };
        }

        private static CharacterData Character(int hp = 30, int attack = 0, int defence = 0, int speed = 5)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.displayName = "Nhân vật thử";
            data.maxHp = hp;
            data.attack = attack;
            data.defence = defence;
            data.speed = speed;
            data.baseAp = 3;
            return data;
        }

        private static EnemyData Enemy(int hp = 20, int attack = 0, int defence = 0, int speed = 1)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.displayName = "Địch thử";
            data.maxHp = hp;
            data.attack = attack;
            data.defence = defence;
            data.speed = speed;
            data.moves.Add(new EnemyMove
            {
                displayName = "Đánh",
                hits = new List<HitData> { Hit(HitAttribute.Blunt, 4) }
            });
            return data;
        }

        private static List<CardData> Copies(CardData card, int count)
        {
            var cards = new List<CardData>();
            for (int i = 0; i < count; i++) cards.Add(card);
            return cards;
        }

        // ---------- Deck ----------

        [Test]
        public void Deck_DrawsDiscardsAndReshuffles()
        {
            var cards = new List<CardData>();
            for (int i = 0; i < 8; i++) cards.Add(Card(1, Hit(HitAttribute.Slash, 1)));
            var deck = new Deck(cards, new System.Random(1));

            Assert.AreEqual(5, deck.Draw(5));
            Assert.AreEqual(5, deck.Hand.Count);
            Assert.AreEqual(3, deck.DrawPile.Count);

            deck.DiscardHand();
            Assert.AreEqual(0, deck.Hand.Count);
            Assert.AreEqual(5, deck.DiscardPile.Count);

            // Chồng rút còn 3 thẻ, rút 5 thì phải xáo chồng bỏ lại.
            Assert.AreEqual(5, deck.Draw(5));
            Assert.AreEqual(5, deck.Hand.Count);
            Assert.AreEqual(8, deck.TotalCount);
        }

        [Test]
        public void Deck_DrawStopsWhenNoCardsLeft()
        {
            var deck = new Deck(Copies(Card(1, Hit(HitAttribute.Slash, 1)), 3), new System.Random(1));
            Assert.AreEqual(3, deck.Draw(5));
            Assert.AreEqual(0, deck.Draw(5));
        }

        // ---------- Thứ tự turn ----------

        [Test]
        public void TurnQueue_OrdersBySpeed_PlayersWinTies()
        {
            var rng = new System.Random(1);
            var none = new List<CardData>();
            var fast = new Unit(Character(speed: 9), none, rng);
            var slow = new Unit(Character(speed: 2), none, rng);
            var enemyTie = new Unit(Enemy(speed: 9));
            var queue = new TurnQueue(new[] { slow, enemyTie, fast });

            Assert.AreSame(fast, queue.Next());
            Assert.AreSame(enemyTie, queue.Next());
            Assert.AreSame(slow, queue.Next());
            Assert.AreEqual(1, queue.Round);

            Assert.AreSame(fast, queue.Next());
            Assert.AreEqual(2, queue.Round);
        }

        [Test]
        public void TurnQueue_SkipsDeadUnits()
        {
            var rng = new System.Random(1);
            var player = new Unit(Character(speed: 5), new List<CardData>(), rng);
            var dead = new Unit(Enemy(hp: 5, speed: 9));
            dead.TakeDamage(99);
            var queue = new TurnQueue(new[] { player, dead });

            Assert.AreSame(player, queue.Next());
            Assert.AreSame(player, queue.Next());
        }

        // ---------- Sát thương ----------

        [Test]
        public void Damage_UsesAttackDefenceAndWeakness()
        {
            var attacker = new Unit(Character(attack: 3), new List<CardData>(), new System.Random(1));
            EnemyData enemyData = Enemy(defence: 1);
            enemyData.weaknesses.Add(new WeaknessEntry { attribute = HitAttribute.Slash, multiplier = 1.5f });
            enemyData.weaknesses.Add(new WeaknessEntry { attribute = HitAttribute.Blunt, multiplier = 0.5f });
            var target = new Unit(enemyData);

            // (6 + 3 - 1) = 8
            Assert.AreEqual(12, DamageCalculator.Calculate(Hit(HitAttribute.Slash, 6), attacker, target));
            Assert.AreEqual(4, DamageCalculator.Calculate(Hit(HitAttribute.Blunt, 6), attacker, target));
            Assert.AreEqual(8, DamageCalculator.Calculate(Hit(HitAttribute.Pierce, 6), attacker, target));
        }

        [Test]
        public void Damage_IsAtLeastOne()
        {
            var attacker = new Unit(Character(attack: 0), new List<CardData>(), new System.Random(1));
            var target = new Unit(Enemy(defence: 50));
            Assert.AreEqual(1, DamageCalculator.Calculate(Hit(HitAttribute.Slash, 2), attacker, target));
        }

        [Test]
        public void Shield_AbsorbsDamageBeforeHp()
        {
            var unit = new Unit(Enemy(hp: 20));
            unit.AddShield(5);

            Assert.AreEqual(3, unit.TakeDamage(8));
            Assert.AreEqual(0, unit.Shield);
            Assert.AreEqual(17, unit.Hp);
        }

        [Test]
        public void Heal_DoesNotExceedMaxHp()
        {
            var unit = new Unit(Enemy(hp: 20));
            unit.TakeDamage(4);
            Assert.AreEqual(4, unit.Heal(10));
            Assert.AreEqual(20, unit.Hp);
        }

        // ---------- Luật trong trận ----------

        private static BattleState OneVsOne(CardData card, out Unit player, out Unit enemy,
            int enemyHp = 50, int playerSpeed = 5)
        {
            var rng = new System.Random(7);
            player = new Unit(Character(speed: playerSpeed), Copies(card, 10), rng);
            enemy = new Unit(Enemy(hp: enemyHp));
            var battle = new BattleState(new[] { player }, new[] { enemy }, rng);
            battle.Start();
            return battle;
        }

        [Test]
        public void StartTurn_RefillsApAndDrawsFive()
        {
            BattleState battle = OneVsOne(Card(1, Hit(HitAttribute.Slash, 5)), out Unit player, out _);

            Assert.AreSame(player, battle.Current);
            Assert.AreEqual(3, player.Ap);
            Assert.AreEqual(BattleState.HandSize, player.Deck.Hand.Count);
        }

        [Test]
        public void PlayCard_SpendsAp_DealsDamage_AndDiscards()
        {
            CardData card = Card(2, Hit(HitAttribute.Slash, 5));
            BattleState battle = OneVsOne(card, out Unit player, out Unit enemy);

            Assert.AreEqual(PlayResult.Ok, battle.PlayCard(card, enemy));
            Assert.AreEqual(1, player.Ap);
            Assert.AreEqual(45, enemy.Hp);
            Assert.AreEqual(4, player.Deck.Hand.Count);
            Assert.AreEqual(1, player.Deck.DiscardPile.Count);
            Assert.AreEqual(1, battle.CardsPlayed);
            Assert.AreEqual(2, battle.ApSpent);
        }

        [Test]
        public void PlayCard_FailsWithoutEnoughAp()
        {
            CardData card = Card(2, Hit(HitAttribute.Slash, 5));
            BattleState battle = OneVsOne(card, out Unit player, out Unit enemy);

            Assert.AreEqual(PlayResult.Ok, battle.PlayCard(card, enemy));
            Assert.AreEqual(PlayResult.NotEnoughAp, battle.PlayCard(card, enemy));
            Assert.AreEqual(1, player.Ap);
            Assert.AreEqual(45, enemy.Hp);
        }

        [Test]
        public void PlayCard_RejectsWrongSideTarget()
        {
            CardData attack = Card(1, Hit(HitAttribute.Slash, 5));
            BattleState battle = OneVsOne(attack, out Unit player, out Unit enemy);
            Assert.AreEqual(PlayResult.InvalidTarget, battle.PlayCard(attack, player));
            Assert.AreEqual(PlayResult.InvalidTarget, battle.PlayCard(attack, null));

            CardData heal = Card(1, Hit(HitAttribute.Heal, 5));
            BattleState healBattle = OneVsOne(heal, out Unit healer, out Unit healEnemy);
            Assert.AreEqual(PlayResult.InvalidTarget, healBattle.PlayCard(heal, healEnemy));
            Assert.AreEqual(PlayResult.Ok, healBattle.PlayCard(heal, healer));
        }

        [Test]
        public void MultiHitCard_AppliesSecondaryHitToSelf()
        {
            CardData card = Card(2, Hit(HitAttribute.Blunt, 4), Hit(HitAttribute.Defence, 6, HitTarget.Self));
            BattleState battle = OneVsOne(card, out Unit player, out Unit enemy);

            Assert.AreEqual(PlayResult.Ok, battle.PlayCard(card, enemy));
            Assert.AreEqual(46, enemy.Hp);
            Assert.AreEqual(6, player.Shield);
        }

        [Test]
        public void PreviewReceivers_ListsMainTargetAndSelf_WithoutChangingState()
        {
            CardData card = Card(2, Hit(HitAttribute.Blunt, 4), Hit(HitAttribute.Defence, 6, HitTarget.Self));
            BattleState battle = OneVsOne(card, out Unit player, out Unit enemy);

            List<Unit> receivers = battle.PreviewReceivers(card, enemy);

            CollectionAssert.AreEquivalent(new[] { enemy, player }, receivers);
            Assert.AreEqual(50, enemy.Hp);
            Assert.AreEqual(0, player.Shield);
            Assert.AreEqual(3, player.Ap);
        }

        [Test]
        public void PreviewReceivers_AreaCard_ListsEveryLivingEnemy()
        {
            var rng = new System.Random(5);
            CardData area = Card(2, Hit(HitAttribute.Magic, 3, HitTarget.AllEnemies));
            var player = new Unit(Character(speed: 9), Copies(area, 10), rng);
            var first = new Unit(Enemy());
            var second = new Unit(Enemy());
            var dead = new Unit(Enemy());
            dead.TakeDamage(999);
            var battle = new BattleState(new[] { player }, new[] { first, second, dead }, rng);
            battle.Start();

            Assert.IsFalse(area.NeedsTarget);
            CollectionAssert.AreEquivalent(new[] { first, second }, battle.PreviewReceivers(area, null));

            Assert.AreEqual(PlayResult.Ok, battle.PlayCard(area, null));
            Assert.AreEqual(17, first.Hp);
            Assert.AreEqual(17, second.Hp);
        }

        [Test]
        public void EndTurn_DiscardsHand_AndPassesTurnToEnemy()
        {
            BattleState battle = OneVsOne(Card(1, Hit(HitAttribute.Slash, 5)), out Unit player, out Unit enemy);

            battle.EndTurn();
            Assert.AreEqual(0, player.Deck.Hand.Count);
            Assert.AreEqual(5, player.Deck.DiscardPile.Count);
            Assert.AreSame(enemy, battle.Current);
        }

        [Test]
        public void Shield_LastsThroughEnemyTurn_ThenExpiresOnOwnersNextTurn()
        {
            CardData shieldCard = Card(1, Hit(HitAttribute.Defence, 10));
            BattleState battle = OneVsOne(shieldCard, out Unit player, out Unit enemy);

            battle.PlayCard(shieldCard, player);
            Assert.AreEqual(10, player.Shield);

            battle.EndTurn();
            battle.ExecuteEnemyTurn();

            // Địch đánh 4, shield chặn hết nên HP còn nguyên. Sang turn mới của người chơi thì shield mất.
            Assert.AreSame(player, battle.Current);
            Assert.AreEqual(player.MaxHp, player.Hp);
            Assert.AreEqual(0, player.Shield);
        }

        [Test]
        public void Enemy_ShowsIntentBeforeActing()
        {
            BattleState battle = OneVsOne(Card(1, Hit(HitAttribute.Slash, 5)), out Unit player, out Unit enemy);

            Assert.IsNotNull(enemy.Intent);
            Assert.AreEqual("Đánh", enemy.Intent.Move.displayName);
            Assert.AreSame(player, enemy.Intent.Target);
        }

        [Test]
        public void KillingLastEnemy_EndsBattleWithVictory()
        {
            CardData card = Card(1, Hit(HitAttribute.Slash, 50));
            BattleState battle = OneVsOne(card, out _, out Unit enemy, enemyHp: 10);
            BattleOutcome reported = BattleOutcome.Ongoing;
            battle.BattleEnded += outcome => reported = outcome;

            battle.PlayCard(card, enemy);

            Assert.IsFalse(enemy.IsAlive);
            Assert.AreEqual(BattleOutcome.Victory, battle.Outcome);
            Assert.AreEqual(BattleOutcome.Victory, reported);
            Assert.AreEqual(PlayResult.BattleOver, battle.PlayCard(card, enemy));
        }

        [Test]
        public void LosingAllCharacters_EndsBattleWithDefeat()
        {
            var rng = new System.Random(3);
            var player = new Unit(Character(hp: 3, speed: 1), new List<CardData>(), rng);
            var enemy = new Unit(Enemy(speed: 9));
            var battle = new BattleState(new[] { player }, new[] { enemy }, rng);
            battle.Start();

            Assert.AreSame(enemy, battle.Current);
            battle.ExecuteEnemyTurn();

            Assert.AreEqual(BattleOutcome.Defeat, battle.Outcome);
        }

        // ---------- Trận đầy đủ với dữ liệu mẫu ----------

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void FullBattle_ThreeVsThree_ReachesAnOutcome(int seed)
        {
            var rng = new System.Random(seed);
            List<CardData> cards = SampleDataBuilder.BuildCards();
            List<EnemyData> enemyData = SampleDataBuilder.BuildEnemies();

            var players = new List<Unit>();
            foreach (CharacterData character in SampleDataBuilder.BuildCharacters())
            {
                // Mỗi thẻ mẫu 2 bản. Số thẻ mẫu thay đổi theo thời gian, nên không ghi cứng cỡ deck.
                var deck = new List<CardData>();
                foreach (CardData card in cards)
                {
                    deck.Add(card);
                    deck.Add(card);
                }
                players.Add(new Unit(character, deck, rng));
            }

            var enemies = new List<Unit>
            {
                new Unit(enemyData[0]),
                new Unit(enemyData[1]),
                new Unit(enemyData[0])
            };

            var battle = new BattleState(players, enemies, rng);
            battle.Start();

            const int turnLimit = 500;
            while (battle.Outcome == BattleOutcome.Ongoing && battle.TurnCount < turnLimit)
            {
                if (battle.Current.IsPlayer)
                {
                    PlayEverythingAffordable(battle);
                    battle.EndTurn();
                }
                else
                {
                    battle.ExecuteEnemyTurn();
                }
            }

            Assert.AreNotEqual(BattleOutcome.Ongoing, battle.Outcome,
                "Trận không kết thúc sau " + turnLimit + " turn.");
            int deckSize = cards.Count * 2;
            foreach (Unit player in players) Assert.AreEqual(deckSize, player.Deck.TotalCount, "Deck bị mất hoặc thừa thẻ.");
            Debug.Log("[ProjectS] Trận thử seed " + seed + ": " + battle.Outcome + " sau " + battle.TurnCount
                + " turn, " + battle.CardsPlayed + " thẻ, " + battle.ApSpent + " AP.");
        }

        /// <summary>Người chơi giả: đánh mọi thẻ đủ AP, thẻ tấn công vào địch ít máu nhất, thẻ hỗ trợ vào đồng đội ít máu nhất.</summary>
        private static void PlayEverythingAffordable(BattleState battle)
        {
            bool played = true;
            while (played && battle.Outcome == BattleOutcome.Ongoing)
            {
                played = false;
                foreach (CardData card in new List<CardData>(battle.Current.Deck.Hand))
                {
                    Unit target = Weakest(card.TargetsAlly ? battle.Players : battle.Enemies);
                    if (battle.PlayCard(card, target) == PlayResult.Ok)
                    {
                        played = true;
                        break;
                    }
                }
            }
        }

        private static Unit Weakest(IReadOnlyList<Unit> units)
        {
            Unit weakest = null;
            foreach (Unit unit in units)
            {
                if (!unit.IsAlive) continue;
                if (weakest == null || unit.Hp < weakest.Hp) weakest = unit;
            }
            return weakest;
        }
    }
}
