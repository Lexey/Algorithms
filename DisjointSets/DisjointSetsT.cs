using System.Collections.Generic;
using System.Linq;

namespace Structures
{
    /// <summary>
    /// Реализация классической структуры Disjoint sets
    /// Generic - версия, позволяющая хранить данные внутри
    /// </summary>
    ///  <remarks>
    /// http://en.wikipedia.org/wiki/Disjoint-set_data_structure
    /// </remarks>
    public sealed class DisjointSets<T> : DisjointSetsBase
    {
        /// <summary>Создает пустой DS</summary>
        public DisjointSets() {}

        /// <summary>Создает DS со значениями из перечисления</summary>
        /// <param name="values">Перечисление значений для добавления</param>
        public DisjointSets(IEnumerable<T> values)
        {
            Add(values);
        }

        /// <summary>Получение значения элемента по индексу</summary>
        /// <param name="index">Индекс элемента</param>
        public T this[int index]
        {
            get { return ((Node<T>)Nodes[index]).Value; }
        }

        /// <summary>Добавление перечисления элементов</summary>
        /// <param name="values">Элементы</param>
        public void Add(IEnumerable<T> values)
        {
            var beforeCount = Nodes.Count;
            Nodes.AddRange(values.Select(x => new Node<T> { Value = x, ParentIndex = -1, Rank = 0 } ));
            SetCount += Nodes.Count - beforeCount;
        }

        /// <summary>Добавление одного элемента</summary>
        /// <param name="value">Элемент</param>
        public void Add(T value)
        {
            Nodes.Add(new Node<T> { Value = value, ParentIndex = -1, Rank = 0 });
            ++SetCount;
        }

        /// <summary>Узел дерева</summary>
        private class Node<T2> : BasicNode
        {
            /// <summary>Собственно полезные данные</summary>
            public T2 Value;
        }
    }
}
