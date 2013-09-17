using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;

namespace Algorithms.LinearProgramming
{
    /// <summary>Базовый функционал для симплекс-методов</summary>
    public class SimplexProblemBase
    {
        /// <summary>Точность вычислений</summary>
        public const decimal Epsilon = 1E-10m;
        /// <summary>Точность расчета значения функционала стартового приближения</summary>
        public const decimal EpsilonFunctional = 1E-6m;
        /// <summary>Величина возможной ошибки в минус при расчете базисной переменной</summary>
        public const decimal EpsilonBasis = 1E-6m;

        /// <summary>ГСЧ</summary>
        private static readonly Random Rnd_ = new Random();
        /// <summary>Логгер</summary>
        protected readonly static ILog Log_ = LogManager.GetCurrentClassLogger();
        /// <summary>Симплекс-таблица</summary>
        private decimal[][] table_;
        /// <summary>Правая часть для таблицы</summary>
        private decimal[] r_;
        /// <summary>Правило выбора входящей переменной</summary>
        private PivotingRule pivotingRule_;
        /// <summary>Функция выбора входящей переменной</summary>
        private Func<int> pivotingFunc_;
        /// <summary>Небазисные столбцы</summary>
        private HashSet<int> freeColumns_;

        /// <summary>Правило выбора входящей переменной</summary>
        public PivotingRule PivotingRule
        {
            get { return pivotingRule_; }
            set
            {
                switch (value)
                {
                    case PivotingRule.MinCostCoefficent:
                        pivotingFunc_ = FindEnteringColumnMinCost;
                        break;
                    case PivotingRule.Blands:
                        pivotingFunc_ = FindEnteringColumnBlands;
                        break;
                    case PivotingRule.ReverseBlands:
                        pivotingFunc_ = FindEnteringColumnReverseBlands;
                        break;
                    default:
                        throw new ArgumentException("Invalid pivoting rule");
                }
                pivotingRule_ = value;
            }
        }

        /// <summary>Матрица системы уравнений Ax=b</summary>
        public decimal[][] A { get; private set; }

        /// <summary>Правая часть системы Ax=b</summary>
        public decimal[] b { get; private set; }

        /// <summary>Вектор целевого функционала (c,x) -> max</summary>
        protected decimal[] c { get; set; }

        /// <summary>Значение функционала</summary>
        public decimal Value { get { return r_[0]; }}

        /// <summary>Конечная точка</summary>
        public decimal[] Solution
        {
            get
            {
                var x = new decimal[c.Length];
                for (var i = 0; i < Basis.Length; ++i)
                {
                    x[Basis[i]] = r_[i + 1];
                }
                return x;
            }
        }

        /// <summary>Базис финальной точки</summary>
        public int[] Basis { get; private set; }

        protected SimplexProblemBase(decimal[][] A, decimal[] b)
        {
            if (A.Length != b.Length)
            {
                throw new ArgumentException("Dimensions of A and b are incompatible");
            }
            this.A = A;
            this.b = b;
            PivotingRule = PivotingRule.MinCostCoefficent;
        }

        /// <summary>Готовит таблицу для дальнейшей обработки</summary>
        private void PrepareTableAndR(decimal[] extraC)
        {
            // первая строка:    -cTrans
            // остальные строки:    A
            var rowsNumber = A.Length;
            var columnsNumber = c.Length;
            var extraRows = extraC != null ? 2 : 1;
            table_ = new decimal[rowsNumber + extraRows][];
            var m0 = new decimal[columnsNumber];
            table_[0] = m0;
            for (var i = 0; i < columnsNumber; ++i)
            {
                m0[i] = -c[i];
            }
            for (var i = 0; i < rowsNumber; ++i)
            {
                table_[i + 1] = (decimal[])A[i].Clone() ;
            }
            if (extraC != null)
            {
                var extra = new decimal[columnsNumber];
                table_[rowsNumber + 1] = extra;
                for (var i = 0; i < extraC.Length; ++i)
                {
                    extra[i] = -extraC[i];
                }
            }
            r_ = new decimal[rowsNumber + extraRows];
            Array.Copy(b, 0, r_, 1, rowsNumber);
        }

        /// <summary>Устанавливает стартовый базис</summary>
        /// <param name="startBasis">Стартовый базис</param>
        /// <param name="identifyBasis">Нужно ли приводить базисные столбцы к единичной матрице</param>
        private void SetStartBasis(int[] startBasis, bool identifyBasis)
        {
            if (startBasis.Distinct().Count() != A.Length)
            {
                throw new ArgumentException("Insufficient number of basis columns");
            }
            Basis = (int[])startBasis.Clone();
            if (identifyBasis)
            {
                IdentifyTable();
            }
            else
            {
                freeColumns_ = new HashSet<int>(Enumerable.Range(0, c.Length).Except(Basis));
            }
        }

        /// <summary>Собственно решение задачи оптимизации</summary>
        /// <param name="startBasis">Стартовый базис</param>
        /// <param name="extraC">Дополнительный функционал, который будет помещен в последнюю строку таблицы</param>
        /// <returns>Статус решения</returns>
        protected SimplexResult SolvImpl(int[] startBasis, decimal[] extraC)
        {
            PrepareToSolv(startBasis, extraC);
            return ContinueSolv();
        }

        /// <summary>Продолжение оптимизации после подготовки или после поиска базиса</summary>
        protected SimplexResult ContinueSolv()
        {
            CalcFunctionalValue();
            return Optimize();
        }

        /// <summary>Установка стартовой таблицы и базиса</summary>
        /// <param name="startBasis">Стартовый базис</param>
        /// <param name="extraC">Дополнительный функционал, который будет помещен в последнюю строку таблицы</param>
        protected void PrepareToSolv(int[] startBasis, decimal[] extraC)
        {
            PrepareTableAndR(extraC);
            SetStartBasis(startBasis, true);
        }

        /// <summary>Установка начального значения функционала</summary>
        /// <param name="value">Начальное значение</param>
        protected void SetValue(decimal value)
        {
            r_[0] = value;
        }

        /// <summary>Копирует состояние из переданного решения задачи поиска допустимого базиса</summary>
        protected void CopyStateFromFeasibility(SimplexProblemBase src)
        {
            // сохраняем результаты расчетов из подзадачи в текущую задачу
            // таблица
            table_ = src.table_;
            var last = table_.Length - 1;
            table_[0] = table_[last]; // новый m0
            Array.Resize(ref table_, last); // выкидываем более ненужную строку
            for (var i = 0; i < last; ++i)
            {
                Array.Resize(ref table_[i], c.Length);
            }
            r_ = src.r_;
            Array.Resize(ref r_, last);
            SetStartBasis(src.Basis, false);
        }

        /// <summary>Находит стартовый базис для задачи</summary>
        /// <returns>Результат работы симплекс-метода</returns>
        protected SimplexResult SolvFeasibilityImpl()
        {
            // решаем задачу Ax +- y = b, (-K, y) -> max, x >= 0, y >= 0. Если исходная задача имеет решение,
            // то и данная имеет решение, причем ее решение дает точку, удовлетворяющую системе Ax=b и y = 0
            // если в правой части есть отрицательное число, то соответствующую доп. переменную нужно добавить со знаком минус
            // иначе будет косяк со стартовым базисом
            var rowsNumber = A.Length;
            var columnsNumber = A[0].Length;
            var extendedColumnsNumber = columnsNumber + rowsNumber;
            Log_.DebugFormat("Solving a feasibility problem of a size {0}x{1}", rowsNumber, extendedColumnsNumber);
            var startIndicies = new int[rowsNumber]; // стартовый базис
            var b1 = (decimal[])b.Clone();
            var a1 = new decimal[rowsNumber][];
            for (var i = 0; i < rowsNumber; ++i)
            {
                var rowA1 = new decimal[extendedColumnsNumber];
                Array.Copy(A[i], 0, rowA1, 0, columnsNumber);
                a1[i] = rowA1;
                var j = columnsNumber + i;
                if (b1[i] < 0)
                {
                    // инвертируем строку, чтобы потом не заниматься подготовкой базиса   
                    for (var k = 0; k < columnsNumber; ++k)
                    {
                        rowA1[k] = -rowA1[k];
                    }
                    b1[i] = -b1[i];
                }
                a1[i][j] = 1;
                startIndicies[i] = j;
            }
            var c1 = new decimal[extendedColumnsNumber]; // целевой функционал. значения пропишем ниже
            var result = SimplexResult.Optimal;
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
                var eq = new SimplexProblem(a1, b1, c1);
                result = eq.SolvImpl(startIndicies, c);
                if (result == SimplexResult.Optimal)
                {
                    if (eq.Basis.Any(t => t >= columnsNumber))
                    {
                        result = SimplexResult.HullIsEmpty;
                        continue; // остались небазисные переменные
                    }
                    CopyStateFromFeasibility(eq);
                    return SimplexResult.Optimal;
                }
                if (result != SimplexResult.FunctionalUnbound)
                {
                    return result;
                }
                // unbound тоже може может быть результатом ошибки округления. попробуем еще
            }
            return result == SimplexResult.FunctionalUnbound ? SimplexResult.RoundingError : result;
        }

        /// <summary>Оптимизирует функционал</summary>
        private SimplexResult Optimize()
        {
            var r = SimplexResult.Inoptimal;
            var iterationsLimit = A.Length * table_[0].Length; //лимит итераций для отсечения зацикливания
            while (--iterationsLimit >= 0)
            {
                if (DoSingleStep(ref r))
                {
                    return r;
                }
            }
            return iterationsLimit >= 0 ? SimplexResult.Optimal : SimplexResult.CycleDetected;
        }

        /// <summary>Один шаг симплекс-метода</summary>
        /// <param name="result">Результат вычислений, если шаг оказался финальным</param>
        /// <returns>true - финальное состояние, false - можно оптимизировать дальше</returns>
        protected bool DoSingleStep(ref SimplexResult result)
        {
// Ищем столбец с минимальным значением в m0
            var newBasisColumn = FindEnteringColumn();
            if (newBasisColumn == -1)
            {
                //оптимум
                result = SimplexResult.Optimal;
                return true;
            }
            // вводим новый столбец в базис
            // ищем строку, в которой нужно заменить базисную переменную
            var leadRowIndex = FindLeavingRow(newBasisColumn);
            if (leadRowIndex == -1)
            {
                // задача неограничена
                result = SimplexResult.FunctionalUnbound;
                return true;
            }
            // обновляем списки небазисных столбцов и индексы базисных столбцов
            freeColumns_.Remove(newBasisColumn);
            freeColumns_.Add(Basis[leadRowIndex - 1]);
            Basis[leadRowIndex - 1] = newBasisColumn;

            var leadRow = table_[leadRowIndex];
            var leadValue = leadRow[newBasisColumn];
            var leadR = r_[leadRowIndex];
            if (leadValue != 1m)
            {
                if (leadR != 0)
                {
                    leadR /= leadValue;
                    r_[leadRowIndex] = leadR;
                }
                leadRow[newBasisColumn] = 1;

                // для базисных столбцов считать бестолку. там заведомо нули в ведущей строке(кроме нового, но его мы потом
                // пересчитаем отдельно)
                Parallel.ForEach(freeColumns_, j =>
                {
                    var jValue = leadRow[j];
                    if (jValue == 0)
                    {
                        return;
                    }
                    jValue /= leadValue;
                    leadRow[j] = jValue;
                });
            }
            Parallel.For(0, table_.Length, k =>
            {
                if (k == leadRowIndex)
                {
                    return;
                }
                var currentRow = table_[k];
                var coeff = currentRow[newBasisColumn];
                if (coeff == 0)
                {
                    return;
                }
                foreach (var j in freeColumns_)
                {
                    var jValue = leadRow[j];
                    if (jValue == 0)
                    {
                        continue;
                    }
                    currentRow[j] -= jValue * coeff;
                }
                if (leadR != 0 && k < A.Length + 1) // в "экстра M" нет смысла обновлять r
                {
                    var v = r_[k] - leadR * coeff;
                    r_[k] = v;
                    if (k > 0 && v < 0)
                    {
                        if (r_[k] < -EpsilonBasis)
                        {
                            Log_.WarnFormat("Rounding error. Got {0} as a new basis var value"
                                , r_[k]);
                        }
                    }
                }
                currentRow[newBasisColumn] = 0;
            });
            return false;
        }

        /// <summary>Вычисляет исходное значение функционала и помещает его в r[0]</summary>
        private void CalcFunctionalValue()
        {
            // считаем исходное значение функционала
            // в базисе все небазисные переменные - нули
            // соответственно, значения базисных переменных - это просто правые части
            var val = 0.0m;
            for (var i = 0; i < Basis.Length; ++i)
            {
                var v = r_[i + 1];
                if (v < -Epsilon) // такого быть не может. Это означает, что исходный базис - не базис
                {
                    throw new ArgumentException("Supplied basis is not a valid basis");
                }
                var index = Basis[i];
                val += c[index] * v; // подставляем базисные переменные в функционал
            }
            r_[0] = val;
        }

        /// <summary>Приводит базисные столбцы к единичной матрице</summary>
        private void IdentifyTable()
        {
            // в итоге - индексы свободных переменных
            // в начале - индексы всех переменных, ибо нужно пересчтитать матрицу
            var m0 = table_[0];
            freeColumns_ = new HashSet<int>(Enumerable.Range(0, m0.Length));
            // перестраиваем симплекс-таблицу в исходное состояние:
            // приводим подматрицу при базисных компонентах к единичной
            //список еще неиспользованных строк
            var rowsNumber = A.Length;
            for (var i = 0; i < rowsNumber; ++i)
            {
                var basisColumnIndex = Basis[i];
                freeColumns_.Remove(basisColumnIndex);
                // Ищем строку, в которой при базисной переменной максимальное значение
                var basisRowIndex = i + 1;
                var basisRow = table_[basisRowIndex];
                var basisVarCoeffcient = basisRow[basisColumnIndex];
                if (basisVarCoeffcient == 0)
                {
                    // перед базисной переменной в базисной строке не может быть нуля
                    throw new ArgumentException("Invalid start basis");
                }
                var basisR = r_[basisRowIndex];
                if (basisVarCoeffcient != 1m)
                {
                    // делим всю строку и правую часть на значение перед базисной переменной
                    // то есть делаем 1 перед базисной переменной
                    if (basisR != 0)
                    {
                        basisR /= basisVarCoeffcient;
                        r_[basisRowIndex] = basisR;
                    }
                    basisRow[basisColumnIndex] = 1;
                    // обрабатываем только значения в небазисных столбцах и необработанных базисных
                    // ибо в обработанных базисных уже нули
                    // вычитаем строку из остальных (по столбцу j)
                    Parallel.ForEach(freeColumns_, j =>
                    {
                        var jValue = basisRow[j]; // значение в j-ом столбце базисной строки
                        if (jValue == 0)
                        {
                            return;
                        }
                        jValue /= basisVarCoeffcient;
                        basisRow[j] = jValue;
                    });
                }
                Parallel.For(0, table_.Length, k =>
                {
                    if (k == basisRowIndex)
                    {
                        return;
                    }
                    var currentRow = table_[k];
                    var coeff = currentRow[basisColumnIndex];
                        // коэффициент, на который умножается вычитаемая базисная строка
                    if (coeff == 0)
                    {
                        return;
                    }
                    foreach (var j in freeColumns_)
                    {
                        var jValue = basisRow[j]; // значение в j-ом столбце базисной строки
                        if (jValue == 0)
                        {
                            continue;
                        }
                        currentRow[j] -= jValue * coeff;
                    }
                    ;
                    currentRow[basisColumnIndex] = 0;
                    if (k == 0 || basisR == 0) // r[0] модифицировать смысла нет
                    {
                        return;
                    }
                    r_[k] -= basisR * coeff;
                });
                m0[basisColumnIndex] = 0;
            }
        }

        /// <summary>Поиск отрицательного коэффициента в строке функционала по одному из правил</summary>
        protected int FindEnteringColumn()
        {
            return pivotingFunc_();
        }

        /// <summary>Правило поиска входящей переменной по минимальному индексу</summary>
        /// <returns></returns>
        private int FindEnteringColumnMinCost()
        {
            var m0 = table_[0];
            var min = decimal.MaxValue;
            var minIndex = -1;
            for (var i = 0; i < m0.Length; ++i)
            {
                var test = m0[i];
                if (test < min)
                {
                    min = test;
                    minIndex = i;
                }
            }
            if (min >= -EpsilonFunctional)
            {
                return -1;
            }
            return minIndex;
        }

        /// <summary>Правило поиска входящей переменной по минимальному индексу</summary>
        /// <returns></returns>
        private int FindEnteringColumnBlands()
        {
            var m0 = table_[0];
            for (var i = 0; i < m0.Length; ++i)
            {
                var test = m0[i];
                if (test < -EpsilonFunctional)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Правило поиска входящей переменной по максимальному индексу</summary>
        private int FindEnteringColumnReverseBlands()
        {
            var m0 = table_[0];
            for (var i = m0.Length - 1; i >= 0; --i)
            {
                var test = m0[i];
                if (test < -EpsilonFunctional)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Ищет ведущую строку</summary>
        /// <param name="c">Столбец</param>
        /// <returns>Индекс строки для исключения или -1, если нельзя исключить</returns>
        private int FindLeavingRow(int c)
        {
            var ind = -1;
            var min = Decimal.MaxValue;
            for (var i = 1; i < A.Length + 1; ++i)
            {
                var v = table_[i][c];
                if (v <= EpsilonFunctional)
                {
                    continue;
                }
                var test = r_[i] / v;
                if (test >= min)
                {
                    continue;
                }
                ind = i;
                min = test;
            }
            return ind;
        }
    }
}
