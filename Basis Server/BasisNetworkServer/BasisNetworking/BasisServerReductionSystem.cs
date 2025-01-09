using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Basis.Network.Core;
using Basis.Network.Core.Compression;
using Basis.Scripts.Networking.Compression;
using LiteNetLib;
using LiteNetLib.Utils;
using static SerializableBasis;
public partial class BasisServerReductionSystem
{
    // Default interval in milliseconds for the timer
    public static Configuration Configuration;
    public static ConcurrentDictionary<int, SyncedToPlayerPulse> PlayerSync = new ConcurrentDictionary<int, SyncedToPlayerPulse>();
    /// <summary>
    /// add the new client
    /// then update all existing clients arrays
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="playerToUpdate"></param>
    /// <param name="serverSideSyncPlayer"></param>
    public static void AddOrUpdatePlayer(NetPeer playerID, ServerSideSyncPlayerMessage playerToUpdate, NetPeer serverSideSyncPlayer)
    {
        //stage 1 lets update whoever send us this datas last player information
        if (PlayerSync.TryGetValue(serverSideSyncPlayer.Id, out SyncedToPlayerPulse playerData))
        {
            playerData.lastPlayerInformation = playerToUpdate;
            playerData.Position = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(playerToUpdate);
        }
        //ok now we can try to schedule sending out this data!
        if (PlayerSync.TryGetValue(playerID.Id, out playerData))
        {
            // Update the player's message
            playerData.SupplyNewData(playerID, playerToUpdate, serverSideSyncPlayer);
        }
        else
        {
            //first time request create said data!
            playerData = new SyncedToPlayerPulse
            {
                //   playerID = playerID,
                lastPlayerInformation = playerToUpdate,
                Position = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(playerToUpdate),
            };
            //ok now we can try to schedule sending out this data!
            if (PlayerSync.TryAdd(playerID.Id, playerData))
            {  // Update the player's message
                playerData.SupplyNewData(playerID, playerToUpdate, serverSideSyncPlayer);
            }
        }
    }
    public static void RemovePlayer(NetPeer playerID)
    {
        if (PlayerSync.TryRemove(playerID.Id, out SyncedToPlayerPulse pulse))
        {
            for (int i = 0; i < 1024; i++)
            {
                ServerSideReducablePlayer player = pulse.ChunkedServerSideReducablePlayerArray.GetPlayer(i);
                if (player != null)
                {
                    player.timer.Dispose();
                    pulse.ChunkedServerSideReducablePlayerArray.SetPlayer(i, null);
                }
            }
        }
        SyncedToPlayerPulse[] Index = PlayerSync.Values.ToArray();
        for (int i = 0; i < Index.Length; i++)
        {
            SyncedToPlayerPulse player = Index[i];
            ServerSideReducablePlayer SSRP = player.ChunkedServerSideReducablePlayerArray.GetPlayer(i);
            if (SSRP != null)
            {
                SSRP.timer.Dispose();
                pulse.ChunkedServerSideReducablePlayerArray.SetPlayer(i, null);
            }
        }
    }
    /// <summary>
    /// Structure to synchronize data with a specific player.
    /// </summary>
    public class SyncedToPlayerPulse
    {
        // The player ID to which the data is being sent
        // public NetPeer playerID;
        public ServerSideSyncPlayerMessage lastPlayerInformation;
        public Vector3 Position;
        /// <summary>
        /// Dictionary to hold queued messages for each player.
        /// Key: Player ID, Value: Server-side player data
        /// </summary>
        public ChunkedBoolArray SyncBoolArray = new ChunkedBoolArray(64);
        public ChunkedServerSideReducablePlayerArray ChunkedServerSideReducablePlayerArray = new ChunkedServerSideReducablePlayerArray(64);
        /// <summary>
        /// Supply new data to a specific player.
        /// </summary>
        /// <param name="playerID">The ID of the player</param>
        /// <param name="serverSideSyncPlayerMessage">The message to be synced</param>
        /// <param name="serverSidePlayer"></param>
        public void SupplyNewData(NetPeer playerID, ServerSideSyncPlayerMessage serverSideSyncPlayerMessage, NetPeer serverSidePlayer)
        {
            ServerSideReducablePlayer playerData = ChunkedServerSideReducablePlayerArray.GetPlayer(serverSidePlayer.Id);
            if (playerData != null)
            {
                // Update the player's message
                playerData.serverSideSyncPlayerMessage = serverSideSyncPlayerMessage;
                playerData.Position = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(serverSideSyncPlayerMessage);
                SyncBoolArray.SetBool(serverSidePlayer.Id, true);
                ChunkedServerSideReducablePlayerArray.SetPlayer(serverSidePlayer.Id, playerData);
            }
            else
            {
                // If the player doesn't exist, add them with default settings
                AddPlayer(playerID, serverSideSyncPlayerMessage, serverSidePlayer);
            }
        }

        /// <summary>
        /// Adds a new player to the queue with a default timer and settings.
        /// </summary>
        /// <param name="playerID">The ID of the player</param>
        /// <param name="serverSideSyncPlayerMessage">The initial message to be sent</param>
        /// <param name="serverSidePlayer"></param>
        public void AddPlayer(NetPeer playerID, ServerSideSyncPlayerMessage serverSideSyncPlayerMessage, NetPeer serverSidePlayer)
        {
            ClientPayload clientPayload = new ClientPayload
            {
                localClient = playerID,
                dataCameFromThisUser = serverSidePlayer.Id
            };
            serverSideSyncPlayerMessage.interval = (byte)Configuration.BSRSMillisecondDefaultInterval;
            ServerSideReducablePlayer newPlayer = new ServerSideReducablePlayer
            {
                serverSideSyncPlayerMessage = serverSideSyncPlayerMessage,
                timer = new ManagedTimer(SendPlayerData, clientPayload, Configuration.BSRSMillisecondDefaultInterval, Configuration.BSRSMillisecondDefaultInterval),
                Writer = new NetDataWriter(true, 204),
                Position = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(serverSideSyncPlayerMessage),
            };
            SendPlayerData(clientPayload);
            SyncBoolArray.SetBool(serverSidePlayer.Id, true);
            ChunkedServerSideReducablePlayerArray.SetPlayer(serverSidePlayer.Id, newPlayer);
        }
        public struct ClientPayload
        {
            public NetPeer localClient;
            public int dataCameFromThisUser;
        }
        /// <summary>
        /// Callback function to send player data at regular intervals.
        /// </summary>
        /// <param name="state">The player ID (passed from the timer)</param>
        private void SendPlayerData(object state)
        {
            var playerID = (ClientPayload)state;
            if (SyncBoolArray.GetBool(playerID.dataCameFromThisUser))
            {
                ServerSideReducablePlayer playerData = ChunkedServerSideReducablePlayerArray.GetPlayer(playerID.dataCameFromThisUser);
                if (playerData != null)
                {
                    if (PlayerSync.TryGetValue(playerID.localClient.Id, out SyncedToPlayerPulse pulse))
                    {
                        try
                        {
                            // Calculate the distance between the two points
                            float activeDistance = Distance(pulse.Position, playerData.Position);
                            // Adjust the timer interval based on the new syncRateMultiplier
                            int adjustedInterval = (int)(Configuration.BSRSMillisecondDefaultInterval * (Configuration.BSRBaseMultiplier + (activeDistance * Configuration.BSRSIncreaseRate)));
                            if (adjustedInterval > byte.MaxValue)
                            {
                                adjustedInterval = byte.MaxValue;
                            }
                            if (playerData.serverSideSyncPlayerMessage.interval != adjustedInterval)
                            {
                                //  Console.WriteLine("Adjusted Interval is" + adjustedInterval);
                                playerData.timer.Change(adjustedInterval, adjustedInterval);
                            }
                            //how long does this data need to last for
                            playerData.serverSideSyncPlayerMessage.interval = (byte)adjustedInterval;
                            playerData.serverSideSyncPlayerMessage.Serialize(playerData.Writer);
                            playerID.localClient.Send(playerData.Writer, BasisNetworkCommons.MovementChannel, DeliveryMethod.Sequenced);
                            playerData.Writer.Reset();
                        }
                        catch (Exception e)
                        {
                            BNL.LogError("Server Reduction System Encounter Isssue " + e.Message + " " + e.StackTrace);
                        }
                    }
                }
                SyncBoolArray.SetBool(playerID.dataCameFromThisUser, false);
            }
        }
    }
    public static float Distance(Vector3 pointA, Vector3 pointB)
    {
        return (pointB - pointA).SquaredMagnitude(); // Avoid intermediate objects if possible
    }

    /// <summary>
    /// Structure representing a player's server-side data that can be reduced.
    /// </summary>
    public class ServerSideReducablePlayer
    {
        public ManagedTimer timer;//create a new timer
        public ServerSideSyncPlayerMessage serverSideSyncPlayerMessage;
        public NetDataWriter Writer;
        public Vector3 Position;
    }
    public class ManagedTimer : IDisposable
    {
        private Timer _timer;
        public bool IsDisposed = false;

        public ManagedTimer(TimerCallback callback, object state, int dueTime, int period)
        {
            _timer = new System.Threading.Timer(callback, state, dueTime, period);
        }
        public void Dispose()
        {
            if (!IsDisposed)
            {
                _timer.Dispose();
                IsDisposed = true;
            }
        }

        public bool Change(int dueTime, int period)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ManagedTimer));
            }
            return _timer.Change(dueTime, period);
        }
    }
}
