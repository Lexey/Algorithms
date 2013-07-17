using System;
using System.Diagnostics;
using System.Linq;

namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи поиска стартовой точки</summary>
    public class SimplexFeasibilityProblem
    {
        private static readonly Random Rnd_ = new Random();

        /// <summary>Конструктор</summary>
        public SimplexFeasibilityProblem(decimal[][] A, decimal[] b)
        {
            if (A.Length != b.Length)
            {
                throw new ArgumentException("Dimensions of A and b are incompatible");
            }
            this.A = A;
            this.b = b;
        }

        /// <summary>Матрица системы уравнений Ax=b</summary>
        public decimal[][] A { get; private set; }

        /// <summary>Правая часть системы Ax=b</summary>
        public decimal[] b { get; private set; }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <param name="basisIndices">Индексы базисных переменных</param>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult Solv(out int[] basisIndices)
        {
            decimal[] x;
            return SolvImpl(out basisIndices, out x);
        }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <param name="basisIndices">Индексы базисных переменных</param>
        /// <param name="x">Значения переменных</param>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult Solv(out int[] basisIndices, out decimal[] x)
        {
            var r = SolvImpl(out basisIndices, out x);
            if (r != SimplexResult.Success)
            {
                return r;
            }
            var length = A[0].Length;
            Array.Resize(ref x, length);
            return SimplexResult.Success;
        }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <param name="basisIndices">Индексы базисных переменных</param>
        /// <param name="x">Значения переменных</param>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult SolvImpl(out int[] basisIndices, out decimal[] x)
        {
            basisIndices = null;
            x = null;
            // решаем задачу Ax +- y = b, (-K, y) -> max, x >= 0, y >= 0. Если исходная задача имеет решение,
            // то и данная имеет решение, причем ее решение дает точку, удовлетворяющую системе Ax=b и y = 0
            // если в правой части есть отрицательное число, то соответствующую доп. переменную нужно добавить со знаком минус
            // иначе будет косяк со стартовым базисом
            var rowsNumber = A.Length;
            var columnsNumber = A[0].Length;
            var a1 = new decimal[rowsNumber][];
            var extendedColumnsNumber = columnsNumber + rowsNumber;
            var startIndicies = new int[rowsNumber]; // стартовый базис
            for (var i = 0; i < rowsNumber; ++i)
            {
                var rowA1 = new decimal[extendedColumnsNumber];
                Array.Copy(A[i], 0, rowA1, 0, columnsNumber);
                a1[i] = rowA1;
                var j = columnsNumber + i;
                a1[i][j] = b[i] < 0 ? -1 : 1;
                startIndicies[i] = j;
            }
            var c1 = new decimal[extendedColumnsNumber]; // целевой функционал. значения пропишем ниже
            var eq = new SimplexProblem(a1, b, c1);
            var result = SimplexResult.Success;
            const int startHalfK = 8;
            var K = startHalfK;
            // TODO: Тут можно попробовать придумать способ найти более грамотные стартовые Ci
            // такие, что -sum (CiAik) < - L < 0. хотя не факт, что это поможет
            // из-за округления могут быть проблемы как с малыми K, так и с большими :(
            while (K < 1000000)
            {
                K *= 2;
                for (var i = 0; i < A.Length; ++i)
                {
                    c1[i + columnsNumber] = -Rnd_.Next(K, K * 2);
                }
                decimal value;
                result = eq.Solv(startIndicies, out x, out value, out basisIndices);
                if (result == SimplexResult.Success && basisIndices.Any(t => t >= columnsNumber))
                {
                    result = SimplexResult.HullIsEmpty;
                    continue; // остались небазисные переменные
                }
                if (result != SimplexResult.FunctionalUnbound)
                {
                    return result;
                }
                // unbound тоже може может быть результатом ошибки округления. попробуем еще
            }
            if (result != SimplexResult.Success)
            {
                return result == SimplexResult.FunctionalUnbound ? SimplexResult.RoundingError : result;
            }
            return SimplexResult.Success;
        }
    }
}
