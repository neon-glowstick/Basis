using LiteNetLib;
using System;
public static partial class BasisNetworkServer
{
    public class ChunkedNetPeerArray
    {
        private readonly object[] _chunkLocks; // Locks for each chunk
        private readonly NetPeer[][] _chunks;  // Array divided into chunks
        private readonly ushort _chunkSize;    // Number of elements in each chunk
        public const ushort totalSize = 1024;  // Total size

        public ChunkedNetPeerArray(ushort chunkSize = 256)
        {
            if (totalSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalSize), "Total size must be greater than zero.");
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");

            _chunkSize = chunkSize;
            ushort numChunks = (ushort)Math.Ceiling((double)totalSize / chunkSize); // Now using ushort
            _chunks = new NetPeer[numChunks][];
            _chunkLocks = new object[numChunks];

            for (ushort i = 0; i < numChunks; i++) // Changed to ushort
            {
                _chunks[i] = new NetPeer[chunkSize];
                _chunkLocks[i] = new object();
            }
        }

        public void SetPeer(ushort index, NetPeer value)
        {
            if (index < 0 || index >= _chunkSize * _chunks.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            ushort chunkIndex = (ushort)(index / _chunkSize); // Changed to ushort
            ushort localIndex = (ushort)(index % _chunkSize); // Changed to ushort

            lock (_chunkLocks[chunkIndex])
            {
                _chunks[chunkIndex][localIndex] = value;
            }
        }

        public NetPeer GetPeer(ushort index)
        {
            if (index < 0 || index >= _chunkSize * _chunks.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            ushort chunkIndex = (ushort)(index / _chunkSize); // Changed to ushort
            ushort localIndex = (ushort)(index % _chunkSize); // Changed to ushort

            lock (_chunkLocks[chunkIndex])
            {
                return _chunks[chunkIndex][localIndex];
            }
        }
    }
}
