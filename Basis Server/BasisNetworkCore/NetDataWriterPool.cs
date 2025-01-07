using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;

namespace BasisNetworkCore
{
    public static class NetDataWriterPool
    {
        private static readonly ConcurrentBag<NetDataWriter> _pool = new ConcurrentBag<NetDataWriter>();
        private static readonly ConcurrentDictionary<int, ConcurrentBag<NetDataWriter>> _sizeBuckets = new ConcurrentDictionary<int, ConcurrentBag<NetDataWriter>>();
        private static readonly int _maxBucketSize = 100; // Max size for size-specific buckets, adjust as needed
        private static readonly int _maxPoolSize = 200;

        /// <summary>
        /// Retrieves a NetDataWriter from the pool or creates a new one if none are available.
        /// Optionally checks for a specific size bucket.
        /// </summary>
        /// <param name="desiredSize">The desired size (capacity) of the NetDataWriter buffer.</param>
        /// <returns>A NetDataWriter instance.</returns>
        public static NetDataWriter GetWriter(int desiredSize = 0)
        {
            // Try to find a writer with the desired size
            if (desiredSize > 0 && _sizeBuckets.TryGetValue(desiredSize, out var bucket) && bucket.TryTake(out var writer))
            {
                writer.Reset(); // Ensure the writer is cleared before reuse.
                return writer;
            }

            // If no match is found, get a regular writer
            if (_pool.TryTake(out var writerFromPool))
            {
                writerFromPool.Reset(); // Clear any data from previous use
                return writerFromPool;
            }

            return new NetDataWriter(true, desiredSize); // Create a new one if the pool is empty
        }

        /// <summary>
        /// Returns a NetDataWriter to the pool for future reuse.
        /// </summary>
        /// <param name="writer">The NetDataWriter to return.</param>
        public static void ReturnWriter(NetDataWriter writer)
        {
            writer.Reset(); // Clear data for the next use.
            int size = writer.Capacity; // Track the capacity of the writer

            // Only add the writer if the pool hasn't exceeded the maximum size
            if (_pool.Count < _maxPoolSize)
            {
                _pool.Add(writer);

                // Optionally, place in a specific size bucket if it hasn't exceeded the max size
                if (size > 0 && _sizeBuckets.TryGetValue(size, out var sizeBucket) && sizeBucket.Count < _maxBucketSize)
                {
                    sizeBucket.Add(writer);
                }
                else if (size > 0)
                {
                    // Create a new bucket if none exists
                    var newBucket = new ConcurrentBag<NetDataWriter>();
                    newBucket.Add(writer);
                    _sizeBuckets[size] = newBucket;
                }
            }
            else
            {
                // If the pool is too large, consider discarding the writer or disposing of it
                //dispose baby!   writer.Dispose(); // Uncomment if disposable
                BNL.LogError("Exceeding Pool Count!");
                CleanUp();
                // Trigger garbage collection
                BNL.Log("Triggering garbage collection...");
                GC.Collect();
                GC.WaitForPendingFinalizers(); // Ensure all finalizers are run
                BNL.Log("Garbage collection completed.");
            }
        }

        /// <summary>
        /// Checks if the pool has any writers with the desired size.
        /// </summary>
        /// <param name="desiredSize">The size to check for.</param>
        /// <returns>True if a writer of the desired size exists in the pool; otherwise, false.</returns>
        public static bool HasWriterOfSize(int desiredSize)
        {
            return _sizeBuckets.ContainsKey(desiredSize) && _sizeBuckets[desiredSize].Count > 0;
        }

        /// <summary>
        /// Periodically cleans up the pool to prevent unbounded growth.
        /// </summary>
        public static void CleanUp()
        {
            // Clean up the pool if it exceeds a maximum size or remove unused writers
            if (_pool.Count > _maxPoolSize)
            {
                // For example, remove items in excess
                while (_pool.Count > _maxPoolSize)
                {
                    _pool.TryTake(out _);
                }
            }

            // Clean up size buckets if they are too large
            foreach (var bucket in _sizeBuckets.Values)
            {
                while (bucket.Count > _maxBucketSize)
                {
                    bucket.TryTake(out _);
                }
            }
        }
    }
}
