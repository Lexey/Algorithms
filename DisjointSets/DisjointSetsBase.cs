using System.Collections.Generic;

namespace Structures
{
    /// <summary>Базовый класс для реализации Disjoint sets</summary>
    ///  <remarks>
    /// http://en.wikipedia.org/wiki/Disjoint-set_data_structure
    /// </remarks>
    public class DisjointSetsBase
    {
        /// <summary>Список всех узлов</summary>
        protected readonly List<BasicNode> Nodes = new List<BasicNode>();

        /// <summary>Создает пустой DS</summary>
        protected DisjointSetsBase() {}

        /// <summary>Число элементов</summary>
        public int Count
        {
            get { return Nodes.Count; }
        }

        /// <summary>Число несвязанных множеств</summary>
        public int SetCount
        {
            get; protected set;
        }

        /// <summary>Поиск идентификатора множества, которому принадлежит элемент по заданному индексу</summary>
        /// <param name="index">Индекс элемента</param>
        /// <returns>Идентификатор множества, которому принадлежит элемент</returns>
        /// <remarks>Идентификатор множества - это индекс элемента, представляющего множество</remarks>
        public int FindSet(int index)
        {
            // сначала ищем индекс корневого элемента дерева, к которому принадлежит наш узел
            var rootIndex = index;
            for (;;)
            {
                var parentIndex = Nodes[rootIndex].ParentIndex;
                if (parentIndex == -1)
                {
                    break;
                }
                rootIndex = parentIndex;
            }

            // компрессия пути - идем от нашего элемента вверх к корню, обновляя ParentIndex на rootIndex
            while (index != rootIndex)
            {
                var node = Nodes[index];
                index = node.ParentIndex;
                node.ParentIndex = rootIndex;
            }
            return rootIndex;
        }

        /// <summary>Объединение двух множеств в одно</summary>
        /// <param name="elementOfSet1">Элемент первого множества</param>
        /// <param name="elementOfSet2">Элемент второго можества</param>
        public void Union(int elementOfSet1, int elementOfSet2)
        {
            elementOfSet1 = FindSet(elementOfSet1);
            elementOfSet2 = FindSet(elementOfSet2);
            if (elementOfSet1 == elementOfSet2)
            {
                return; // уже одно множество
            }

            var set1Root = Nodes[elementOfSet1];
            var set2Root = Nodes[elementOfSet2];
            var rankDifference = set1Root.Rank - set2Root.Rank;
            // Цепляем дерево с меньшим рангом к корню дерева с большим. В этом случае ранг получившегося дерева равен большему, кроме случая, когда ранги равны (тогда будет +1)
            if (rankDifference > 0) // у 1-го ранг больше
            {
                set2Root.ParentIndex = elementOfSet1;
            }
            else if (rankDifference < 0) // у 2-го больше
            {
                set1Root.ParentIndex = elementOfSet2;
            }
            else // ранги равны. пофигу что к чему цеплять, но нужно увеличить ранг того, к чему прицепили
            {
                set2Root.ParentIndex = elementOfSet1;
                ++set1Root.Rank;
            }

            // поскольку слили 2 в одно, уменьшаем число сетов
            --SetCount;
        }

        /// <summary>Узел дерева</summary>
        protected class BasicNode
        {
            /// <summary>Индекс родителя (после компрессии пути указывает на корень)</summary>
            public int ParentIndex;

            /// <summary>Примерный уровень ноды в дереве (с несжатыми путями), считая от корня</summary>
            public int Rank;
        }
    }
}
