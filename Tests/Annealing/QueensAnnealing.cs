using System;
using System.Linq;
using Algorithms.Annealing;

namespace Tests.Annealing
{
    public class QueensAnnealing : AnnealingPowerSeries<Point>
    {
        private Point nextPoint_;

        /// <summary>.ctor</summary>
        /// <param name="size">Размер доски</param>
        public QueensAnnealing(ushort size)
        {
            if (size < 4)
            {
                throw new ArgumentException("Size should be greater than 3", "size");
            }
            CurrentPoint = new Point(size);
            StartTemperature = 5;
            Alpha = 0.995;
            IterationsPerTemperatureStep = (uint)Math.Max(100, size / 50);
            StopTemperature = 0.0001;
        }

        public override bool Solve()
        {
            Value = CurrentPoint.Value;
 	        return base.Solve();
        }

        protected override double GetNextValue()
        {
            MakeNextPoint();
            return nextPoint_.Value;
        }

        private void MakeNextPoint()
        {
            // перестановка двух случайных столбцов
            // первый из которых должен иметь ненулевое число коллизий
            var size = CurrentPoint.Board.Length;
            ushort i;
            do
            {
                i = (ushort)Random.Next(0, size);
            } while (!CurrentPoint.HasCollisions(i));
            var j = (ushort)((i + Random.Next(0, size - 1)) % size);
            nextPoint_ = new Point(CurrentPoint);
            nextPoint_.SwapColumns(i, j);
        }

        protected override Point GetNextPoint()
        {
            return nextPoint_;
        }
    }

    public class Point
    {
        private readonly ushort size_;
        private readonly ushort[] queensOnPositiveDiags_;
        private readonly ushort[] queensOnNegativeDiags_;

        public Point(ushort size)
        {
            size_ = size;
            var sort = new Tuple<ushort, ushort>[size];
            var r = new Random();
            for (ushort i = 0; i < size; ++i)
            {
                sort[i] = Tuple.Create(i, (ushort)r.Next(0, ushort.MaxValue));
            }
            Array.Sort(sort, (x,y) => x.Item2 - y.Item2);
            Board = sort.Select(x => x.Item1).ToArray();
            // считаем число ферзей на каждой из положительных диагоналей (r - c = k)
            // и отрицательных диагоналей (r + c = l)
            // крайние 2 диагонали состоят из одного элемента и реально не нужны
            // но так проще математика
            var diagsCount = 2 * size - 1;
            queensOnNegativeDiags_ = new ushort[diagsCount];
            queensOnPositiveDiags_ = new ushort[diagsCount];
            for (var i = 0; i < size; ++i)
            {
                var row = Board[i];
                var positiveIndex = row - i + size - 1;
                ++queensOnPositiveDiags_[positiveIndex];
                var negativeIndex = row + i;
                ++queensOnNegativeDiags_[negativeIndex];
            }
            for (var i = 1; i < diagsCount - 1; ++i)
            {
                var p = queensOnPositiveDiags_[i];
                Value += (uint)(p - 1) * p; // каждый из k ферзей бъет ровно k - 1 соседей по диагонали
                var n = queensOnNegativeDiags_[i];
                Value += (uint)(n - 1) * n;
            }
        }

        public Point(Point src)
        {
            size_ = src.size_;
            Board = (ushort[])src.Board.Clone();
            queensOnPositiveDiags_ = (ushort[])src.queensOnPositiveDiags_.Clone();
            queensOnNegativeDiags_ = (ushort[])src.queensOnNegativeDiags_.Clone();
            Value = src.Value;
        }

        /// <summary>Доска</summary>
        public ushort[] Board { get; private set; }
        /// <summary>Полное число коллизий</summary>
        public uint Value { get; private set; }

        /// <summary>Проверяет, есть ли коллизии у ферзя в колонке</summary>
        public bool HasCollisions(int column)
        {
            var row = Board[column];
            var positiveIndex = row - column + size_ - 1;
            if (queensOnPositiveDiags_[positiveIndex] > 1)
            {
                return true;
            }
            var negativeIndex = row + column;
            if (queensOnNegativeDiags_[negativeIndex] > 1)
            {
                return true;
            }
            return false;
        }

        internal void SwapColumns(ushort i, ushort j)
        {
            var ri = Board[i];
            var rj = Board[j];
            MoveQueen(i, rj);
            MoveQueen(j, ri);
        }

        private void MoveQueen(ushort column, ushort toRow)
        {
            // сначала вычитаем с исходных диагоналей
            var fromRow = Board[column];
            var positiveAdjust = - column + size_ - 1;
            var positiveIndex = fromRow + positiveAdjust;
            var k = queensOnPositiveDiags_[positiveIndex];
            --k;
            queensOnPositiveDiags_[positiveIndex] = k;
            Value -= (uint)k * 2;
            var negativeAdjust = column;
            var negativeIndex = fromRow + negativeAdjust;
            k = queensOnNegativeDiags_[negativeIndex];
            --k;
            queensOnNegativeDiags_[negativeIndex] = k;
            Value -= (uint)k * 2;
            // потом добавляем к новым
            positiveIndex = toRow + positiveAdjust;
            k = queensOnPositiveDiags_[positiveIndex];
            Value += (uint)k * 2;
            ++k;
            queensOnPositiveDiags_[positiveIndex] = k;
            negativeIndex = toRow + negativeAdjust;
            k = queensOnNegativeDiags_[negativeIndex];
            Value += (uint)k * 2;
            ++k;
            queensOnNegativeDiags_[negativeIndex] = k;
            Board[column] = toRow;
        }
    }
}
