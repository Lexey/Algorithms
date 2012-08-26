namespace Structures
{
    /// <summary>Реализация классической структуры Disjoint sets</summary>
    ///  <remarks>
    /// http://en.wikipedia.org/wiki/Disjoint-set_data_structure
    /// </remarks>
    public sealed class DisjointSets : DisjointSetsBase
    {
        /// <summary>Создает пустой DS</summary>
        public DisjointSets() {}

        /// <summary>Создает DS с заданным количеством элементов</summary>
        /// <param name="count">Количество элементов</param>
        public DisjointSets(int count)
        {
            Add(count);
        }

        /// <summary>Добавление заданного количества элементов</summary>
        /// <param name="count">Количество элементов</param>
        public void Add(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                Nodes.Add(new BasicNode { ParentIndex = -1, Rank = 0 });
            }
            SetCount += count;
        }
    }
}
