using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common.Logging;

namespace Algorithms.LinearProgramming
{
	/// <summary>Решение задачи LP Ax &lt;= b, cx -> max алгоритмом Seide (см. 9.10 в книге Randomized Algorithms)
	/// ожидание сложности O(nd!), n - число неравенств, d - число базисных переменных
	/// Хорош для решения задач, где ограничений гораздо больше, чем переменных
	/// </summary>
	public class SeideProblem
	{
        public SeideProblem(decimal[][] A, decimal[] b, decimal[] c)
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

		public decimal[][] A { get; private set; }

		public decimal[] b { get; private set; }

        public decimal[] c { get; private set; }

		/// <summary>
		/// Решает задачу LP алгоритмом Seide
		/// Для финального вычисления использует Симплекс - метод
		/// </summary>
		/// <param name="x">Оптимальная точка</param>
		/// <param name="value">Оптимальное значение функционала</param>
		/// <returns></returns>
		public SimplexResult Solv(out decimal[] x, out decimal value)
		{
            // инициализация результата
			x = null;
		    var rowsNumber = A.Length;
			var a = new decimal[rowsNumber][];
			for (var i = 0; i < rowsNumber; ++i)
			{
				a[i] = A[i];
			}
			currentState_.SetState(a, b, c);

			var rnd = new Random();
			states_ = new Stack<State>(); //стек состояний, чтобы избавиться от явной рекурсии
			//есть лишние ограничения, которые нужно исключить
			for (;;)
			{
#region симуляция рекурсивных вызовов
				var rows = currentState_.Rows;
				var columns = currentState_.Columns;
				if (rows <= columns || rows - columns < 3)
				{
					// здесь у нас остались только базисные ограничения (или избыток мал (<3))
					// просто решаем задачу симлекс-методом
					// добавляем вспомогательные переменные, чтобы превратить неравенства в равенства
                    var eq = BuildSimplexProblem(currentState_);
                    Log_.DebugFormat("Solving subproblem of size {0}x{1}", eq.A.Length, eq.c.Length);
                    decimal[] x1;
					var result = eq.Solv(out x1, out value);
					if (result != SimplexResult.Success)
					{
						while (result == SimplexResult.FunctionalUnbound)
						{
							// делать нечего. придется возвращать предыдущие удаленные ограничения поштучно
							// и решать задачу с ними
							// если к unbound привело снятие самого первого ограничения,
							// то нам зверски не повезло
							// может быть есть какой-то более грамотный путь проверить ограниченность
							// задачи с данным ограничением, но пока я его не придумал
							RollbackStates(false);
							if (states_.Count == 0)
							{
                                return SimplexResult.FunctionalUnbound;
							}
							currentState_ = states_.Pop().CurrentState;
							rows = currentState_.Rows;
							columns = currentState_.Columns;
							eq = BuildSimplexProblem(currentState_);
                            Log_.DebugFormat("Solving subproblem of size {0}x{1}", eq.A.Length, eq.c.Length);
							result = eq.Solv(out x1, out value);
						}
						if (result != SimplexResult.Success)
						{
							// если случился empty, то нужно возвращаться до состояния перед добавлением очередного ограничения
							if (result != SimplexResult.HullIsEmpty)
							{
                                return result;
							}
							RollbackStates(true);
							if (states_.Count == 0)
							{
                                //увы, исходная задача неразрешима
                                return SimplexResult.HullIsEmpty;
							}
							rows = currentState_.Rows;
							columns = currentState_.Columns;
						}
					}
					if (result == SimplexResult.Success)
					{
						// убираем лишние переменные
						x = new decimal[columns];
						Array.Copy(x1, x, columns);
						// возвращаемся на уровень выше
					}
					while (states_.Count > 0)
					{
#region возврат вверх по "стеку"
						var previousState = states_.Pop();
						var removedRowIndex = previousState.RemovedRowIndex;

						var removedColumnIndex = previousState.RemovedColumnIndex;
						if (removedColumnIndex < 0) //ранее удаляли строку
						{
#region удаляли строку
							var removedRow = previousState.NewState.RemovedRow;
							// проверяем, нарушает ли решение удаленное ограничение
							var val = 0.0m;
							if (result == SimplexResult.Success)
							{
								for (var i = 0; i < columns; ++i)
								{
                                    val += removedRow[i] * x[i];
								}
							}
							if (result != SimplexResult.Success || val - previousState.NewState.RemovedB > SimplexProblem.Epsilon)
							{
								// ограничение нарушено
								// удаляем одну из переменных из неравенств, превратив удаленное ранее неравенство в равенство
								// и добавив ограничение на положительность удаленной переменной
								// (иначе легко огребаем неограниченность функционала)
								var maxValue = Math.Abs(removedRow[0]);
								var columnIndexToRemove = 0;									
								for (var i = 1; i < columns; ++i)
								{
									var testValue = Math.Abs(removedRow[i]);
									if (testValue > maxValue)
									{
										maxValue = testValue;
										columnIndexToRemove = i;
									}
								}
								var tempRow = new decimal[columns];
								Array.Copy(removedRow, tempRow, columns);
								removedRow = tempRow;
                                if (maxValue < SimplexProblem.Epsilon)
                                {
                                    // тут возможны 3 варианта:
                                    // 1) result == Success. Значит правая часть < 0 и условие невыполнимо.
                                    // система с данным ограничением несовместна. нужно откручивать предыдущее закрепленное ограничение
                                    // 2) result != Success и правая часть < 0. условие невыполнимо
                                    // нужно откручивать предыдущее закрепленное ограничение
                                    // 3) правая часть >= 0. условие выполняется автоматом,
                                    // a) result == FunctionalUnbound нужно откручивать до предыдущего снятого ограничения
                                    // b) result == HullIsEmpty. Ошибкой уже не считается, т.к. мы уже сняли одно ограничение,
                                    // которое возможно ее вызвало. нужно вернуться на предыдущее снятое ограничение
                                    result = result == SimplexResult.Success || previousState.NewState.RemovedB < 0
                                            ? SimplexResult.HullIsEmpty : SimplexResult.FunctionalUnbound;
                                    RollbackStates(result == SimplexResult.HullIsEmpty);
                                    if (states_.Count == 0)
                                    {
                                        return result; // не судьба
                                    }
                                    rows = currentState_.Rows;
                                    columns = currentState_.Columns;
                                    continue; // переход на открутку на предыдущий уровень
                                }
								maxValue = removedRow[columnIndexToRemove]; //теперь уже со знаком
								var bLeadValue = previousState.NewState.RemovedB / maxValue;
                                if (columns > 1)
                                {
                                    for (var j = 0; j < columns; ++j)
                                    {
                                        if (j == columnIndexToRemove)
                                        {
                                            continue;
                                        }
                                        var jValue = removedRow[j];
                                        if (jValue != 0)
                                        {
                                            jValue /= maxValue;
                                            removedRow[j] = jValue;
                                        }
                                    }
                                    var newA = new decimal[rows + 1][];
                                    var newB = new decimal[rows + 1];
                                    for (var i = 0; i < rows; ++i)
                                    {
                                        var newRow = new decimal[columns - 1];
                                        var row = currentState_.A[i];
                                        var rowLeadValue = row[columnIndexToRemove];
                                        var currentColumn = 0;
                                        for (var j = 0; j < columns; ++j)
                                        {
                                            if (j == columnIndexToRemove)
                                            {
                                                continue;
                                            }
                                            var rcValue = row[j];
                                            if (rowLeadValue != 0)
                                            {
                                                var jValue = removedRow[j];
                                                if (jValue != 0)
                                                {
                                                    rcValue -= jValue * rowLeadValue;
                                                }
                                            }
                                            newRow[currentColumn++] = rcValue;
                                        }
                                        newA[i] = newRow;
                                        var bValue = currentState_.b[i];
                                        if (bLeadValue != 0 && rowLeadValue != 0)
                                        {
                                            bValue -= bLeadValue * rowLeadValue;
                                        }
                                        newB[i] = bValue;
                                    }
                                    var newC = new decimal[columns - 1];
                                    var cLeadValue = currentState_.c[columnIndexToRemove];
                                    var newLastRow = new decimal[columns - 1];
                                    var newColumnIndex = 0;
                                    for (var j = 0; j < columns; ++j)
                                    {
                                        if (j == columnIndexToRemove)
                                        {
                                            continue;
                                        }
                                        var cValue = currentState_.c[j];
                                        if (cLeadValue != 0)
                                        {
                                            var jValue = removedRow[j];
                                            if (jValue != 0)
                                            {
                                                cValue -= jValue * cLeadValue;
                                            }
                                        }
                                        newC[newColumnIndex] = cValue;
                                        // новое ограничение на неотрицательность удаленной переменной
                                        newLastRow[newColumnIndex++] = removedRow[j];
                                    }
                                    newA[rows] = newLastRow;
                                    newB[rows] = bLeadValue;
                                    var newState = new LazyState();
                                    newState.SetState(newA, newB, newC);
                                    states_.Push(new State(previousState.CurrentState.Clone()
                                        , removedRowIndex, columnIndexToRemove
                                        , newState.Clone()));
                                    currentState_ = newState;
                                    break; // переход на внешний уровень for...
                                }
							    // значение считается "в лоб". Это bLeadValue
							    // проверяем на неотрицательность:
							    if (bLeadValue < 0) //система несовместна
							    {
							        // !!! здесь мы сами зафиксировали ограничение, но не добавляли его в стек
							        // поэтому оно уже откатилось. нужно только найти предыдущую точку
							        // где было отброшено ограничение
							        RollbackStates(false);
							        if (states_.Count == 0)
							        {
                                        return SimplexResult.HullIsEmpty; // не судьба
							        }
							        result = SimplexResult.FunctionalUnbound;
							        rows = currentState_.Rows;
							        columns = currentState_.Columns;
							        continue; // переход на открутку на предыдущий уровень
							    }
							    result = SimplexResult.Success;
							    x = new decimal[1];
							    x[0] = bLeadValue;
							    value = bLeadValue != 0 ? currentState_.c[0] * bLeadValue : 0;
							}
							// else //ограничение не нарушено. просто возвращаемся на предыдущий уровень
#endregion удаляли строку
						}
						else
						{
#region удаляли переменную
							// до этого удаляли переменную
							// вычислим ее, новый вектор x и новое значение функционала
							var removedRow = previousState.CurrentState.A[removedRowIndex];
							var newX = new decimal[columns + 1];
							var xRemoved = previousState.CurrentState.b[removedRowIndex];
							decimal newVal = 0;
							var xIndex = 0;
							for (var i = 0; i < columns + 1; ++i)
							{
								if (i == removedColumnIndex)
								{
                                    continue;
								}
								var xVal = x[xIndex++];
								newX[i] = xVal;
								xRemoved -= removedRow[i] * xVal;
								newVal += previousState.CurrentState.c[i] * xVal;
							}
							xRemoved /= removedRow[removedColumnIndex];

							newX[removedColumnIndex] = xRemoved;
							newVal += previousState.CurrentState.c[removedColumnIndex] * xRemoved;
							x = newX;
							value = newVal;
#endregion удаляли переменную
						}
						currentState_ = previousState.CurrentState;
						rows = currentState_.Rows;
						columns = currentState_.Columns;

						// возвращаемся на предыдущий уровень
#endregion возврат вверх по "стеку"
					}
					
					if (states_.Count == 0)
					{
                        return result;
					}
					// переходим к следующей вложенной итерации
				}
				else
				{
					// рандомно выбираем ограничение для исключения
					var rowIndexToDelete = rnd.Next(rows);
					var oldState = currentState_.Clone();
					currentState_.UpdateState(rowIndexToDelete);
					states_.Push(new State(oldState, rowIndexToDelete, -1
						, currentState_.Clone()));
				}
#endregion симуляция рекурсивных вызовов
			}
		}

        /// <summary>Создает задачу LP для симплекс-метода по заданному состоянию</summary>
        private SimplexProblem BuildSimplexProblem(LazyState state)
        {
            var rows = state.Rows;
            var columns = state.Columns;
            var extendedColumns = columns + rows;
            var A1 = new decimal[rows][];
            for (var i = 0; i < rows; ++i)
            {
                var srcRow = state.A[i];
                var tgtRow = new decimal[extendedColumns];
                Array.Copy(srcRow, tgtRow, columns);
                A1[i] = tgtRow;
                tgtRow[columns + i] = 1;
            }
            var c1 = new decimal[extendedColumns];
            Array.Copy(state.c, c1, columns);
            return new SimplexProblem(A1, state.b, c1);
        }

		/// <summary>Откручивает состояния до ближайшего снятого ограничения</summary>
		private void RollbackStates(bool resultEmpty)
		{
			while (states_.Count > 0)
			{
				var st = states_.Peek();
				if (resultEmpty)
				{
					// нужно выбрать в том числе и ближайшее
					// закрепленное ограничение
					if (st.RemovedColumnIndex >= 0)
					{
                        resultEmpty = false; // после того, как его выбрали, ищем следующее удаленное
					}
				}
				else
				{
					// останавливаемся на первом удаленном ограничении
					if (st.RemovedColumnIndex == -1)
					{
                        // здесь нужно восстановить текущий стейт по тому состоянию, к "после которого" возвращаемся
                        // ибо при откручивании вверх по переполнению возможно,
                        // что текущий state будет не соответствовать тому, который был после перехода
                        currentState_ = st.NewState;
                        break;
					}
				}
				states_.Pop();
			}
		}
#region поля
		/// <summary>Текущее состояние</summary>
		LazyState currentState_ = new LazyState();
		/// <summary>Стек состояний</summary>
		Stack<State> states_;

	    private static readonly ILog Log_ = LogManager.GetCurrentClassLogger();

	    #endregion поля
	}

#region States
    internal class LazyState
	{
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public decimal[][] A
		{
			get { return removedRows_ == null ? A_ : CalcState().A; }
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public decimal[] b
		{
			get { return removedRows_ == null ? b_ : CalcState().b; }
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public decimal[] c
		{
			get { return removedRows_ == null ? c_ : CalcState().c; }
		}

		public int Rows
		{
			get { return A_.Length - removedRowsCount_; }
		}

		public int Columns
		{
			get { return c_.Length; }
		}

		public decimal[] RemovedRow
		{
			get { return removedRow_; }
		}

		public decimal RemovedB
		{
			get { return removedB_; }
		}

		/// <summary>Явная установка стейта</summary>
		public void SetState(decimal[][] A, decimal[] b, decimal[] c)
		{
			A_ = A;
			b_ = b;
			c_ = c;
			removedRow_ = null;
			removedRowsCount_ = 0;
			removedRows_ = null;
		}

		/// <summary>Отложенное удаление строки</summary>
		public void UpdateState(int removedRow)
		{
			EnsureCorrectRemovedLeftRows();
            var tmp = removedRow;
			if (removedRowsCount_ != 0)
			{
				tmp = leftRows_[removedRow];
				leftRows_.RemoveAt(removedRow);
			}
			removedRow_ = A_[tmp];
			removedB_ = b_[tmp];
			removedRows_.Add(removedRow);
			++removedRowsCount_;
		}

		/// <summary>
		/// Поскольку состояния удаленных и оставшихся строк разделяются
		/// то возможна ситуация, когда списки удаленных и оставшихся строк уже "ушли" дальше в клонированной копии
		/// В этом случае нужно создать свою версию
		/// </summary>
		private void EnsureCorrectRemovedLeftRows()
		{
			if (removedRows_ == null) // исходное состоние
			{
				removedRows_ = new List<int>();
				removedRowsCount_ = 0;
				leftRows_ = null;
				return;
			}
			if (leftRows_ == null || removedRowsCount_ != removedRows_.Count)
			{
				EnsureLeftRows();
				if (removedRowsCount_ != removedRows_.Count)
				{
					var newRemovedRows = new List<int>(removedRowsCount_);
					for (var i = 0; i < removedRowsCount_; ++i)
					{
                        newRemovedRows.Add(removedRows_[i]);
					}
					removedRows_ = newRemovedRows;
				}
			}
		}

		private void EnsureLeftRows()
		{
			if (removedRowsCount_ != removedRows_.Count)
			{
                var n = A_.Length;
				leftRows_ = new List<int>(n);
				for (var i = 0; i < n; ++i)
                {
					leftRows_.Add(i);
                }
				for (var i = 0; i < removedRowsCount_; ++i)
				{
                    leftRows_.RemoveAt(removedRows_[i]);
				}
			}
            else if (leftRows_ == null)
            {
                var n = A_.Length - 1;
                leftRows_ = new List<int>(n);
                var removedIndex = removedRows_[removedRowsCount_ - 1];
                for (var i = 0; i <= n; ++i)
                {
                    if (i == removedIndex)
                    {
                        continue;
                    }
                    leftRows_.Add(i);
                }
            }
		}

		private LazyState CalcState()
		{
            decimal[][] newA;
            decimal[] newB;
			if (leftRows_ == null) //специальная версия для случая удаления только одной строки
			{
				var rows = A_.Length - 1;
				newA = new decimal[rows][];
				newB = new decimal[rows];
				var deletedRowIndex = removedRows_[0];
				var insertIndex = 0;
				for (var i = 0; i <= rows; ++i)
				{
					if (i == deletedRowIndex)
					{
                        continue;
					}
					newA[insertIndex] = A_[i];
					newB[insertIndex++] = b_[i];
				}
			}
			else
			{
				EnsureLeftRows();
				newA = new decimal[leftRows_.Count][];
                newB = new decimal[leftRows_.Count];
				var insertIndex = 0;
				foreach (var i in leftRows_)
				{
					newA[insertIndex] = A_[i];
					newB[insertIndex++] = b_[i];
				}
			}
            A_ = newA;
            b_ = newB;
			removedRowsCount_ = 0;
			removedRows_ = null;
			leftRows_ = null;
			// removedRow все еще показывает на удаленную строку старой матрицы. вдруг это кому-нибудь пригодится.
			return this;
		}

		/// <summary>Клонирование</summary>
		public LazyState Clone()
		{
			var state = new LazyState
			{
			    removedRows_ = removedRows_,
			    removedRowsCount_ = removedRowsCount_,
			    A_ = A_,
			    b_ = b_,
			    c_ = c_,
			    leftRows_ = leftRows_,
			    removedRow_ = removedRow_,
			    removedB_ = removedB_
			};
		    return state;
		}

		/// <summary>Индексы строк, удаленных из A (по отношению к leftRows)</summary>
		private List<int> removedRows_;
		/// <summary>Счетчик удаленных строк</summary>
		private int removedRowsCount_;
		/// <summary>Исходное значение A</summary>
		private decimal[][] A_;
		/// <summary>Индексы оставшихся переменных</summary>
		private List<int> leftRows_;
		private decimal[] b_;
        private decimal[] c_;
		private decimal[] removedRow_;
        private decimal removedB_;
	}

	/// <summary>Состояние алгоритма</summary>
	internal struct State
	{
		public State(LazyState currentState, int removedRowIndex, int removedColumnIndex
			, LazyState newState)
		{
			CurrentState = currentState;
			RemovedRowIndex = removedRowIndex; RemovedColumnIndex = removedColumnIndex;
			NewState = newState;
		}

		public LazyState CurrentState;
		public LazyState NewState;
		public int RemovedRowIndex;
		public int RemovedColumnIndex;
    }
#endregion States
}
