using System;
using Common.Logging;

namespace Algorithms.Annealing
{
    /// <summary>Базовый класс для оптимизационных алгоритмов на база отжига (simulated annealing)</summary>
    public abstract class AnnealingBase<T>
    {
        protected static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private double stopTemperature_;

        /// <summary>.ctor</summary>
        protected AnnealingBase()
        {
            Random = new Random();
            Value = double.MaxValue;
            stopTemperature_ = 0.5;
            OptimalValue = 0;
        }

        /// <summary>Текущая точка</summary>
        public T Solution { get; protected set; }
        /// <summary>Текущее значение целевой функции</summary>
        public double Value { get; protected set; }
        /// <summary>Ищет оптимум целевой функции</summary>
        /// <returns>true, если достигнут оптимум; иначе false</returns>
        public virtual bool Solve()
        {
            CurrentIteration = 0;
            if (Value <= OptimalValue)
            {
                Logger.Debug("Initial value is already optimal");
                return true;
            }
            double t;
            do
            {
                ++CurrentIteration;
                t = NextTemperature();
                var point = NextPoint();
                var value = CalcValue(point);
                var valueDelta = value - Value;
                if (valueDelta <= 0)
                {
                    Logger.TraceFormat("New value {0} (old {1}). Doing shift", value, Value);
                    Solution = point;
                    Value = value;
                    continue;
                }
                var p = 0d;
                if (t > 0)
                {
                    p = Math.Exp(-valueDelta / t);
                }
                if (p <= double.Epsilon)
                {
                    Logger.Trace("Probabilty is too low. Point is fixed");
                    continue;
                        // сдвинуться в точку с большим значением уже нельзя, но есть еще шанс перепрыгнуть случайно в лучшее значение
                }
                var doShift = Random.NextDouble() <= p;
                Logger.Trace(f => f("Probabilty is {0}. {1} shift. New value {2} (old {3})"
                                    , p, doShift ? "Doing" : "Not doing", value, Value));
                if (doShift)
                {
                    Solution = point;
                    Value = value;
                }
                if (CurrentIteration % 1000 == 0)
                {
                    Logger.DebugFormat("Done {0} iterations. Current value = {1}, t = {2}"
                                       , CurrentIteration, Value, t);
                }
            } while (Value > OptimalValue && t >= StopTemperature);
            Logger.DebugFormat("Finished after {0} iterations. Final t: {1}, v = {2}"
                , CurrentIteration, t, Value);
            return Value <= OptimalValue;
        }

        /// <summary>Максимальная температура, до которой итерировать</summary>
        public double StopTemperature
        {
            get { return stopTemperature_; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Stop temperature should be nonnegative");
                }
                stopTemperature_ = value;
            }
        }
        /// <summary>Значение функции, при достижении которого имеем оптимум</summary>
        public double OptimalValue { get; set; }

        /// <summary>Random number generator</summary>
        protected Random Random { get; private set; }
        /// <summary>Текущая итерация</summary>
        protected int CurrentIteration { get; private set; }
        /// <summary>Генерирует следующую точку</summary>
        protected abstract T NextPoint();
        /// <summary>Вычисляет значение целевой функции для точки</summary>
        protected abstract double CalcValue(T point);
        /// <summary>Следующее значение "температуры"</summary>
        protected abstract double NextTemperature();
    }
}
