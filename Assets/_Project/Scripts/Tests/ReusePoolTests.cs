using Capstone.App.Core;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class ReusePoolTests
    {
        class Item
        {
            public bool Shown;
        }

        static ReusePool<Item> NewPool()
        {
            return new ReusePool<Item>(() => new Item(), i => i.Shown = true, i => i.Shown = false);
        }

        [Test]
        public void AReleasedItemIsReusedNotRecreated()
        {
            ReusePool<Item> pool = NewPool();
            Item first = pool.Get();
            pool.Release(first);
            Item second = pool.Get();
            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.Created);
        }

        [Test]
        public void ThreeDecisionsInARowNeverCreateMoreThanThreeCards()
        {
            ReusePool<Item> pool = NewPool();
            for (int round = 0; round < 5; round++)
            {
                pool.ReleaseAll();
                pool.Get();
                pool.Get();
                pool.Get();
            }

            Assert.AreEqual(3, pool.Created);
            Assert.AreEqual(3, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
        }

        [Test]
        public void HooksShowAndHideItems()
        {
            ReusePool<Item> pool = NewPool();
            Item item = pool.Get();
            Assert.IsTrue(item.Shown);
            pool.Release(item);
            Assert.IsFalse(item.Shown);
        }

        [Test]
        public void ReleasingAnItemTwiceIsIgnored()
        {
            ReusePool<Item> pool = NewPool();
            Item item = pool.Get();
            pool.Release(item);
            pool.Release(item);
            Assert.AreEqual(1, pool.IdleCount);
        }
    }
}
