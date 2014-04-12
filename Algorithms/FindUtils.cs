using System;
using System.Collections.Generic;

namespace Algorithms
{
    /// <summary>Реализации стандартных алгоритмов поиска в отсортированном списке</summary>
    public static class FindUtils
    {
        /// <summary>
        /// Возвращает наименьшую позицию i из [0, end] в списке, такую, что list[i] >= val
        /// </summary>
        /// <typeparam name="T">Тип элемента. Должен реализовывать IComparable&lt;T&gt; или IComparable</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <returns>Нижняя граница для данного значения (может быть list.Count)</returns>
        public static int LowerBound<T>(this IList<T> list, T val)
        {
            return LowerBound(list, val, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [from, end] в списке, такую, что list[i] >= val
        /// </summary>
        /// <typeparam name="T">Тип элемента. Должен реализовывать IComparable&lt;T&gt; или IComparable</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <param name="from">Начальный индекс массива</param>
        /// <returns>Нижняя граница (не вкл) для данного значения (может быть list.Count)</returns>
        public static int LowerBound<T>(this IList<T> list, T val, int from)
        {
            return list.LowerBound(val, from, list.Count, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [0, end] в списке, такую, что list[i] >= val
        /// </summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <typeparam name="U">Тип значения для поиска</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <param name="comparer">Функция с семантикой Comparer&lt;T&gt;.Compare</param>
        /// <returns>Нижняя граница для данного значения (может быть list.Count)</returns>
        public static int LowerBound<T, U>(this IList<T> list, U val, Func<T, U, int> comparer)
        {
            return LowerBound(list, val, 0, list.Count, comparer);
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [from, to] в списке, такую, что list[i] >= val
        /// </summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <typeparam name="U">Тип значения для поиска</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <param name="from">Начальный индекс массива</param>
        /// <param name="to">Верхняя граница массива (не включается)</param>
        /// <param name="comparer">Функция с семантикой Comparer&lt;T&gt;.Compare</param>
        /// <returns>Нижняя граница для данного значения (может быть to)</returns>
        public static int LowerBound<T, U>(this IList<T> list, U val, int from, int to, Func<T, U, int> comparer)
        {
            ValidateIndices(from, to, list);
            if (to <= from) // пустой диапазон
            {
                return to;
            }
            if (comparer(list[from], val) >= 0) //первое значение >= val
            {
                return from;
            }
            // тут мы уже обеспечили себе ивариант:
            // диапазон не пуст, list[from] < val, и to либо за границей диапазона, либо list[to] >= val
            for (;;)
            {
                var median = (to + from) / 2;
                if (median == from)
                {
                    return to;
                }
                var cmpResult = comparer(list[median], val);
                if (cmpResult < 0)
                {
                    // сужает диапазон, ибо median > from
                    from = median;
                }
                else
                {
                    // сохраняет инвариант, ибо median > from, т.е. диапазон не пуст,
                    // и list[median] >= val
                    // c другой стороны median < to, так что происходит сужение диапазона
                    to = median;
                }
            }
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [0, end] в списке, такую, что list[i] > val
        /// </summary>
        /// <typeparam name="T">Тип элемента. Должен реализовывать IComparable&lt;T&gt; или IComparable</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <returns>Верхняя граница для данного значения (может быть list.Count)</returns>
        public static int UpperBound<T>(this IList<T> list, T val)
        {
            return UpperBound(list, val, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [0, end] в списке, такую, что list[i] > val
        /// </summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <typeparam name="U">Тип значения для поиска</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <param name="comparer">Функция с семантикой Comparer&lt;T&gt;.Compare</param>
        /// <returns>Верхняя граница для данного значения (может быть list.Count)</returns>
        public static int UpperBound<T, U>(this IList<T> list, U val, Func<T, U, int> comparer)
        {
            return UpperBound(list, val, 0, list.Count, comparer);
        }

        /// <summary>
        /// Возвращает наименьшую позицию i из [from, to] в списке, такую, что list[i] > val
        /// </summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <typeparam name="U">Тип значения для поиска</typeparam>
        /// <param name="list">Отсортированный по возрастанию список элементов</param>
        /// <param name="val">Значение для сравнения</param>
        /// <param name="from">Начальный индекс массива</param>
        /// <param name="to">Верхняя граница массива (не включается)</param>
        /// <param name="comparer">Функция с семантикой Comparer&lt;T&gt;.Compare</param>
        /// <returns>Верхняя граница для данного значения (может быть to)</returns>
        public static int UpperBound<T, U>(this IList<T> list, U val, int from, int to, Func<T, U, int> comparer)
        {
            ValidateIndices(from, to, list);
            if (to <= from) // пустой диапазон
                return to;
            if (comparer(list[from], val) > 0) //первое значение > val
            {
                return from;
            }
            // тут мы уже обеспечили себе ивариант:
            // диапазон не пуст, list[from] <= val, и to либо за диапазоном, либо list[to] > val
            for (;;)
            {
                var median = (to + from) / 2;
                if (median == from)
                {
                    return to;
                }
                var cmpResult = comparer(list[median], val);
                if (cmpResult <= 0)
                {
                    // уменьшает диапазон, ибо median > from
                    from = median;
                }
                else
                {
                    // сохраняет инвариант, ибо median > from, т.е. диапазон не пуст,
                    // и list[median] > val
                    // c другой стороны median < to, так что происходит сужение диапазона
                    to = median;
                }
            }
        }

        /// <summary>
        /// Валидирует индексы
        /// </summary>
        /// <typeparam name="T">Тип элемента коллекции</typeparam>
        /// <param name="from">Начальный индекс</param>
        /// <param name="to">Верхняя граница (не включается)</param>
        /// <param name="list">Коллекция</param>
        private static void ValidateIndices<T>(int from, int to, ICollection<T> list)
        {
            if (from < 0 || to < 0)
            {
                throw new ArgumentException("Indices should be non-negative");
            }
            if (to > list.Count)
            {
                throw new ArgumentException("to should be less or equal to the list.Count");
            }
            if (to < from)
            {
                throw new ArgumentException("to should be not less than from");
            }
        }
    }
}
