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
		    var A = new Matrix(new[] { new[] { 1.0, 1 } });
			var b = new Vector(new [] { 1.0 });
            var c = new Vector(new[] { 1.0, 0 });
			var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(1, x[0]);
			Assert.AreEqual(0, x[1]);
            Assert.AreEqual(1, val);
        }

		[Test]
		public void Test02()
		{
			//x + y = 1, y->max
            var A = new Matrix(new[] { new[] { 1.0, 1 } });
            var b = new Vector(new[] { 1.0 });
            var c = new Vector(new[] { 0.0, 1 });
            var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(1, x[1]);
			Assert.AreEqual(0, x[0]);
            Assert.AreEqual(1, val);
        }
		
		[Test]
		public void Test03()
		{
			//x + y = 1, -y->max
            var A = new Matrix(new[] { new[] { 1.0, 1 } });
            var b = new Vector(new[] { 1.0 });
            var c = new Vector(new[] { 0.0, -1 });
			var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(0, x[1]);
			Assert.AreEqual(1, x[0]);
            Assert.AreEqual(0, val);
        }

		[Test]
		public void Test04()
		{
			//x + y + z = 1, x - y = 0, -x->max
            var A = new Matrix(new[]
            {
                new[] { 1.0, 1, 1 },
                new[] { 1.0, -1, 0 }
            });
            var b = new Vector(new[] { 1.0, 0 });
            var c = new Vector(new[] { -1.0, 0, 0 });
			var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(0, x[0]);
			Assert.AreEqual(0, x[1]);
			Assert.AreEqual(1, x[2]);
            Assert.AreEqual(0, val);
        }

		[Test]
		public void Test05()
		{
			// 2x1 + x2 + s1 = 64, x1 + 3x2 + s2 = 72, x2 + s3 = 20, 4x1 + 6x2->max
            var A = new Matrix(new[]
            {
                new[] { 2.0, 1, 1, 0, 0 },
                new[] { 1.0, 3, 0, 1, 0 },
                new[] { 0.0, 1, 0, 0, 1 }
            });
            var b = new Vector(new[] { 64.0, 72, 20 });
            var c = new Vector(new[] { 4.0, 6, 0, 0, 0 });
			var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(24, x[0]);
			Assert.AreEqual(16, x[1]);
			Assert.AreEqual(0, x[2]);
			Assert.AreEqual(0, x[3]);
			Assert.AreEqual(4, x[4]);
            Assert.AreEqual(192, val);
        }

		[Test]
		public void Test06()
		{
			//3x1 + 4x2 - x3 = 6, x1 + 3x2 = 3, 2x1 + x2 + x4 = 4 4x1 + 16x2->max
            var A = new Matrix(new[]
            {
                new[] { 3.0, 4, -1, 0 },
                new[] { 1.0, 3, 0, 0 },
                new[] { 2.0, 1, 0, 1 }
            });
            var b = new Vector(new[] { 6.0, 3, 4 });
            var c = new Vector(new[] { 4.0, 16, 0, 0 });
			var eq = new SimplexProblem(A, b, c);
			Vector x;
			double val;
			var res = eq.Solv(out x, out val);
			Assert.AreEqual(res, SimplexResult.Success);
			Assert.AreEqual(72.0/5, Math.Round(val, 2));
			Assert.AreEqual(6.0 / 5, Math.Round(x[0], 2));
			Assert.AreEqual(3.0 / 5, Math.Round(x[1], 2));
			Assert.AreEqual(0, x[2]);
			Assert.AreEqual(1, Math.Round(x[3], 2));
		}
	}
}
