using Algorithms;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    [TestFixture]
    public class PermutationsTests
    {
        [Test]
        public void Test01()
        {
            var factorial = 1;
            for (var len = 2; len < 10; ++len)
            {
                factorial *= len;
                var visited = new HashSet<string>();
                var seq = Enumerable.Range('a', len).Select(_ => (char)_).ToList();
                do
                {
                    var s = new string(seq.ToArray());
                    visited.Add(s);
                } while (seq.NextPermutation());
                Assert.That(visited.Count, Is.EqualTo(factorial));
            }
        }

        [Test]
        public void Test02()
        {
            // Дана таблица размером 2×5. В левом верхнем углу записано число 1.
            // Сколькими способами таблицу можно дополнить числами {1,2,3,4,5} так, чтобы
            // 1) в каждой строчке присутствовало каждое из чисел от 1 до 5
            // 2) в каждом столбце все числа были различны?
            // (Пример такого заполнения: первая строчка: 1,2,5,4,3, вторая строчка: 3,5,2,1,4.)
            var r1 = new byte[] { 2, 3, 4, 5 };
            var r2 = new byte[] { 1, 2, 3, 4, 5 };
            var valid = 0;
            do
            {
                do
                {
                    if (r2[0] != 1 && r2[1] != r1[0] && r2[2] != r1[1]
                        && r2[3] != r1[2] && r2[4] != r1[3])
                    {
                        ++valid;
                    }
                } while (r2.NextPermutation());
            } while (r1.NextPermutation());
            Assert.That(valid, Is.EqualTo(1056));
        }
    }
}
