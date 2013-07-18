using System;
using Algorithms;
using Algorithms.LinearProgramming;
using NUnit.Framework;

namespace Tests.LinearProgramming
{
	[TestFixture]
	public class SimplexTest
	{
		[Test]
		public void Test01()
		{
			// x + y = 1, x->max
		    var A = new[] { new[] { 1m, 1 } };
			var b = new [] { 1m };
            var c = new[] { 1m, 0 };
			var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
			Assert.That(res, Is.EqualTo(SimplexResult.Success));
		    var x = eq.Solution;
			Assert.That(x[0], Is.EqualTo(1));
            Assert.That(x[1], Is.EqualTo(0));
            Assert.That(eq.Value, Is.EqualTo(1m));
        }

		[Test]
		public void Test02()
		{
			//x + y = 1, y->max
            var A = new[] { new[] { 1m, 1 } };
            var b = new[] { 1m };
            var c = new[] { 0m, 1 };
            var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
			Assert.That(res, Is.EqualTo(SimplexResult.Success));
		    var x = eq.Solution;
            Assert.That(x[0], Is.EqualTo(0));
			Assert.That(x[1], Is.EqualTo(1));
            Assert.That(eq.Value, Is.EqualTo(1m));
        }
		
		[Test]
		public void Test03()
		{
			//x + y = 1, -y->max
            var A = new[] { new[] { 1m, 1 } };
            var b = new[] { 1m };
            var c = new[] { 0m, -1 };
			var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
            Assert.That(res, Is.EqualTo(SimplexResult.Success));
            var x = eq.Solution;
            Assert.That(x[0], Is.EqualTo(1));
            Assert.That(x[1], Is.EqualTo(0));
            Assert.That(eq.Value, Is.EqualTo(0m));
        }

		[Test]
		public void Test04()
		{
			//x + y + z = 1, x - y = 0, -x->max
            var A = new[]
            {
                new[] { 1m, 1, 1 },
                new[] { 1m, -1, 0 }
            };
            var b = new[] { 1m, 0 };
            var c = new[] { -1m, 0, 0 };
			var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
            Assert.That(res, Is.EqualTo(SimplexResult.Success));
            var x = eq.Solution;
            Assert.That(x[0], Is.EqualTo(0));
            Assert.That(x[1], Is.EqualTo(0));
            Assert.That(x[2], Is.EqualTo(1));
            Assert.That(eq.Value, Is.EqualTo(0m));
        }

		[Test]
		public void Test05()
		{
			// 2x1 + x2 + s1 = 64, x1 + 3x2 + s2 = 72, x2 + s3 = 20, 4x1 + 6x2->max
            var A = new[]
            {
                new[] { 2m, 1, 1, 0, 0 },
                new[] { 1m, 3, 0, 1, 0 },
                new[] { 0m, 1, 0, 0, 1 }
            };
            var b = new[] { 64m, 72, 20 };
            var c = new[] { 4m, 6, 0, 0, 0 };
			var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
            Assert.That(res, Is.EqualTo(SimplexResult.Success));
            var x = eq.Solution;
            Assert.That(x[0], Is.EqualTo(24m));
            Assert.That(x[1], Is.EqualTo(16m));
            Assert.That(x[2], Is.EqualTo(0m));
            Assert.That(x[3], Is.EqualTo(0m));
            Assert.That(x[4], Is.EqualTo(4m));
            Assert.That(eq.Value, Is.EqualTo(192m));
        }

		[Test]
		public void Test06()
		{
			//3x1 + 4x2 - x3 = 6, x1 + 3x2 = 3, 2x1 + x2 + x4 = 4 4x1 + 16x2->max
            var A = new[]
            {
                new[] { 3m, 4, -1, 0 },
                new[] { 1m, 3, 0, 0 },
                new[] { 2m, 1, 0, 1 }
            };
            var b = new[] { 6m, 3, 4 };
            var c = new[] { 4m, 16, 0, 0 };
			var eq = new SimplexProblem(A, b, c);
			var res = eq.Solv();
            Assert.That(res, Is.EqualTo(SimplexResult.Success));
            var x = eq.Solution;
            Assert.That(Math.Round(x[0], 2), Is.EqualTo(6m / 5m));
            Assert.That(Math.Round(x[1], 2), Is.EqualTo(3m / 5m));
            Assert.That(x[2], Is.EqualTo(0m));
            Assert.That(Math.Round(x[3], 2), Is.EqualTo(1m));
            Assert.That(Math.Round(eq.Value, 2), Is.EqualTo(72m / 5m));
		}
	}
}
