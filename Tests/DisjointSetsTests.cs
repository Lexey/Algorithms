using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.DisjointSets;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DisjointSetsTests
    {
        private readonly Random random_ = new Random();
        private const int ElementsNumber_ = 10000;
        private readonly List<int> seq_ = Enumerable.Range(0, ElementsNumber_).ToList();

        [Test]
        public void Test01NonGeneric()
        {
            for (var i = 1; i <= ElementsNumber_; i += 1 + i / (10 + random_.Next(0, 10)))
            {
                Console.WriteLine("i = {0}", i);
                var djs = new DisjointSets(ElementsNumber_);
                foreach (var el in RandomShuffle(seq_))
                {
                    djs.Union(el, el % i);
                }
                VerifySets(djs, i);
            }
        }

        [Test]
        public void Test02Generic()
        {
            for (var i = 1; i <= ElementsNumber_; i += 1 + i / (10 + random_.Next(0, 10)))
            {
                Console.WriteLine("i = {0}", i);
                var rs = RandomShuffle(seq_).ToList();
                var djs = new DisjointSets<int>(rs);
                foreach (var el in rs)
                {
                    djs.Union(el, el % i);
                }
                VerifySets(djs, i);
                for (var j = 0; j < ElementsNumber_; ++j)
                {
                    Assert.That(djs[j], Is.EqualTo(rs[j]));
                }
            }
        }

        private void VerifySets(DisjointSetsBase djs, int mod)
        {
            Assert.That(djs.Count, Is.EqualTo(ElementsNumber_));
            Assert.That(djs.SetCount, Is.EqualTo(mod));
            for (var i = 0; i < ElementsNumber_; ++i)
            {
                Assert.That(djs.FindSet(i), Is.EqualTo(djs.FindSet(i % mod)), "i = {0}, mod = {1}", i, mod);
            }
        }

        private IEnumerable<T> RandomShuffle<T>(IEnumerable<T> en)
        {
            return en.OrderBy(x => random_.Next());
        }
    }
}
