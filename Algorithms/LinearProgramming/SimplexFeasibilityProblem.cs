using System;
using System.Linq;

namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи поиска стартовой точки</summary>
    public class SimplexFeasibilityProblem
    {
        /// <summary>Конструктор</summary>
        public SimplexFeasibilityProblem(Matrix A, Vector b)
        {
            if (A.Rows != b.Count)
            {
                throw new ArgumentException("Dimensions of A and b are incompatible");
            }
            this.A = A;
            this.b = b;
        }

        /// <summary>Матрица системы уравнений Ax=b</summary>
        public Matrix A { get; private set; }

        /// <summary>Правая часть системы Ax=b</summary>
        public Vector b { get; private set; }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <param name="basisIndices">Индексы базисных переменных</param>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult Solv(out int[] basisIndices)
        {
            Vector x;
            return Solv(out basisIndices, out x);
        }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <param name="basisIndices">Индексы базисных переменных</param>
        /// <param name="x">Значения переменных</param>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult Solv(out int[] basisIndices, out Vector x)
        {
            basisIndices = null;
            x = null;
            // решаем задачу Ax +- y = b, (-K, y) -> max, x >= 0, y >= 0. Если исходная задача имеет решение,
            // то и данная имеет решение, причем ее решение дает точку, удовлетворяющую системе Ax=b и y = 0
            // если в правой части есть отрицательное число, то соответствующую доп. переменную нужно добавить со знаком минус
            // иначе будет косяк со стартовым базисом
            var rowsNumber = A.Rows;
            var columnsNumber = A.Columns;
            var A1 = new Matrix(rowsNumber, columnsNumber + rowsNumber);
            var startIndicies = new int[A.Rows]; // стартовый базис
            for (var i = 0; i < rowsNumber; ++i)
            {
                var ra1 = A1[i];
                var ra = A[i];
                Array.Copy(ra, 0, ra1, 0, columnsNumber);
                var j = columnsNumber + i;
                A1[i][j] = b[i] < 0 ? -1 : 1;
                startIndicies[i] = j;
            }
            var c1 = new Vector(rowsNumber + columnsNumber); // целевой функционал. значения пропишем ниже
            var eq = new SimplexProblem(A1, b, c1);
            var result = SimplexResult.Success;
            const double startHalfK = -1;
            var K = startHalfK;
            // TODO: Тут можно попробовать придумать способ найти более грамотные стартовые Ci
            // такие, что -sum (CiAik) < - L < 0. хотя не факт, что это поможет
            // из-за округления могут быть проблемы как с малыми K, так и с большими :(
            for (var iter = 0; iter < 100; ++iter) //максимальное число итераций
            {
                K *= 2;
                for (var i = 0; i < A.Rows; ++i)
                {
                    c1[i + columnsNumber] = K;
                }
                double value;
                result = eq.Solv(startIndicies, out x, out value, out basisIndices);
                if (result != SimplexResult.FunctionalUnbound)
                {
                    break;
                }
                // unbound невозможен по смыслу задачи. он возможен только из-за ошибок вычислений.
                // пытаемся его подавить, увеличив коэффициенты
            }
            if (result != SimplexResult.Success)
            {
                return result == SimplexResult.FunctionalUnbound ? SimplexResult.RoundingError : result;
            }
            // может так оказаться, что в оптимальном решении вспомогательной задачи
            // остались ненулевые вспомогательные переменные. Это означает, что исходная задача не имеет решения
            if (basisIndices.Any(t => t >= columnsNumber))
            {
                return SimplexResult.HullIsEmpty;
            }
            return SimplexResult.Success;
        }
    }
}
