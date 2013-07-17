using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;

namespace Algorithms.LinearProgramming
{
    /// <summary>Класс для решения задачи ЛП Ax = b, x >= 0, cx -> max симплекс-методом</summary>
    public class SimplexProblem
    {
        /// <summary>Точность вычислений</summary>
        public const decimal Epsilon = 1E-18m;
        /// <summary>Точность расчета значения функционала стартового приближения</summary>
        public const decimal Epsilon2 = 1E-12m;

        /// <summary>Конструктор</summary>
        public SimplexProblem(decimal[][] A, decimal[] b, decimal[] c)
        {
            if (A.Length != b.Length)
            {
                throw new ArgumentException("Dimensions of A and b are incompatible");
            }
            if (A[0].Length != c.Length)
            {
                throw new ArgumentException("Dimensions of A and c are incompatible");
            }
            this.A = A;
            this.b = b;
            this.c = c;
        }

        /// <summary>Матрица системы уравнений Ax=b</summary>
        public decimal[][] A { get; private set; }

        /// <summary>Правая часть системы Ax=b</summary>
        public decimal[] b { get; private set; }

        /// <summary>Вектор целевого функционала (c,x) -> max</summary>
        public decimal[] c { get; private set; }


        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        /// <param name="startBasis">Индексы базисных переменных стартовой точки</param>
        /// <param name="x">Конечная точка</param>
        /// <param name="value">Конечное значение функционала</param>
        /// <returns>Результат вычислений</returns>
        public SimplexResult Solv(int[] startBasis, out decimal[] x, out decimal value)
        {
            int[] tmp;
            return Solv(startBasis, out x, out value, out tmp);
        }

        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        /// <param name="startBasis">Индексы базосных переменных стартовой точки</param>
        /// <param name="x">Конечная точка</param>
        /// <param name="value">Конечное значение функционала</param>
        /// <param name="basisIndices">Индексы базиса оптимума</param>
        /// <returns>Результат вычислений</returns>
        public SimplexResult Solv(int[] startBasis, out decimal[] x, out decimal value, out int[] basisIndices)
        {
            return SolvImpl(startBasis, out x, out value, out basisIndices, false);
        }

        /// <summary>Тоже самое, что и предыдущий метод, но имеет хинт для обхода начальной линеаризации матрицы</summary>
        private SimplexResult SolvImpl(int[] startBasis, out decimal[] x, out decimal value, out int[] basisIndices, bool bypassLinearization)
        {
            if (startBasis.Distinct().Count() != A.Length)
            {
                throw new ArgumentException("Insufficient number of basis columns");
            }
            var rowsNumber = A.Length;
            var columnsNumber = c.Length;
            Log_.DebugFormat("Solving problem of size {0}x{1}", rowsNumber, columnsNumber);

            // будет показывать, какая строка какую базисную переменную выражает через небазисные
            basisIndices = new int[rowsNumber];

            // строим матрицу для симплекс-метода
            // первая строка:    -cTrans
            // остальные строки:    A
            var m = new decimal[rowsNumber + 1][];
            var m0 = new decimal[columnsNumber];
            m[0] = m0;
            for (var i = 0; i < columnsNumber; ++i)
            {
                m0[i] = -c[i];
            }
            for (var i = 0; i < rowsNumber; ++i)
            {
                m[i + 1] = (decimal[])A[i].Clone();
            }

            var r = new decimal[rowsNumber + 1]; //вектор правых частей
            Array.Copy(b, 0, r, 1, rowsNumber);

            //список еще неиспользованных строк
            var unusedRows = new HashSet<int>(Enumerable.Range(1, rowsNumber));
            // в итоге - индексы свободных переменных
            // в начале - индексы всех переменных, ибо нужно пересчтитать матрицу
            var freeHash = new HashSet<int>(Enumerable.Range(0, columnsNumber));
            ++rowsNumber; // в m на одну строку больше
            // перестраиваем симплекс-таблицу в исходное состояние:
            // приводим подматрицу при базисных компонентах к единичной
            foreach (var basisColumn in startBasis)
            {
                freeHash.Remove(basisColumn);
                // Ищем строку, в которой при базисной переменной максимальное значение
                var basisRowIndex = FindMaxRow(m, basisColumn, unusedRows);
                basisIndices[basisRowIndex - 1] = basisColumn;
                var basisRow = m[basisRowIndex];
                var basisValue = basisRow[basisColumn];
                if (basisValue == 0)
                {
                    throw new ArgumentException("Invalid start basis");
                }
                // делим всю строку и правую часть на значение перед базисной переменной
                // то есть делаем 1 перед базисной переменной
                var basisR = r[basisRowIndex];
                if (basisR != 0)
                {
                    basisR /= basisValue;
                    r[basisRowIndex] = basisR;
                }
                basisRow[basisColumn] = 1;
                // обрабатываем только значения в небазисных столбцах и необработанных базисных
                // ибо в обработанных базисных уже нули

                Parallel.ForEach(freeHash, j =>
                {
                    var jValue = basisRow[j];
                    if (jValue == 0)
                    {
                        return;
                    }
                    jValue /= basisValue;
                    basisRow[j] = jValue;
                    // вычитаем строку из остальных
                    for (var k = 0; k < rowsNumber; ++k)
                    {
                        if (k == basisRowIndex)
                        {
                            continue;
                        }
                        var currentRow = m[k];
                        var coeff = currentRow[basisColumn];
                        if (coeff == 0)
                        {
                            continue;
                        }
                        currentRow[j] -= jValue * coeff;
                    }
                });
                // вычитаем значение правой части из остальных
                Parallel.For(1, rowsNumber, k =>
                {
                    if (k == basisRowIndex)
                    {
                        return;
                    }
                    var currentRow = m[k];
                    if (basisR != 0)
                    {
                        var coeff = currentRow[basisColumn];
                        if (coeff != 0)
                        {
                            r[k] -= basisR * coeff;
                        }
                    }
                    currentRow[basisColumn] = 0;
                });
                m0[basisColumn] = 0;
                unusedRows.Remove(basisRowIndex);
            }

            // считаем исходное значение функционала
            // в базисе все небазисные переменные - нули
            // соответственно, значения базисных переменных - это просто правые части
            var val = 0.0m;
            for (var i = 0; i < basisIndices.Length; ++i)
            {
                var v = r[i + 1];
                if (v < -Epsilon) // такого быть не может. Это означает, что исходный базис - не базис
                {
                    throw new ArgumentException("Supplied basis is not a valid basis");
                }
                var index = basisIndices[i];
                val += c[index] * v;
            }
            r[0] = val;
            x = new decimal[columnsNumber];
            value = r[0];

            // оптимизируем
            var iterationsLimit = (rowsNumber - 1) * columnsNumber; //лимит итераций для отсечения зацикливания
            while (--iterationsLimit >= 0)
            {
                var newBasisColumn = FindMinColumn(m);
                if (m0[newBasisColumn] >= -Epsilon2)
                {
                    break; //оптимум
                }
                // вводим новый столбец в базис
                // ищем строку, в которой нужно заменить базисную переменную
                var leadRowIndex = FindLeadRow(m, newBasisColumn, r);
                if (leadRowIndex == -1)
                {
                    return SimplexResult.FunctionalUnbound;
                }
                // обновляем списки небазисных столбцов и индексы базисных столбцов
                freeHash.Remove(newBasisColumn);
                freeHash.Add(basisIndices[leadRowIndex - 1]);
                basisIndices[leadRowIndex - 1] = newBasisColumn;

                var leadRow = m[leadRowIndex];
                var leadValue = leadRow[newBasisColumn];
                var leadR = r[leadRowIndex];
                if (leadR != 0)
                {
                    leadR /= leadValue;
                    r[leadRowIndex] = leadR;
                }
                leadRow[newBasisColumn] = 1;

                // для базисных столбцов считать бестолку. там заведомо нули в ведущей строке(кроме нового, но его мы потом
                // пересчитаем отдельно)
                Parallel.ForEach(freeHash, j =>
                {
                    var jValue = leadRow[j];
                    if (jValue == 0)
                    {
                        return;
                    }
                    jValue /= leadValue;
                    leadRow[j] = jValue;

                    for (var k = 0; k < rowsNumber; ++k)
                    {
                        if (k == leadRowIndex)
                        {
                            continue;
                        }
                        var currentRow = m[k];
                        var coeff = currentRow[newBasisColumn];
                        if (coeff == 0)
                        {
                            continue;
                        }
                        currentRow[j] -= jValue * coeff;
                    }
                });
                Parallel.For(0, rowsNumber, k =>
                {
                    if (k == leadRowIndex)
                    {
                        return;
                    }
                    var currentRow = m[k];
                    if (leadR != 0)
                    {
                        var coeff = currentRow[newBasisColumn];
                        if (coeff != 0)
                        {
                            r[k] -= leadR * coeff;
                            if (k > 0 && r[k] < 0)
                            {
                                if (r[k] < -Epsilon)
                                {
                                    Log_.WarnFormat("Rounding error. Got {0} as a new basis var value"
                                        , r[k]);
                                }
                                r[k] = 0;
                            }
                        }
                    }
                    currentRow[newBasisColumn] = 0;
                });
            }
            value = r[0];
            for (var i = 0; i < b.Length; ++i)
            {
                x[basisIndices[i]] = r[i + 1];
            }
            return iterationsLimit >= 0 ? SimplexResult.Success : SimplexResult.CycleDetected;
        }

        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        /// <param name="x">Конечная точка</param>
        /// <param name="value">Конечное значение функционала</param>
        /// <returns>Результат вычислений</returns>
        public SimplexResult Solv(out decimal[] x, out decimal value)
        {
            int[] basisIndicies;
            return Solv(out x, out value, out basisIndicies);
        }

        /// <summary>Решает задачу ЛП симплекс-методом</summary>
        /// <param name="x">Конечная точка</param>
        /// <param name="value">Конечное значение функционала</param>
        /// <param name="basisIndices">Индексы базиса оптимума</param>
        /// <returns>Результат вычислений</returns>
        public SimplexResult Solv(out decimal[] x, out decimal value, out int[] basisIndices)
        {
            basisIndices = null;
            // инициализация
            x = new decimal[c.Length];
            value = 0;
            // находим стартовую точку
            var fp = new SimplexFeasibilityProblem(A, b);
            int[] startBasis;
            var r = fp.Solv(out startBasis);
            if (r != SimplexResult.Success)
            {
                return r;
            }
            //теперь решаем собственно задачу
            return Solv(startBasis, out x, out value, out basisIndices);
        }

        /// <summary>Поиск минимального коэффициента у функционала</summary>
        private static int FindMinColumn(decimal[][] m)
        {
            var ind = -1;
            var min = decimal.MaxValue;
            var m0 = m[0];
            for (var i = 0; i < m0.Length; ++i)
            {
                var test = m0[i];
                if (test >= min)
                {
                    continue;
                }
                min = test;
                ind = i;
            }
            return ind;
        }


        /// <summary>Ищет ведущую строку</summary>
        /// <param name="m">Симплекс - матрица</param>
        /// <param name="c">Столбец</param>
        /// <param name="r">Вектор правых частей</param>
        /// <returns>Индекс строки для исключения или -1, если нельзя исключить</returns>
        private static int FindLeadRow(decimal[][] m, int c, decimal[] r)
        {
            var ind = -1;
            var min = decimal.MaxValue;
            for (var i = 1; i < m.Length; ++i)
            {
                var v = m[i][c];
                if (v <= Epsilon)
                {
                    continue;
                }
                var test = r[i] / v;
                if (test >= min)
                {
                    continue;
                }
                ind = i;
                min = test;
            }
            return ind;
        }

        /// <summary>Ищет максимальный по модулю эл-т в столбце и возвращает его индекс</summary>
        /// <param name="m">Симплекс - матрица</param>
        /// <param name="c">Столбец</param>
        /// <param name="unused">Таблица неисползованных строк</param>
        /// <returns>Индекс строки максимального элемента</returns>
        private static int FindMaxRow(decimal[][] m, int c, IEnumerable<int> unused)
        {
            var ind = -1;
            var max = decimal.MinValue;
            foreach (var i in unused)
            {
                var test = Math.Abs(m[i][c]);
                if (test <= max)
                {
                    continue;
                }
                max = test;
                ind = i;
            }
            return ind;
        }

        private readonly static ILog Log_ = LogManager.GetCurrentClassLogger();
    }
}
