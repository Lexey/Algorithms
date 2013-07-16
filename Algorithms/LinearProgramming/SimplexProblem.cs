using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;

namespace Algorithms.LinearProgramming
{
	/// <summary>Класс для решения задачи ЛП Ax = b, x >= 0, cx -> max симплекс-методом</summary>
	public class SimplexProblem
	{
		/// <summary>Точность вычислений</summary>
		public const double Epsilon = 1E-12;
        /// <summary>Точность расчета значения функционала стартового приближения</summary>
        public const double Epsilon2 = 1E-9;
		
        /// <summary>Конструктор</summary>
		public SimplexProblem(Matrix A, Vector b, Vector c)
		{
			if (A.Rows != b.Count)
			{
                throw new ArgumentException("Dimensions of A and b are incompatible");
			}
			if (A.Columns != c.Count)
			{
                throw new ArgumentException("Dimensions of A and c are incompatible");
			}
			this.A = A;
			this.b = b;
			this.c = c;
		}

		/// <summary>Матрица системы уравнений Ax=b</summary>
		public Matrix A { get; private set; }

		/// <summary>Правая часть системы Ax=b</summary>
		public Vector b { get; private set; }

		/// <summary>Вектор целевого функционала (c,x) -> max</summary>
		public Vector c { get; private set; }


		/// <summary>Решает задачу ЛП симплекс-методом</summary>
		/// <param name="startIndices">Индексы базисных переменных стартовой точки</param>
		/// <param name="x">Конечная точка</param>
		/// <param name="value">Конечное значение функционала</param>
		/// <returns>Результат вычислений</returns>
		public SimplexResult Solv(int[] startIndices, out Vector x, out double value)
		{
			int[] tmp;
			return Solv(startIndices, out x, out value, out tmp);
		}

		/// <summary>Решает задачу ЛП симплекс-методом</summary>
		/// <param name="startIndices">Индексы базосных переменных стартовой точки</param>
		/// <param name="x">Конечная точка</param>
		/// <param name="value">Конечное значение функционала</param>
		/// <param name="basisIndices">Индексы базиса оптимума</param>
		/// <returns>Результат вычислений</returns>
		public SimplexResult Solv(int[] startIndices, out Vector x, out double value, out int[] basisIndices)
		{
            if (startIndices.Distinct().Count() != A.Rows)
            {
                throw new ArgumentException("Insufficient number of basis columns");
            }
            var rowsNumber = A.Rows;
            var columnsNumber = A.Columns;
            Log_.DebugFormat("Solving problem of size {0}x{1}", rowsNumber, columnsNumber);

            // инициализируем массив индексов базисных переменных
            basisIndices = new int[rowsNumber];

            // строим матрицу для симплекс-метода
			// первая строка:    -cTrans
			// остальные строки:    A
			var m = new Matrix(rowsNumber + 1, columnsNumber);
		    var m0 = m[0];
			for (var i = 0; i < columnsNumber; ++i)
			{
                m0[i] = -c[i];
			}
			for (var i = 0; i < rowsNumber; ++i)
			{
				var row1 = A[i];
				var row2 = m[i + 1];
				Array.Copy(row1, row2, columnsNumber);
			}

			var r = new Vector(b.Count + 1); //вектор правых частей
			Array.Copy(((Matrix)b)[0], 0, ((Matrix)r)[0], 1, b.Count);

            //список еще неиспользованных строк
			var unusedRows = new HashSet<int>(Enumerable.Range(1, rowsNumber));
            // в итоге - индексы свободных переменных
            // в начале - индексы всех переменных, ибо нужно пересчтитать матрицу
            var freeHash = new HashSet<int>(Enumerable.Range(0, columnsNumber));
            ++rowsNumber; // в m на одну строку больше
            // перестраиваем симплекс-таблицу в исходное состояние:
            // приводим подматрицу при базисных компонентах к единичной
			foreach (var basisColumn in startIndices)
			{
			    freeHash.Remove(basisColumn);
				var basisRowIndex = FindMaxRow(m, basisColumn, unusedRows);
				basisIndices[basisRowIndex - 1] = basisColumn;
				var basisRow = m[basisRowIndex];
				var basisValue = basisRow[basisColumn];
				var basisR = r[basisRowIndex];
				if (basisR != 0)
				{
					basisR /= basisValue;
					r[basisRowIndex] = basisR;
				}
				basisRow[basisColumn] = 1;
				foreach (var j in freeHash)
				{
					var jValue = basisRow[j];
					if (jValue == 0)
					{
                        continue;
					}
					jValue /= basisValue;
					basisRow[j] = jValue;
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
				}
				for (var k = 1; k < rowsNumber; ++k)
				{
					if (k == basisRowIndex)
					{
                        continue;
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
				}
				m[0][basisColumn] = 0;
				unusedRows.Remove(basisRowIndex);
			}

            // считаем исходное значение функционала
			var val = 0.0; 
			for (var i = 0; i < basisIndices.Length; ++i)
			{
				var index = basisIndices[i];
				val += c[index] * r[i + 1];
			}
			r[0] = val;
			x = new Vector(A.Columns);
			value = r[0];

			// оптимизируем
			var iterationsLimit = A.Rows * A.Columns; //лимит итераций для отсечения зацикливания
			while (--iterationsLimit >= 0)
			{
				var newBasisColumn = FindMinColumn(m);
				if (m[0][newBasisColumn] >= -Epsilon2)
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
				foreach (var j in freeHash)
				{					
					var jValue = leadRow[j];
				    if (jValue == 0)
				    {
				        continue;
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
				}
				for (var k = 0; k < rowsNumber; ++k)
				{
					if (k == leadRowIndex)
					{
                        continue;
					}
					var currentRow = m[k];
					if (leadR != 0)
					{
						var coeff = currentRow[newBasisColumn];
						if (coeff != 0)
						{
                            r[k] -= leadR * coeff;
						}
					}
					currentRow[newBasisColumn] = 0;
				}
			}
			value = r[0];
			for (var i = 0; i < b.Count; ++i)
			{
				x[basisIndices[i]] = r[i + 1];
			}
			return iterationsLimit >= 0 ? SimplexResult.Success : SimplexResult.CycleDetected;
		}

		/// <summary>Решает задачу ЛП симплекс-методом</summary>
		/// <param name="x">Конечная точка</param>
		/// <param name="value">Конечное значение функционала</param>
		/// <returns>Результат вычислений</returns>
		public SimplexResult Solv(out Vector x, out double value)
		{
            // инициализация
            x = new Vector(A.Columns);
            value = 0;
		    int[] basisIndicies;
            // находим стартовую точку
            var fp = new SimplexFeasibilityProblem(A, b);
		    var r = fp.Solv(out basisIndicies);
            if (r != SimplexResult.Success)
            {
                return r;
            }
            //теперь решаем собственно задачу
			return Solv(basisIndicies, out x, out value);
		}

		/// <summary>Поиск минимального коэффициента у функционала</summary>
		private static int FindMinColumn(Matrix m)
		{
			var ind = -1;
			var min = double.MaxValue;
		    var m0 = m[0];
			for (var i = 0; i < m.Columns; ++i)
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
		private static int FindLeadRow(Matrix m, int c, Vector r)
		{
			var ind = -1;
			var min = double.MaxValue;
			for (var i = 1; i < m.Rows; ++i)
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
		private static int FindMaxRow(Matrix m, int c, IEnumerable<int> unused)
		{
			var ind = -1;
			var max = double.MinValue;
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
