namespace Algorithms.LinearProgramming
{
    /// <summary>Правила выбора входящей переменной</summary>
    public enum PivotingRule
    {
        /// <summary>Выбор переменной с минимальным (отрицательным) коэффициентом в целевой функции</summary>
        MinCostCoefficent,
        /// <summary>Выбор переменной (с отрицательным коэффициентов в целевой функции) с максимальным индексом</summary>
        ReverseBlands,
        /// <summary>Выбор переменной (с отрицательным коэффициентов в целевой функции) с минимальным индексом</summary>
        Blands
    }
}
