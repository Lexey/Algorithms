using System;

namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи ЛП Ax = b, x >= 0, cx -> max симплекс-методом</summary>
    public class SimplexProblem : SimplexProblemBase
    {
        /// <summary>Конструктор</summary>
        public SimplexProblem(decimal[][] A, decimal[] b, decimal[] c) : base(A, b)
        {
            if (A[0].Length != c.Length)
            {
                throw new ArgumentException("Dimensions of A and c are incompatible");
            }
            base.c = c;
        }

        /// <summary>Вектор целевого функционала (c,x) -> max</summary>
        public new decimal[] c { get { return base.c; } }

        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        /// <remarks>Система уже должна быть приведена в вид, в котором строки и столбцы базиса соответствуют передаваемому вектору базисных индексов</remarks>
        /// <param name="startBasis">Индексы базисных переменных стартовой точки: [i] - индекс базисного столбца, i - базисная строка</param>
        /// <returns>Результат вычислений</returns>
        public SimplexResult Solv(int[] startBasis)
        {
            return SolvImpl(startBasis, null);
        }


        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        public SimplexResult Solv()
        {
            // находим стартовую точку
            var r = SolvFeasibilityImpl();
            if (r != SimplexResult.Success)
            {
                return r;
            }
            //теперь решаем собственно задачу
            return ContinueSolv();
        }
    }
}
