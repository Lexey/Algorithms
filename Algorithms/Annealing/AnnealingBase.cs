using System;
using Common.Logging;

namespace Algorithms.Annealing
{
    /// <summary>Базовый класс для оптимизационных алгоритмов на база отжига (simulated annealing)</summary>
    public abstract class AnnealingBase<T>
    {
        private ILog logger_;
        private double stopTemperature_;

        /// <summary>.ctor</summary>
        protected AnnealingBase() : this(LogManager.GetCurrentClassLogger()) {}
        /// <summary>.ctor</summary>
        protected AnnealingBase(ILog logger)
        {
            Log = logger;
            Random = new Random();
            Value = double.MaxValue;
            stopTemperature_ = 0.5;
            OptimalValue = 0;
        }

        /// <summary>Текущая точка</summary>
        public T CurrentPoint { get; protected set; }
        /// <summary>Текущее значение целевой функции</summary>
        public double Value { get; protected set; }
        /// <summary>Лучшая точка</summary>
        public T BestPoint { get; private set; }
        /// <summary>Лучшее значение целевой функции</summary>
        public double BestValue { get; private set; }
        /// <summary>Логгер</summary>
        public ILog Log
        {
            get { return logger_; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                logger_ = value;
            }
        }
        /// <summary>Ищет оптимум целевой функции</summary>
        /// <returns>true, если достигнут оптимум; иначе false</returns>
        public virtual bool Solve()
        {
            CurrentIteration = 0;
            BestPoint = CurrentPoint;
            BestValue = Value;
            if (Value <= OptimalValue)
            {
                Log.Debug("Initial value is already optimal");
                return true;
            }
            double t;
            do
            {
                ++CurrentIteration;
                t = NextTemperature();
                var value = GetNextValue();
                var valueDelta = value - Value;
                if (valueDelta <= 0)
                {
                    Log.TraceFormat("New value {0} (old {1}). Doing shift", value, Value);
                    var point = GetNextPoint();
                    CurrentPoint = point;
                    Value = value;
                    if (value < BestValue)
                    {
                        BestPoint = CurrentPoint;
                        BestValue = value;
                    }
                }
                else
                {
                    var p = 0d;
                    if (t > 0)
                    {
                        p = Math.Exp(-valueDelta / t);
                    }
                    if (p <= double.Epsilon)
                    {
                        Log.Trace("Probabilty is too low. Point is fixed");
                        // сдвинуться в точку с большим значением уже нельзя, но есть еще шанс перепрыгнуть случайно в лучшее значение
                    }
                    else
                    {
                        var doShift = Random.NextDouble() <= p;
                        Log.Trace(f => f("Probabilty is {0}. {1} shift. New value {2} (old {3})"
                                            , p, doShift ? "Doing" : "Not doing", value, Value));
                        if (doShift)
                        {
                            var point = GetNextPoint();
                            CurrentPoint = point;
                            Value = value;
                        }
                    }
                }
                if (CurrentIteration % 5000 == 0)
                {
                    Log.DebugFormat("Done {0} iterations. Current value = {1}, t = {2:e3}"
                                       , CurrentIteration, Value, t);
                }
            } while (Value > OptimalValue && t >= StopTemperature);
            Log.DebugFormat("Finished after {0} iterations. Final t: {1:e3}, v = {2}"
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
        protected ulong CurrentIteration { get; private set; }
        /// <summary>Расчет целевой функции в следующей точке</summary>
        protected abstract double GetNextValue();
        /// <summary>Получение следующей точки</summary>
        protected abstract T GetNextPoint();
        /// <summary>Следующее значение "температуры"</summary>
        protected abstract double NextTemperature();
    }
}
