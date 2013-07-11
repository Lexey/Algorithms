using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithms
{
	/// <summary>Вектор</summary>
	public class Vector : Matrix
	{
		/// <summary>Конструктор вектора длины n</summary>
		public Vector(int n) : base(1, n) {}
        /// <summary>Конструктор из списка значений</summary>
        public Vector(IList<double> values) : base(new [] { values }) {}

		/// <summary>Индексер элементов вектора</summary>
		public new double this[int i]
		{
			get { return base[0][i]; }
			set { base[0][i] = value; }
		}

		/// <summary>Конструктор из другого вектора</summary>
		public Vector(Vector other) : base(other) {}

		/// <summary>Полное клонирование вектора</summary>
		public new Vector Clone()
		{
			return new Vector(this);
		}

		/// <summary>Число элементов</summary>
		public int Count
		{
			get { return Columns; }
		}
	}

	/// <summary>Матрица M x N</summary>
	public class Matrix
	{
		/// <summary>Конструктор матрицы размерности m x n</summary>
		/// <param name="m">Число строк</param>
		/// <param name="n">Число столбцов</param>
		public Matrix(int m, int n)
		{
			Vals_ = new double[m][];
			for (var i = 0; i < m; ++i)
			{
                Vals_[i] = new double[n];
			}
		}

        /// <summary>Конструктор из значений</summary>
        public Matrix(IList<IList<double>> values)
        {
            var rows = values.Count;
            Vals_ = new double[rows][];
            Vals_[0] = values[0].ToArray();
            var columns = values[0].Count;
            if (columns == 0)
            {
                throw new ArgumentException("First row has zero length");
            }
            for (var i = 1; i < rows; ++i)
            {
                if (values[i].Count != columns)
                {
                    throw new ArgumentException("Rows have different length");
                }
                Vals_[i] = values[i].ToArray();
            }
        }

		/// <summary>Конструктор копии матрицы</summary>
		public Matrix(Matrix other)
		{
			Vals_ = new double[other.Vals_.Length][];
			for (var i = 0; i < other.Vals_.Length; ++i)
			{
			    Vals_[i] = (double[])other.Vals_[i].Clone();
			}
		}

		/// <summary>Полное клонирование матрицы</summary>
		public Matrix Clone()
		{
			return new Matrix(this);
		}

		/// <summary>Индексер элементов матрицы</summary>
		public double[] this[int i]
		{
			get { return Vals_[i]; }
		}

		/// <summary>Возвращает транспонированную матрицу</summary>
		public Matrix Transpose()
		{
			var m = new Matrix(Vals_[0].Length, Vals_.Length);
			for (var i = 0; i < Vals_.Length; ++i)
			{
                for (var j = 0; j < Vals_[0].Length; ++j)
                {
                    m[j][i] = Vals_[i][j];
                }
			}
			return m;
		}

		/// <summary>Число строк</summary>
		public int Rows
		{
			get { return Vals_.Length; }
		}

		/// <summary>Число колонок</summary>
		public int Columns
		{
			get { return Vals_[0].Length; }
		}

		#region Статические методы
		/// <summary>Единичная матрица размера n</summary>
		public static Matrix Identity(int n)
		{
			var m = new Matrix(n, n);
			for (var i = 0; i < n; ++i)
			{
                m[i][i] = 1;
			}
			return m;
		}

		/// <summary>Нулевая матрица размера n</summary>
		public static Matrix Zero(int n)
		{
			return new Matrix(n, n);
		}
		#endregion Статические методы

		/// <summary>Значения матрицы</summary>
		protected double[][] Vals_;
	}
}
