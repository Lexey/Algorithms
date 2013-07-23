using System;
using Common.Logging;

namespace Algorithms.Annealing
{
    /// <summary>База для реализаций отжига с температурой, изменяющейся по степенному закону</summary>
    public abstract class AnnealingPowerSeries<T> : AnnealingBase<T>
    {
        private double alpha_;
        private uint iterationsPerTemperatureStep_;
        private double startTemperature_;
        private double currentTemperature_;
        private uint iterationToTemperatureDrop_;

        protected AnnealingPowerSeries()
        {
            Init();
        }

        protected AnnealingPowerSeries(ILog logger) : base(logger)
        {
            Init();
        }

        private void Init()
        {
            StartTemperature = 30;
            alpha_ = 0.98;
            iterationsPerTemperatureStep_ = 30;
        }

        public override bool Solve()
        {
            currentTemperature_ = startTemperature_;
            iterationToTemperatureDrop_ = iterationsPerTemperatureStep_;
            return base.Solve();
        }

        /// <summary>Стартовая температура</summary>
        public double StartTemperature
        {
            get { return startTemperature_; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Start temperature should be positive");
                }
                startTemperature_ = value;
            }
        }

        /// <summary>Коэффициент изменения температуры</summary>
        public double Alpha
        {
            get { return alpha_; }
            set
            {
                if (value <= 0 || value >= 1)
                {
                    throw new ArgumentException("Alpha should be in (0,1)");
                }
                alpha_ = value;
            }
        }

        public uint IterationsPerTemperatureStep
        {
            get { return iterationsPerTemperatureStep_; }
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException("Iterations number should be positive");
                }
                iterationsPerTemperatureStep_ = value;
            }
        }

        protected override double NextTemperature()
        {
            if (iterationToTemperatureDrop_-- == 0)
            {
                currentTemperature_ *= alpha_;
                iterationToTemperatureDrop_ = iterationsPerTemperatureStep_;
                Log.DebugFormat("T = {0:e3}, Value = {1}, Best = {2}"
                    , currentTemperature_, Value, BestValue);
            }
            return currentTemperature_;
        }
    }
}
