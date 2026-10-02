using NUnit.Framework;
using CrazyLabs.Progression;
using UnityEngine;

namespace CrazyLabs.Tests
{
    public class ProgressionServiceTests
    {
        UpgradeDefinition speed;
        ProgressionService service;
        InMemoryProfileStore store;

        [SetUp]
        public void SetUp()
        {
            speed = ScriptableObject.CreateInstance<UpgradeDefinition>(); // defaults: type LaunchPower, 5 levels, cost 20, x1.6
            store = new InMemoryProfileStore();
            service = new ProgressionService(new[] { speed }, store);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(speed);

        [Test]
        public void CannotBuyWithoutCoins()
        {
            Assert.IsFalse(service.TryPurchase(speed));
            Assert.AreEqual(0, service.GetLevel(speed));
        }

        [Test]
        public void PurchaseSpendsCoinsAndRaisesLevel()
        {
            service.AddCoins(100);
            Assert.IsTrue(service.TryPurchase(speed));
            Assert.AreEqual(1, service.GetLevel(speed));
            Assert.AreEqual(80, service.Coins);
        }

        [Test]
        public void CostGrowsWithLevel()
        {
            service.AddCoins(1000);
            int first = service.GetCost(speed);
            service.TryPurchase(speed);
            Assert.Greater(service.GetCost(speed), first);
        }

        [Test]
        public void CannotExceedMaxLevel()
        {
            service.AddCoins(100000);
            for (int i = 0; i < 20; i++) service.TryPurchase(speed);
            Assert.AreEqual(speed.MaxLevel, service.GetLevel(speed));
            Assert.IsFalse(service.CanAfford(speed));
        }

        [Test]
        public void ProgressSurvivesReload()
        {
            service.AddCoins(100);
            service.TryPurchase(speed);

            var reloaded = new ProgressionService(new[] { speed }, store);
            Assert.AreEqual(1, reloaded.GetLevel(speed));
            Assert.AreEqual(80, reloaded.Coins);
        }

        [Test]
        public void BonusScalesWithLevel()
        {
            service.AddCoins(1000);
            service.TryPurchase(speed);
            service.TryPurchase(speed);
            Assert.AreEqual(speed.BonusPerLevel * 2f, service.GetBonus(UpgradeType.LaunchPower), 1e-5f);
        }
    }
}
