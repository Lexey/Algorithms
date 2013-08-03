using System.Collections.Generic;
using Algorithms;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class KnapsackTests
    {
        private IEnumerable<object> GetTestData()
        {
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 0,
                            Values = new[] {1u, 2u, 3u, 4u},
                            Weights = new[] {1u, 2u, 3u, 4u}
                        },
                    0u, new bool[4], "Capacity = 0"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 10,
                            Values = new uint[0],
                            Weights = new uint[0]
                        },
                    0u, new bool[0], "No elements"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 0,
                            Values = new [] { 1u },
                            Weights = new [] { 1u }
                        },
                    0u, new bool[1], "No space"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 10,
                            Values = new [] { 13u },
                            Weights = new [] { 10u }
                        },
                    13u, new [] { true }, "One element"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 10,
                            Values = new [] { 33u, 21u },
                            Weights = new [] { 11u, 10u }
                        },
                    21u, new [] { false, true }, "Only 2nd fits"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 9,
                            Values = new [] { 33u, 21u },
                            Weights = new [] { 11u, 10u }
                        },
                    0u, new bool[2], "None fits"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 11u,
                            Values = new [] { 33u, 21u },
                            Weights = new [] { 11u, 10u }
                        },
                    33u, new [] { true, false }, "First is the best"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 21u,
                            Values = new [] { 33u, 5u },
                            Weights = new [] { 11u, 10u }
                        },
                    38u, new [] { true, true }, "Both taken"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 10u,
                            Values = new [] { 33u, 34u },
                            Weights = new [] { 10u, 10u }
                        },
                    34u, new [] { false, true }, "Second is the best"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 10u,
                            Values = new [] { 33u, 35u, 34u },
                            Weights = new [] { 10u, 11u, 10u }
                        },
                    34u, new [] { false, false, true }, "Third is the best"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 20u,
                            Values = new [] { 33u, 35u, 34u },
                            Weights = new [] { 10u, 11u, 10u }
                        },
                    67u, new [] { true, false, true }, "First and third"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 21u,
                            Values = new [] { 33u, 35u, 34u },
                            Weights = new [] { 10u, 11u, 10u }
                        },
                    69u, new [] { false, true, true }, "Second and third"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 7u,
                            Values = new [] { 16u, 19u, 23u, 28u },
                            Weights = new [] { 2u, 3u, 4u, 5u }
                        },
                    44u, new [] { true, false, false, true }, "A sample from the lecture"
                };
            yield return new object[]
                {
                    new Problem
                        {
                            Capacity = 11u,
                            Values = new [] { 8u, 10u, 15u, 4u },
                            Weights = new [] { 4u, 5u, 8u, 3u }
                        },
                    19u, new [] { false, false, true, true }, "ks_4_0"
                };
        }

        [Test]
        [TestCaseSource("GetTestData")]
        public void Test(Problem problem, uint expectedValue, bool[] expectedTaken, string comment)
        {
            bool[] taken;
            var value = Knapsack.Pack(problem.Capacity, problem.Values, problem.Weights, out taken);
            TestValid(problem, value, taken);
            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(taken, Is.EqualTo(expectedTaken));
        }

        public void TestValid(Problem problem, uint value, bool[] taken)
        {
            var usedCapacity = 0u;
            var calcValue = 0u;
            Assert.That(taken.Length, Is.EqualTo(problem.Values.Length));
            for (var i = 0; i < taken.Length; ++i)
            {
                if (!taken[i]) continue;
                usedCapacity += problem.Weights[i];
                calcValue += problem.Values[i];
            }
            Assert.That(usedCapacity, Is.LessThanOrEqualTo(problem.Capacity));
            Assert.That(calcValue, Is.EqualTo(value));
        }
    }

    public class Problem
    {
        public uint Capacity;
        public uint[] Values;
        public uint[] Weights;
    }
}
