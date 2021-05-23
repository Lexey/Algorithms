using System;

namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи ЛП Ax = b, x >= 0, cx -> max симплекс-методом</summary>
    public class IlpProblem : SimplexProblem
    {
        /// <summary>Конструктор</summary>
        public IlpProblem(decimal[][] A, decimal[] b, decimal[] c) : base(A, b, c) { }

        /// <summary>Решает задачу ILP симплекс-методом</summary>
        /// <remarks>Система уже должна быть приведена в вид, в котором строки и столбцы базиса соответствуют передаваемому вектору базисных индексов</remarks>
        /// <returns>Результат вычислений</returns>
        public new SimplexResult Solv()
        {
            for (; ; )
            {
                var r = base.Solv();
                switch (r)
                {
                    case SimplexResult.Infeasible:
                        return r;
                    case SimplexResult.FunctionalUnbound:
                        return r;
                    case SimplexResult.Optimal:
                        // проверка на целочисленность
                        if (!GenerateCuts())
                        {
                            return SimplexResult.Optimal;
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected result");
                }
            }
        }
    }
}
