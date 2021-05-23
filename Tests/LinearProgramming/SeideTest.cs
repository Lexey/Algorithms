using Algorithms.LinearProgramming;
using NUnit.Framework;
using System;

namespace Tests.LinearProgramming
{
    [TestFixture]
    public class SeideTest
    {
        [Test]
        public void Test01()
        {
            for (var i = 0; i < 10000; ++i)
            {
                // x + y <= 1, x->max
                var A = new[] { new[] { 1m, 1 } };
                var b = new[] { 1m };
                var c = new[] { 1m, 0 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(res, SimplexResult.Optimal);
                Assert.AreEqual(1m, x[0]);
                Assert.AreEqual(0m, x[1]);
                Assert.AreEqual(1m, val);
            }
        }

        [Test]
        public void Test02()
        {
            for (var i = 0; i < 10000; ++i)
            {
                // x + y <= 1, y->max
                var A = new[] { new[] { 1m, 1 } };
                var b = new[] { 1m };
                var c = new[] { 0m, 1 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(res, SimplexResult.Optimal);
                Assert.AreEqual(1m, x[1]);
                Assert.AreEqual(0m, x[0]);
                Assert.AreEqual(1m, val);
            }
        }

        [Test]
        public void Test03()
        {
            for (var i = 0; i < 10000; ++i)
            {
                // x + y <= 1, -y->max
                var A = new[] { new[] { 1m, 1 } };
                var b = new[] { 1m };
                var c = new[] { 0m, -1 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(res, SimplexResult.Optimal);
                Assert.AreEqual(0, x[1]);
                Assert.AreEqual(1, x[0]);
                Assert.AreEqual(0, val);
            }
        }

        [Test]
        public void Test04()
        {
            for (var i = 0; i < 10000; ++i)
            {
                //x+y+z<=1, x-y <= 0, -x-y->max
                var A = new[]
                {
                    new[] { 1m, 1, 1 },
                    new[] { 1m, -1, 0 }
                };
                var b = new[] { 1m, 0 };
                var c = new[] { -1m, -1, 0 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(res, SimplexResult.Optimal);
                Assert.AreEqual(0m, x[0]);
                Assert.AreEqual(0m, x[1]);
                Assert.AreEqual(1m, x[2]);
                Assert.AreEqual(0m, val);
            }
        }

        [Test]
        public void Test05()
        {
            for (var i = 0; i < 10000; ++i)
            {
                // 2x1 + x2 <= 64, x1 + 3x2 <= 72, x2 <= 20, 4x1 + 6x2->max
                var A = new[]
                {
                    new[] { 2m, 1 },
                    new[] { 1m, 3 },
                    new[] { 0m, 1 }
                };
                var b = new[] { 64m, 72, 20 };
                var c = new[] { 4m, 6 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(res, SimplexResult.Optimal);
                Assert.AreEqual(24m, x[0]);
                Assert.AreEqual(16m, x[1]);
                Assert.AreEqual(192m, val);
            }
        }

        [Test]
        public void Test06()
        {
            for (var i = 0; i < 10000; ++i)
            {
                //-3x1 - 4x2 <= -6, x1 + 3x2 - y <= 3, 2x1 + x2 <= 4, y <= 0 4x1 + 16x2->max
                var A = new[]
                {
                    new[] { -3m, -4, 0 },
                    new[] { 1m, 3, -1 },
                    new[] { 2m, 1, 0 },
                    new[] { 0m, 0, 1 }
                };
                var b = new[] { -6m, 3, 4, 0 };
                var c = new[] { 4m, 16, 0 };
                var eq = new SeideProblem(A, b, c);
                decimal[] x;
                decimal val;
                var res = eq.Solv(out x, out val);
                Assert.AreEqual(SimplexResult.Optimal, res);
                Assert.AreEqual(6m / 5, Math.Round(x[0], 2));
                Assert.AreEqual(3m / 5, Math.Round(x[1], 2));
                Assert.AreEqual(0m, x[2]);
                Assert.AreEqual(72m / 5, Math.Round(val, 2));
            }
        }

        // Создает задачу LP по заданным точкам
        // тест базируется на тестовом примере задачи 3 второго раунда Google Code Jam
        // есть N кораблей в положительном подпространстве (x,y,z)
        // у каждого корабля есть приемник. Требуемая мощность передатчика P, установленного в (X,Y,Z),
        // при которой корабль i услышыт передачу, удовлетворяет неравенству:
        // |Xi - X| + |Yi - Y| + |Zi - Z| <= P
        // нужно найти минимальную мощность передатчика (при произвольном выборе его координат)
        private static SeideProblem CreateLP(int[][] points, double[] p)
        {
            var n = p.Length;
            var dp = new decimal[n];
            var dx = new decimal[n]; // xi / pi
            var dy = new decimal[n]; // yi / pi
            var dz = new decimal[n]; // zi / pi
            for (var i = 0; i < n; ++i)
            {
                var v = 1 / (decimal)p[i];
                dp[i] = v;
                dx[i] = points[i][0] * v; dy[i] = points[i][1] * v; dz[i] = points[i][2] * v;
            }

            // исходная задача эквивалентна задаче линейного программирования
            // -p - > max
            // -P - (-1)^(k/4)*X*dPi - (-1)^(k/2)*Y*dPi - (-1)^k*Z*dPi <= -(-1)^(k/4)dXi - (-1)^(k/2)dYi - (-1)^kdZi
            // i = 0...n-1, k = 0...7, P,X,Y,Z >= 0
            // 8n неравенсв, 4 переменных

            // заполняем матрицу A и вектор b
            // Au=b, u = (P, x, y, z)
            var A = new decimal[8 * n][];
            var b = new decimal[8 * n];

            var xsign = new[] { 1, 1, 1, 1, -1, -1, -1, -1 };
            var ysign = new[] { 1, 1, -1, -1, 1, 1, -1, -1 };
            var zsign = new[] { 1, -1, 1, -1, 1, -1, 1, -1 };

            for (var i = 0; i < n; ++i)
            {
                for (var k = 0; k < 8; ++k)
                {
                    var ki = 8 * i + k;
                    var row = new decimal[4];
                    A[ki] = row;
                    row[0] = -1; //p
                    row[1] = -dp[i] * xsign[k];
                    row[2] = -dp[i] * ysign[k];
                    row[3] = -dp[i] * zsign[k]; //x,y,z
                    b[ki] = -xsign[k] * dx[i] - ysign[k] * dy[i] - zsign[k] * dz[i];
                }
            }

            // функционал -p -> max
            var c = new decimal[4];
            c[0] = -1;

            return new SeideProblem(A, b, c);
        }

        [Test]
        public void Test07()
        {
            //0 0 0 1
            //1 2 0 1
            //3 4 0 1
            //2 1 0 1
            var points = new[]
            {
                new[] { 0, 0, 0 },
                new[] { 1, 2, 0 },
                new[] { 3, 4, 0 },
                new[] { 2, 1, 0 }
            };
            var p = new double[] { 1, 1, 1, 1 };
            var eq = CreateLP(points, p);
            for (var t = 0; t < 5000; ++t)
            {
                decimal result;
                decimal[] opt; //финальный результат
                var rs = eq.Solv(out opt, out result);
                Assert.AreEqual(SimplexResult.Optimal, rs);
                Assert.AreEqual(3.5m, Math.Round(-result, 6));
            }
        }

        [Test]
        public void Test08()
        {
            // 1 0 0 1
            // 2 1 1 4
            // 3 2 3 2
            var points = new[]
            {
                new[] { 1, 0, 0 },
                new[] { 2, 1, 1 },
                new[] { 3, 2, 3 },
            };
            var p = new double[] { 1, 4, 2 };
            var eq = CreateLP(points, p);
            for (var t = 0; t < 5000; ++t)
            {
                decimal result;
                decimal[] opt; //финальный результат
                var rs = eq.Solv(out opt, out result);
                Assert.AreEqual(SimplexResult.Optimal, rs);
                Assert.AreEqual(2.333333m, Math.Round(-result, 6));
            }
        }

        [Test]
        public void Test09()
        {
            // 10
            // 749709 772910 278417 6
            // 208544 311102 354951 9
            // 698123 952054 988819 1
            // 252486 827293 503163 2
            // 730571 757552 677587 8
            // 492834 423148 364526 8
            // 746326 533367 349434 9
            // 896852 717974 338577 10
            // 36714 843765 227839 11
            // 862207 802188 940476 6
            var points = new[]
            {
                new[] { 749709, 772910, 278417 },
                new[] { 208544, 311102, 354951 },
                new[] { 698123, 952054, 988819 },
                new[] { 252486, 827293, 503163 },
                new[] { 730571, 757552, 677587 },
                new[] { 492834, 423148, 364526 },
                new[] { 746326, 533367, 349434 },
                new[] { 896852, 717974, 338577 },
                new[] { 36714, 843765, 227839 },
                new[] { 862207, 802188, 940476 },
            };
            var p = new double[] { 6, 9, 1, 2, 8, 8, 9, 10, 11, 6 };
            var eq = CreateLP(points, p);
            for (var t = 0; t < 5000; ++t)
            {
                decimal result;
                decimal[] opt; //финальный результат
                var rs = eq.Solv(out opt, out result);
                Assert.AreEqual(SimplexResult.Optimal, rs);
                Assert.AreEqual(352018m, Math.Round(-result, 6));
            }
        }
    }
}
