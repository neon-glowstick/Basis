using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BasisNetworkCore
{
    public static class NetDataWriterPool
    {
        private static readonly ConcurrentBag<NetDataWriter> _pool = new ConcurrentBag<NetDataWriter>();
        private static readonly ConcurrentDictionary<int, ConcurrentBag<NetDataWriter>> _sizeBuckets = new ConcurrentDictionary<int, ConcurrentBag<NetDataWriter>>();
        private static readonly int _maxBucketSize = 100; // Max size for size-specific buckets
        private static readonly int _maxPoolSize = 200;
        private static readonly int _maxTotalBucketWriters = 500; // Global cap for size-bucket writers

        private static int _activeWriters = 0; // Tracks active writers being used outside the pool

        // Timer for periodic cleanup
        private static readonly Timer _cleanupTimer;

        static NetDataWriterPool()
        {
            // Initialize the cleanup timer (runs every minute)
            _cleanupTimer = new Timer(_ => CleanUp(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Retrieves a NetDataWriter from the pool or creates a new one if none are available.
        /// Optionally checks for a specific size bucket.
        /// </summary>
        /// <param name="desiredSize">The desired size (capacity) of the NetDataWriter buffer.</param>
        /// <returns>A NetDataWriter instance.</returns>
        public static NetDataWriter GetWriter(int desiredSize = 0)
        {
            Interlocked.Increment(ref _activeWriters);

            // Try to find a writer with the desired size
            if (desiredSize > 0 && _sizeBuckets.TryGetValue(desiredSize, out var bucket) && bucket.TryTake(out var writer))
            {
                writer.Reset();
                return writer;
            }

            // If no match is found, get a regular writer
            if (_pool.TryTake(out var writerFromPool))
            {
                writerFromPool.Reset();
                return writerFromPool;
            }

            // Create a new writer if none available
            return new NetDataWriter(true, desiredSize);
        }

        /// <summary>
        /// Returns a NetDataWriter to the pool for future reuse.
        /// </summary>
        /// <param name="writer">The NetDataWriter to return.</param>
        public static void ReturnWriter(NetDataWriter writer)
        {
            Interlocked.Decrement(ref _activeWriters);
            writer.Reset(); // Clear data for the next use.
            int size = writer.Capacity; // Track the capacity of the writer

            // Add to the main pool if it's not full
            if (_pool.Count < _maxPoolSize)
            {
                _pool.Add(writer);
            }
            else if (size > 0 && GetTotalWritersInBuckets() < _maxTotalBucketWriters)
            {
                // Optionally, place in a specific size bucket if the global cap isn't exceeded
                if (_sizeBuckets.TryGetValue(size, out var sizeBucket))
                {
                    if (sizeBucket.Count < _maxBucketSize)
                    {
                        sizeBucket.Add(writer);
                    }
                }
                else
                {
                    // Create a new bucket if needed
                    var newBucket = new ConcurrentBag<NetDataWriter> { writer };
                    _sizeBuckets[size] = newBucket;
                }
            }
            else
            {
                // If both the pool and buckets are too large, discard the writer
                BNL.LogError("Writer discarded due to exceeding pool limits!");
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
            // Remove excess writers from the main pool
            while (_pool.Count > _maxPoolSize)
            {
                _pool.TryTake(out _);
            }

            // Clean up size buckets if they are too large
            foreach (var bucket in _sizeBuckets.Values)
            {
                while (bucket.Count > _maxBucketSize)
                {
                    bucket.TryTake(out _);
                }
            }

            BNL.Log($"Cleanup completed. Pool size: {_pool.Count}, Active writers: {_activeWriters}, Total bucket writers: {GetTotalWritersInBuckets()}");
        }

        /// <summary>
        /// Gets the total number of writers across all size buckets.
        /// </summary>
        /// <returns>Total count of size-bucket writers.</returns>
        private static int GetTotalWritersInBuckets()
        {
            int total = 0;
            foreach (var bucket in _sizeBuckets.Values)
            {
                total += bucket.Count;
            }
            return total;
        }
    }
}
