// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace Markov
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Builds and walks interconnected states based on probabilities probed at different depths.
    /// </summary>
    /// <typeparam name="T">The type of the constituent parts of each state in the Markov chain.</typeparam>
    public class MarkovChainWithBackoff<T>
        where T : IEquatable<T>
    {
        private readonly List<MarkovChain<T>> chains = new List<MarkovChain<T>>();
        private readonly int desiredNumNextStates;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkovChainWithBackoff{T}"/> class.
        /// </summary>
        /// <param name="maximumOrder">Indicates the maximum/starting order of the <see cref="MarkovChainWithBackoff{T}"/>.</param>
        /// <param name="desiredNumNextStates">Indicates the desired number of next states of the <see cref="MarkovChainWithBackoff{T}"/>.</param>
        /// <remarks>
        /// <para>The order of a traditional markov generator indicates the depth of its internal state. A generator
        /// with an order of 1 will choose items based on the previous item, a generator with an order of 2
        /// will choose items based on the previous 2 items, and so on.</para>
        /// <para>In a <see cref="MarkovChainWithBackoff{T}"/> we start with an order of <paramref name="maximumOrder"/>.
        /// We peak at the next possible and if we have more than <paramref name="desiredNumNextStates"/> we generate
        /// at that order. If we don't have enough next possible states we lower the order by 1 and repeat. If we reach
        /// an order of one we generate regardless of the number of possible states.</para>
        /// <para>One is the lowest valid <paramref name="maximumOrder"/> and zero is the lowest valid<paramref name="desiredNumNextStates"/>.</para>
        /// </remarks>
        public MarkovChainWithBackoff(int maximumOrder, int desiredNumNextStates)
        {
            if (maximumOrder < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumOrder));
            }

            if (desiredNumNextStates < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredNumNextStates));
            }

            this.desiredNumNextStates = desiredNumNextStates;

            for (var order = maximumOrder; order > 0; order--)
            {
                this.chains.Add(new MarkovChain<T>(order));
            }
        }

        /// <summary>
        /// Adds the items to the generator with a weight of one.
        /// </summary>
        /// <param name="items">The items to add to the generator.</param>
        public void Add(IEnumerable<T> items)
        {
            foreach (var chain in this.chains)
            {
                chain.Add(items);
            }
        }

        /// <summary>
        /// Randomly walks the chain, backing off the order when necessary.
        /// </summary>
        /// <param name="rand">The random number source for the chain.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the items chosen.</returns>
        /// <remarks>Assumes an empty starting state.</remarks>
        public IEnumerable<T> Chain(Random rand)
        {
            var workingQueue = new Queue<T>();

            while (true)
            {
                foreach (var chain in this.chains)
                {
                    var nextStates = chain.GetNextStates(workingQueue);
                    if (nextStates is null)
                    {
                        if (chain.Order <= 1)
                        {
                            yield break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (nextStates.Count >= this.desiredNumNextStates || chain.Order <= 1)
                    {
                        var totalNonTerminalWeight = nextStates.Sum(w => w.Value);

                        var terminalWeight = chain.GetTerminalWeight(workingQueue);
                        var randomValue = rand.Next(totalNonTerminalWeight + terminalWeight) + 1;

                        if (randomValue > totalNonTerminalWeight)
                        {
                            yield break;
                        }

                        var currentWeight = 0;
                        foreach (var nextItem in nextStates)
                        {
                            currentWeight += nextItem.Value;
                            if (currentWeight >= randomValue)
                            {
                                yield return nextItem.Key;
                                workingQueue.Enqueue(nextItem.Key);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }
    }
}
