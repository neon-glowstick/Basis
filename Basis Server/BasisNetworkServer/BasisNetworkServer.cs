using Basis.Network.Core;
using Basis.Network.Server;
using Basis.Network.Server.Auth;
using BasisServerHandle;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
public static class BasisNetworkServer
{
    public static EventBasedNetListener listener;
    public static NetManager server;
    public static ConcurrentDictionary<ushort, NetPeer> Peers = new ConcurrentDictionary<ushort, NetPeer>();
    public static ChunkedNetPeerArray chunkedNetPeerArray = new ChunkedNetPeerArray();
    public static Configuration Configuration;
    public static IAuth auth;
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
    public static void StartServer(Configuration configuration)
    {
        Configuration = configuration;
        BasisServerReductionSystem.Configuration = configuration;
        auth = new PasswordAuth(configuration.Password ?? string.Empty);

        SetupServer(configuration);
        BasisServerHandleEvents.SubscribeServerEvents();

        if (configuration.EnableStatistics)
        {
            BasisStatistics.StartWorkerThread(BasisNetworkServer.server);
        }
        BNL.Log("Server Worker Threads Booted");

    }
    #region Server Setup
    public static void SetupServer(Configuration configuration)
    {
        listener = new EventBasedNetListener();
        server = new NetManager(listener)
        {
            AutoRecycle = false,
            UnconnectedMessagesEnabled = false,
            NatPunchEnabled = configuration.NatPunchEnabled,
            AllowPeerAddressChange = configuration.AllowPeerAddressChange,
            BroadcastReceiveEnabled = false,
            UseNativeSockets = configuration.UseNativeSockets,
            ChannelsCount = BasisNetworkCommons.TotalChannels,
            EnableStatistics = configuration.EnableStatistics,
            IPv6Enabled = configuration.IPv6Enabled,
            UpdateTime = BasisNetworkCommons.NetworkIntervalPoll,
            PingInterval = configuration.PingInterval,
            DisconnectTimeout = configuration.DisconnectTimeout,
            PacketPoolSize = 2000,
            UnsyncedEvents = true,

        };

        StartListening(configuration);
    }

    public static void StartListening(Configuration configuration)
    {
        if (configuration.OverrideAutoDiscoveryOfIpv)
        {
            BNL.Log("Server Wiring up SetPort " + Configuration.SetPort + "IPv6Address " + Configuration.IPv6Address);
            server.Start(Configuration.IPv4Address, Configuration.IPv6Address, Configuration.SetPort);
        }
        else
        {
            BNL.Log("Server Wiring up SetPort " + Configuration.SetPort);
            server.Start(Configuration.SetPort);
        }
    }
    #endregion
    public static void BroadcastMessageToClients(NetDataWriter Reader, byte channel, NetPeer sender, ReadOnlySpan<NetPeer> authenticatedClients, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
    {
        foreach (NetPeer client in authenticatedClients)
        {
            if (client.Id != sender.Id)
            {
                client.Send(Reader, channel, deliveryMethod);
            }
        }
    }
    public static void BroadcastMessageToClients(NetDataWriter Reader, byte channel, ReadOnlySpan<NetPeer> authenticatedClients, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
    {
        int count = authenticatedClients.Length;
        for (int index = 0; index < count; index++)
        {
            authenticatedClients[index].Send(Reader, channel, deliveryMethod);
        }
    }
    public static void BroadcastMessageToClients(NetDataWriter Reader, byte channel, ref List<NetPeer> authenticatedClients, DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced)
    {
        int count = authenticatedClients.Count;
        for (int index = 0; index < count; index++)
        {
            authenticatedClients[index].Send(Reader, channel, deliveryMethod);
        }
    }
}
