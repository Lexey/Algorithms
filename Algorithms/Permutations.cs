using System;
using System.Collections.Generic;

namespace Algorithms
{
    public static class Permutations
    {
        /// <summary>Генерирует следующую перестановку списка</summary>
        /// <typeparam name="T">Тип элемента списка</typeparam>
        /// <param name="seq">Список элементов</param>
        /// <returns>true в случае успешной перестановка, false - если вернулись к исходной перестановке (упорядоченному по убыванию списку)</returns>
        public static bool NextPermutation<T>(this IList<T> seq) where T : IComparable<T>
        {
            if (seq.Count < 2)
            {
                return false;
            }
            var end = seq.Count;
            var first = end - 1;
            for (; ; ) // поиск с конца первых 2 соседних элементов first и second : first < second
            {
                var second = first;
                var v = seq[--first];
                if (v.CompareTo(seq[second]) < 0)
                {
                    // ищем с конца первый элемент swapCandidate: first < swapCandidate
                    // очевидно, что он может как совпасть с second, так и оказаться правее него
                    var swapCandidate = end;
                    while (v.CompareTo(seq[--swapCandidate]) >= 0) { }
                    // меняем местами first и swapCandidate
                    seq[first] = seq[swapCandidate];
                    seq[swapCandidate] = v;
                    // и меняем порядок элементов от second до конца на обратный
                    // (очевидно, что до переворота эта подпоследовательность была отсортирована в обратном порядке)
                    seq.Reverse(second, end - second);
                    return true;
                }

                if (first == 0) // полностью перевернутый порядок
                {
                    seq.Reverse();
                    return false;
                }
            }
        }

        /// <summary>Переворот последовательности</summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <param name="seq">Последовательность</param>
        public static void Reverse<T>(this IList<T> seq)
        {
            seq.Reverse(0, seq.Count);
        }

        /// <summary>Переворот подпоследовательности</summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <param name="seq">Последовательность</param>
        /// <param name="start">Первый элемент подпоследовательности</param>
        /// <param name="length">Длина подпоследовательности</param>
        public static void Reverse<T>(this IList<T> seq, int start, int length)
        {
            var end = start + length;
            while (--end > start)
            {
                var t = seq[start];
                seq[start] = seq[end];
                seq[end] = t;
                ++start;
            }
        }
    }
}
