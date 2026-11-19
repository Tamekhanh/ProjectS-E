using System.Collections.Generic;
using NUnit.Framework;
using ProjectS.Data;
using ProjectS.Data.Editor;
using UnityEngine;

namespace ProjectS.Tests
{
    public class DataTests
    {
        [TestCase(Rate.Bet, 3)]
        [TestCase(Rate.Gimel, 3)]
        [TestCase(Rate.Dalet, 3)]
        [TestCase(Rate.He, 3)]
        [TestCase(Rate.Vav, 2)]
        [TestCase(Rate.Zayin, 2)]
        [TestCase(Rate.Het, 2)]
        [TestCase(Rate.Tet, 1)]
        [TestCase(Rate.Yod, 1)]
        [TestCase(Rate.Aleph, 1)]
        public void MaxCopies_FollowsRateTable(Rate rate, int expected)
        {
            Assert.AreEqual(expected, RateRules.MaxCopies(rate));
        }

        [Test]
        public void Rate_HasTenValues()
        {
            Assert.AreEqual(10, System.Enum.GetValues(typeof(Rate)).Length);
        }

        [TestCase(HitAttribute.Slash, true)]
        [TestCase(HitAttribute.Blunt, true)]
        [TestCase(HitAttribute.Pierce, true)]
        [TestCase(HitAttribute.Magic, true)]
        [TestCase(HitAttribute.Heal, false)]
        [TestCase(HitAttribute.Support, false)]
        [TestCase(HitAttribute.Defence, false)]
        public void IsDamage_OnlyForDamagingAttributes(HitAttribute attribute, bool expected)
        {
            Assert.AreEqual(expected, attribute.IsDamage());
        }

        [Test]
        public void GetMultiplier_UsesWeaknessTable_AndDefaultsToOne()
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.weaknesses.Add(new WeaknessEntry { attribute = HitAttribute.Slash, multiplier = 1.5f });
            enemy.weaknesses.Add(new WeaknessEntry { attribute = HitAttribute.Blunt, multiplier = 0.5f });

            Assert.AreEqual(1.5f, enemy.GetMultiplier(HitAttribute.Slash));
            Assert.AreEqual(0.5f, enemy.GetMultiplier(HitAttribute.Blunt));
            Assert.AreEqual(1f, enemy.GetMultiplier(HitAttribute.Pierce));

            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void GetMultiplier_IgnoresWeaknessForHealSupportDefence()
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.weaknesses.Add(new WeaknessEntry { attribute = HitAttribute.Heal, multiplier = 2f });

            Assert.AreEqual(1f, enemy.GetMultiplier(HitAttribute.Heal));
            Assert.AreEqual(1f, enemy.GetMultiplier(HitAttribute.Support));
            Assert.AreEqual(1f, enemy.GetMultiplier(HitAttribute.Defence));

            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void TargetsAlly_DependsOnFirstMainTargetHit()
        {
            CardData attack = ScriptableObject.CreateInstance<CardData>();
            attack.hits.Add(new HitData { attribute = HitAttribute.Blunt, value = 4 });
            attack.hits.Add(new HitData { attribute = HitAttribute.Defence, value = 4, target = HitTarget.Self });

            CardData heal = ScriptableObject.CreateInstance<CardData>();
            heal.hits.Add(new HitData { attribute = HitAttribute.Heal, value = 6 });

            Assert.IsTrue(attack.NeedsTarget);
            Assert.IsFalse(attack.TargetsAlly);
            Assert.IsTrue(heal.NeedsTarget);
            Assert.IsTrue(heal.TargetsAlly);

            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(heal);
        }

        [Test]
        public void SampleData_HasExpectedCounts()
        {
            Assert.AreEqual(3, SampleDataBuilder.BuildCharacters().Count);
            Assert.AreEqual(12, SampleDataBuilder.BuildCards().Count);
            Assert.AreEqual(2, SampleDataBuilder.BuildEnemies().Count);
        }

        [Test]
        public void SampleCards_AreValid()
        {
            var names = new HashSet<string>();
            foreach (CardData card in SampleDataBuilder.BuildCards())
            {
                Assert.IsTrue(names.Add(card.name), "Trùng tên file thẻ: " + card.name);
                Assert.IsNotEmpty(card.displayName);
                Assert.GreaterOrEqual(card.apCost, CardData.MinApCost, card.name);
                Assert.LessOrEqual(card.apCost, CardData.MaxApCost, card.name);
                Assert.IsNotEmpty(card.hits, card.name);
                foreach (HitData hit in card.hits)
                {
                    Assert.Greater(hit.value, 0, card.name);
                    if (hit.status != StatusType.None) Assert.Greater(hit.statusAmount, 0, card.name);
                }
            }
        }

        [Test]
        public void SampleEnemies_HaveMovesAndWeaknesses()
        {
            foreach (EnemyData enemy in SampleDataBuilder.BuildEnemies())
            {
                Assert.GreaterOrEqual(enemy.moves.Count, 2, enemy.name);
                Assert.IsNotEmpty(enemy.weaknesses, enemy.name);
                foreach (EnemyMove move in enemy.moves) Assert.IsNotEmpty(move.hits, enemy.name);
            }
        }
    }
}
