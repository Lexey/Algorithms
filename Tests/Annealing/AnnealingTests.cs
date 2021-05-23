using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Tests.Annealing
{
    [TestFixture]
    public class AnnealingTests
    {
        [Test]
        public void Test01()
        {
            ushort n = 4;
            for (var i = 0; i < 10; ++i)
            {
                Console.WriteLine("Calculating queens for size {0}", n);
                var a = new QueensAnnealing(n);
                Assert.IsTrue(a.Solve());
                ValidateSolution(n, a.CurrentPoint.Board);
                n *= 2;
            }
        }

        [Test]
        public void Test02()
        {
            var a = new QueensAnnealing(1000);
            var sw = Stopwatch.StartNew();
            Assert.IsTrue(a.Solve());
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            ValidateSolution(1000, a.CurrentPoint.Board);
        }

        private void ValidateSolution(int size, ushort[] r)
        {
            Assert.That(r.Length, Is.EqualTo(size), "Wrong solution size");
            for (var i = 0; i < size; ++i)
            {
                for (var j = i + 1; j < size; ++j)
                {
                    Assert.That(r[i], Is.Not.EqualTo(r[j])
                                , "The same row {0} at columns {1} and {2}", r[i], i, j);
                    var d = j - i;
                    Assert.That(Math.Abs(r[j] - r[i]), Is.Not.EqualTo(d)
                                , "The same diagonal at {0}:{1} and {2}:{3}", i, r[i], j, r[j]);
                }
            }
        }
    }
}