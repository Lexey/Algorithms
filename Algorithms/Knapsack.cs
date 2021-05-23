using Common.Logging;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Algorithms
{
    /// <summary>Реализация укладки рюкзака через BnB</summary>
    public class Knapsack
    {
        private readonly static ILog Log_ = LogManager.GetLogger<Knapsack>();

        /// <summary>Список предметов</summary>
        private readonly Item[] items_;
        /// <summary>Вместимость</summary>
        private readonly uint capacity_;
        /// <summary>Стэк состояний</summary>
        private readonly State[] stack_;
        /// <summary>Лучшая раскладка</summary>
        private BitArray bestSolution_;
        /// <summary>Лучшая ценность</summary>
        private uint bestValue_;
        /// <summary>Кэш решений подзадач</summary>
        private readonly Dictionary<CacheItemKey, CacheItemValue> cache_ = new Dictionary<CacheItemKey, CacheItemValue>();
        /// <summary>Число перебранных полных веток</summary>
        private ulong fullBranches_;
        /// <summary>Число попаданий кэша</summary>
        private ulong cacheHits_;
        /// <summary>Число промахов кэша</summary>
        private ulong cacheMisses_;

        /// <summary>.ctor</summary>
        /// <param name="capacity">Вместимость</param>
        /// <param name="values">Ценности предметов</param>
        /// <param name="weights">Веса предметов</param>
        public Knapsack(uint capacity, IList<uint> values, IList<uint> weights)
        {
            if (values.Count != weights.Count)
            {
                throw new ArgumentException("Values and weights should have the same length");
            }
            capacity_ = capacity;
            items_ = new Item[values.Count];
            if (values.Count == 0) return;
            for (var i = 0; i < values.Count; ++i)
            {
                items_[i] = new Item(i, values[i], weights[i]);
            }
            // сортируем в порядке убывания ценности на единицу веса
            Array.Sort(items_, (x, y) => y.ValuePerUnit.CompareTo(x.ValuePerUnit));
            stack_ = new State[items_.Length - 1];
        }

        /// <summary>Упаковка рюкзака</summary>
        /// <param name="capacity">Вместимость</param>
        /// <param name="values">Ценности предметов</param>
        /// <param name="weights">Веса предметов</param>
        /// <param name="taken">Флаги брать/не брать</param>
        /// <returns>Итоговая ценность упакованного</returns>
        public static uint Pack(uint capacity, uint[] values, uint[] weights, out bool[] taken)
        {
            var k = new Knapsack(capacity, values, weights);
            return k.Pack(out taken);
        }

        /// <summary>Преобразование результата из внутреннего формата</summary>
        private bool[] MakeFinalSolution()
        {
            var r = new bool[items_.Length];
            for (var i = 0; i < items_.Length; ++i)
            {
                if (!bestSolution_[i])
                {
                    continue;
                }
                r[items_[i].Index] = true;
            }
            return r;
        }

        /// <summary>Собственно реализация упаковки</summary>
        /// <param name="takenItems">Флаги брать/не брать</param>
        /// <returns>Итоговая ценность упакованного</returns>
        public uint Pack(out bool[] takenItems)
        {
            var maxEstimate = CalcEstimate(capacity_, 0);
            CreateGreedySolution();
            if (bestValue_ == maxEstimate)
            {
                // лучше уже не будет
                Log_.Debug("Greedy algorithm produced the best solution!");
                takenItems = MakeFinalSolution();
                return bestValue_;
            }

            var currentSolution = new bool[items_.Length]; // текущее решение
            var currentItem = 0; // индекс текущего предмета
            var currentValue = 0u; // текущая стоимость
            var capacityLeft = capacity_;
            var taken = true; // признак взятия предмета
            for (; ; ) // перебор всех предметов с отсечениями по максимальным оценкам остатков
            {
                var item = items_[currentItem];
                long branchValue; // цена полной ветки перебора
                if (currentItem == stack_.Length)
                {
                    // это последний предмет. спускаться больше некуда,
                    // так что просто рассматриваем два варианта - берем или нет
                    ++fullBranches_;
                    if (capacityLeft >= item.Weight)
                    {
                        currentSolution[currentItem] = true;
                        branchValue = currentValue + item.Value;
                    }
                    else
                    {
                        currentSolution[currentItem] = false;
                        branchValue = currentValue;
                    }
                    if (branchValue > bestValue_) // новое решение лучше предыдущего. запоминаем
                    {
                        bestSolution_ = new BitArray(currentSolution);
                        bestValue_ = (uint)branchValue;
                    }
                    taken = false;
                }
                else // не последний предмет - решаем подзадачи для вариантов берем/не берем
                {
                    if (taken) // берем
                    {
                        stack_[currentItem] = new State
                        {
                            CapacityLeft = capacityLeft,
                            MaxEstimate = maxEstimate,
                            Taken = true,
                            Value = currentValue,
                            BranchValue = -1
                        };
                        if (capacityLeft < item.Weight)
                        {
                            taken = false;
                            continue;
                        }
                        currentSolution[currentItem] = true;
                        // пытаемся получить решение подзадачи из кэша
                        var newCapacity = capacityLeft - item.Weight;
                        var cachedValue = TryApplyPartialSolution(newCapacity, currentItem + 1, ref currentSolution);
                        if (cachedValue >= 0)
                        {
                            // нашлось кэшированное решение
                            var newValue = currentValue + cachedValue + item.Value;
                            stack_[currentItem].BranchValue = newValue; // сохраняем, чтобы закэшировать текущее решение позже
                            if (newValue > bestValue_)
                            {
                                bestSolution_ = new BitArray(currentSolution);
                                bestValue_ = (uint)newValue;
                            }
                            taken = false; // теперь рассматриваем ветку, когда предмет не берется
                            continue;
                        }
                        // кэшированного решения нет
                        var newValue2 = currentValue + item.Value;
                        var newMaxEstimate = CalcEstimate(newCapacity, currentItem + 1) + newValue2;
                        if (newMaxEstimate <= bestValue_)
                        {
                            // верхняя оценка не лучше, чем уже известное решение
                            // смысла решать нет
                            taken = false;
                            continue;
                        }
                        maxEstimate = newMaxEstimate;
                        currentValue = newValue2;
                        capacityLeft = newCapacity;
                        ++currentItem;
                        continue; // переходим к перебору слежующего предмета
                    }
                    else // не берем
                    {
                        stack_[currentItem].Taken = false;
                        currentSolution[currentItem] = false;
                        var cachedValue = TryApplyPartialSolution(capacityLeft, currentItem + 1, ref currentSolution);
                        if (cachedValue >= 0)
                        {
                            // нашли решение подзадачи в кэше
                            var newValue = currentValue + cachedValue;
                            if (newValue > bestValue_)
                            {
                                bestSolution_ = new BitArray(currentSolution);
                                bestValue_ = (uint)newValue;
                            }
                            branchValue = Math.Max(stack_[currentItem].BranchValue, newValue);
                            // проваливаемся на открутку стэка состояний
                        }
                        else
                        {
                            // нет кэшированного решения
                            var newMaxEstimate = CalcEstimate(capacityLeft, currentItem + 1) + currentValue;
                            if (newMaxEstimate > bestValue_)
                            {
                                // есть шанс получить лучшее решение в подветке
                                maxEstimate = newMaxEstimate;
                                ++currentItem;
                                taken = true;
                                continue; // идем перебирать следующий предмет
                            }
                            branchValue = stack_[currentItem].BranchValue;
                            // проваливаемся на открутку стэка состояний
                        }
                    }
                }
                // открутка стэка состояний до последнего взятого предмета
                while (currentItem != 0 && !taken)
                {
                    var s = stack_[currentItem - 1];
                    if (branchValue >= 0)
                    {
                        // если есть валидное решение, то кэшируем его
                        CachePartialSolution(capacityLeft, currentItem, (uint)branchValue - currentValue, currentSolution);
                        branchValue = Math.Max(s.BranchValue, branchValue);
                        s.BranchValue = branchValue;
                    }
                    else
                    {
                        branchValue = s.BranchValue;
                    }
                    capacityLeft = s.CapacityLeft;
                    currentValue = s.Value;
                    taken = s.Taken;
                    --currentItem;
                }
                if (!taken)
                {
                    Log_.DebugFormat("Full branches: {0}, cache entries: {1}, cache hits: {2}, cache misses: {3}"
                        , fullBranches_, cache_.Count, cacheHits_, cacheMisses_);
                    // currentItem == 0 - это конец перебора
                    takenItems = MakeFinalSolution();
                    return bestValue_;
                }
                taken = false;
                maxEstimate = stack_[currentItem].MaxEstimate;
            }
        }

        /// <summary>Создает жадное решение</summary>
        /// <remarks>Пакуем первый попавшийся предмет с максимумом ценность/вес, если можем</remarks>
        private void CreateGreedySolution()
        {
            var capacity = capacity_;
            var v = 0u;
            var s = new bool[items_.Length];
            for (var i = 0; i < items_.Length && capacity > 0; ++i)
            {
                var item = items_[i];
                if (item.Weight <= capacity)
                {
                    s[i] = true;
                    v += item.Value;
                    capacity -= item.Weight;
                }
            }
            bestValue_ = v;
            bestSolution_ = new BitArray(s);
        }

        /// <summary>Кэширует решение подзадачи</summary>
        /// <param name="capacity">Емкость рюкзака подзадачи</param>
        /// <param name="startItem">Первый предмет</param>
        /// <param name="value">Оптимальная ценность</param>
        /// <param name="solution">Решение исходной задачи</param>
        private void CachePartialSolution(uint capacity, int startItem, uint value, bool[] solution)
        {
            var a = new bool[items_.Length - startItem];
            Array.Copy(solution, startItem, a, 0, a.Length);
            cache_.Add(new CacheItemKey { Capacity = capacity, StartItem = startItem }
                , new CacheItemValue { Array = new BitArray(a), Value = value });
        }

        /// <summary>Пытаемся найти и применить закэшированное решение подзадачи</summary>
        /// <param name="capacity">Оставшаяся вместимость</param>
        /// <param name="startItem">Первый предмет</param>
        /// <param name="solution">Текущее решение</param>
        /// <returns>Новое значение стоимости или -1, если нет кэшированного решения</returns>
        private long TryApplyPartialSolution(uint capacity, int startItem, ref bool[] solution)
        {
            CacheItemValue v;
            if (!cache_.TryGetValue(new CacheItemKey { Capacity = capacity, StartItem = startItem }
                                    , out v))
            {
                ++cacheMisses_;
                return -1;
            }
            ++cacheHits_;
            v.Array.CopyTo(solution, startItem);
            return v.Value;
        }

        /// <summary>Верхняя оценка ценности</summary>
        /// <param name="leftCapacity">Остаток вместимости</param>
        /// <param name="fromIndex">Индекс первого рассматриваемого предмета</param>
        /// <returns>Верхняя оценка</returns>
        private uint CalcEstimate(uint leftCapacity, int fromIndex)
        {
            if (leftCapacity == 0)
            {
                return 0;
            }
            var v = 0.0;
            // Take first elements since these are the maximum ones by value per unit
            for (var i = fromIndex; i < items_.Length; ++i)
            {
                var item = items_[i];
                var w = item.Weight;
                if (w <= leftCapacity)
                {
                    v += item.Value + double.Epsilon * w;
                    leftCapacity -= w;
                }
                else
                {
                    v += (item.ValuePerUnit + double.Epsilon) * leftCapacity;
                    break;
                }
            }
            return (uint)v;
        }

        /// <summary>Состояние алгоритма</summary>
        private class State
        {
            /// <summary>Текущая ценность</summary>
            public uint Value;
            /// <summary>Максимальная оценка прибавки, которую можно получить по оставшимся предметам</summary>
            public uint MaxEstimate;
            /// <summary>Признак взятия/не взятия текущего предмета</summary>
            public bool Taken;
            /// <summary>Остаток вместимости</summary>
            public uint CapacityLeft;
            /// <summary>Ценности ветки (-1, если нет решений)</summary>
            public long BranchValue;
        }

        /// <summary>Элемент упаковки</summary>
        private struct Item
        {
            private readonly int index_;
            private readonly uint value_;
            private readonly uint weight_;
            private readonly double valuePerUnit_;

            public Item(int index, uint value, uint weight)
            {
                index_ = index;
                value_ = value;
                weight_ = weight;
                valuePerUnit_ = (double)value / weight;
            }

            /// <summary>Индекс исходного элемента</summary>
            public int Index
            {
                get { return index_; }
            }

            /// <summary>Ценность</summary>
            public uint Value
            {
                get { return value_; }
            }

            /// <summary>Вес</summary>
            public uint Weight
            {
                get { return weight_; }
            }

            /// <summary>Ценность единицы веса</summary>
            public double ValuePerUnit
            {
                get { return valuePerUnit_; }
            }
        }

        /// <summary>Ключ элемента кэша</summary>
        private struct CacheItemKey : IEquatable<CacheItemKey>
        {
            public bool Equals(CacheItemKey other)
            {
                return StartItem == other.StartItem && Capacity == other.Capacity;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                return obj is CacheItemKey && Equals((CacheItemKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StartItem * 397) ^ (int)Capacity;
                }
            }

            /// <summary>Вещь, для которой решалась подзадача (которую взяли)</summary>
            public int StartItem;
            /// <summary>Емкость подрюкзака</summary>
            public uint Capacity;
        }

        /// <summary>Элемент кэша</summary>
        private struct CacheItemValue
        {
            /// <summary>Массив флагов берем/не берем</summary>
            public BitArray Array;
            /// <summary>Ценность укладки</summary>
            public uint Value;
        }
    }
}
