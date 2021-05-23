
namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи поиска стартовой точки</summary>
    public class SimplexFeasibilityProblem : SimplexProblemBase
    {
        /// <summary>Конструктор</summary>
        public SimplexFeasibilityProblem(decimal[][] A, decimal[] b) : base(A, b) { }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <returns>Результат работы симплекс-метода</returns>
        public SimplexResult Solv()
        {
            return SolvFeasibilityImpl();
        }
    }
}
