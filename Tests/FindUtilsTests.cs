using System;
using Algorithms;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class FindUtilsTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test01LbBadFrom()
        {
            var arr = new[] { 0 };
            arr.LowerBound(10, -1, arr.Length, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test02UbBadFrom()
        {
            var arr = new[] { 0 };
            arr.UpperBound(10, -1, arr.Length, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test03LbBadTo()
        {
            var arr = new[] { 0 };
            arr.LowerBound(10, 0, -1, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test04UbBadTo()
        {
            var arr = new[] { 0 };
            arr.UpperBound(10, 0, -1, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test05LbBadTo2()
        {
            var arr = new[] { 0 };
            arr.LowerBound(10, 0, arr.Length + 1, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test06UbBadTo2()
        {
            var arr = new[] { 0 };
            arr.UpperBound(10, 0, arr.Length + 1, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test07LbBadOrder()
        {
            var arr = new[] { 0, 1, 2 };
            arr.LowerBound(10, arr.Length - 1, 0, (x, y) => x - y);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test08UbBadOrder()
        {
            var arr = new[] { 0, 1, 2 };
            arr.UpperBound(10, 2, 1, (x, y) => x - y);
        }

        [Test]
        public void Test09LbEmpty()
        {
            var arr = new[] { 1, 5, 12, 35, 123 };
            var r = arr.LowerBound(10, 0, 0, (x, y) => x - y);
            Assert.AreEqual(0, r);
            r = arr.LowerBound(11, 2, 2, (x, y) => x - y);
            Assert.AreEqual(2, r);
            r = arr.LowerBound(30, arr.Length, arr.Length, (x, y) => x - y);
            Assert.AreEqual(arr.Length, r);
        }

        [Test]
        public void Test10UbEmpty()
        {
            var arr = new[] { 1, 5, 12, 35, 123 };
            var r = arr.UpperBound(10, 0, 0, (x, y) => x - y);
            Assert.AreEqual(0, r);
            r = arr.UpperBound(21, 2, 2, (x, y) => x - y);
            Assert.AreEqual(2, r);
            r = arr.UpperBound(101, arr.Length, arr.Length, (x, y) => x - y);
            Assert.AreEqual(arr.Length, r);
        }

        [Test]
        public void Test11Lb()
        {
            var arr = new[] { 1, 5, 12, 12, 123, 512, 512, 14534 };
            var r = arr.LowerBound(15, 0, 3, (x, y) => x - y);
            Assert.AreEqual(3, r);
            r = arr.LowerBound(5, 1, 4, (x, y) => x - y);
            Assert.AreEqual(1, r);
            r = arr.LowerBound(30000);
            Assert.AreEqual(arr.Length, r);
            r = arr.LowerBound(-1);
            Assert.AreEqual(0, r);
            r = arr.LowerBound(42);
            Assert.AreEqual(4, r);
            r = arr.LowerBound(1002);
            Assert.AreEqual(7, r);
            r = arr.LowerBound(12);
            Assert.AreEqual(2, r);
            r = arr.LowerBound(3);
            Assert.AreEqual(1, r);
        }

        [Test]
        public void Test12Ub()
        {
            var arr = new[] { 1, 5, 12, 12, 123, 512, 512, 14534 };
            var r = arr.UpperBound(15, 0, 3, (x, y) => x - y);
            Assert.AreEqual(3, r);
            r = arr.UpperBound(5, 1, 4, (x, y) => x - y);
            Assert.AreEqual(2, r);
            r = arr.UpperBound(30000);
            Assert.AreEqual(arr.Length, r);
            r = arr.UpperBound(-1);
            Assert.AreEqual(0, r);
            r = arr.UpperBound(1);
            Assert.AreEqual(1, r);
            r = arr.UpperBound(42);
            Assert.AreEqual(4, r);
            r = arr.UpperBound(1002);
            Assert.AreEqual(7, r);
            r = arr.UpperBound(12);
            Assert.AreEqual(4, r);
            r = arr.UpperBound(3);
            Assert.AreEqual(1, r);
            r = arr.UpperBound(14534);
            Assert.AreEqual(arr.Length, r);
        }
    }
}
