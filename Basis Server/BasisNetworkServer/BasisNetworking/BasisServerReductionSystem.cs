using System;
using System.Collections.Concurrent;
using Basis.Network.Core;
using Basis.Network.Core.Compression;
using Basis.Scripts.Networking.Compression;
using BasisNetworkCore;
using LiteNetLib;
using LiteNetLib.Utils;
using static SerializableBasis;
public partial class BasisServerReductionSystem
{
    // Default interval in milliseconds for the timer
    public static Configuration Configuration;
    public static ConcurrentDictionary<NetPeer, SyncedToPlayerPulse> PlayerSync = new ConcurrentDictionary<NetPeer, SyncedToPlayerPulse>();
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
        if (PlayerSync.TryGetValue(serverSideSyncPlayer, out SyncedToPlayerPulse playerData))
        {
            playerData.lastPlayerInformation = playerToUpdate;
        }
        //ok now we can try to schedule sending out this data!
        if (PlayerSync.TryGetValue(playerID, out playerData))
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
                queuedPlayerMessages = new ConcurrentDictionary<NetPeer, ServerSideReducablePlayer>(),
                lastPlayerInformation = playerToUpdate,
            };
            //ok now we can try to schedule sending out this data!
            if (PlayerSync.TryAdd(playerID, playerData))
            {  // Update the player's message
                playerData.SupplyNewData(playerID, playerToUpdate, serverSideSyncPlayer);
            }
        }
    }
    public static void RemovePlayer(NetPeer playerID)
    {
        if (PlayerSync.TryRemove(playerID, out SyncedToPlayerPulse pulse))
        {
            foreach (ServerSideReducablePlayer player in pulse.queuedPlayerMessages.Values)
            {
                player.timer.Dispose();
            }
        }
        foreach (SyncedToPlayerPulse player in PlayerSync.Values)
        {
            if (player.queuedPlayerMessages.TryRemove(playerID, out ServerSideReducablePlayer reduceablePlayer))
            {
                reduceablePlayer.timer.Dispose();
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
        /// <summary>
        /// Dictionary to hold queued messages for each player.
        /// Key: Player ID, Value: Server-side player data
        /// </summary>
        public ConcurrentDictionary<NetPeer, ServerSideReducablePlayer> queuedPlayerMessages = new ConcurrentDictionary<NetPeer, ServerSideReducablePlayer>();
        public ChunkedBoolArray SyncBoolArray = new ChunkedBoolArray(64);
        /// <summary>
        /// Supply new data to a specific player.
        /// </summary>
        /// <param name="playerID">The ID of the player</param>
        /// <param name="serverSideSyncPlayerMessage">The message to be synced</param>
        /// <param name="serverSidePlayer"></param>
        public void SupplyNewData(NetPeer playerID, ServerSideSyncPlayerMessage serverSideSyncPlayerMessage, NetPeer serverSidePlayer)
        {
            if (queuedPlayerMessages.TryGetValue(serverSidePlayer, out ServerSideReducablePlayer playerData))
            {
                // Update the player's message
                playerData.serverSideSyncPlayerMessage = serverSideSyncPlayerMessage;
                SyncBoolArray.SetBool(serverSidePlayer.Id, true);
                queuedPlayerMessages[serverSidePlayer] = playerData;
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
                dataCameFromThisUser = serverSidePlayer
            };
            ServerSideReducablePlayer newPlayer = new ServerSideReducablePlayer
            {
                serverSideSyncPlayerMessage = serverSideSyncPlayerMessage,
                timer = new System.Threading.Timer(SendPlayerData, clientPayload, Configuration.BSRSMillisecondDefaultInterval, Configuration.BSRSMillisecondDefaultInterval),
                Writer = new NetDataWriter(true, 204),
            };
            SyncBoolArray.SetBool(serverSidePlayer.Id, true);
            queuedPlayerMessages[serverSidePlayer] = newPlayer;
        }

        /// <summary>
        /// Removes a player from the queue and disposes of their timer.
        /// </summary>
        /// <param name="playerID">The ID of the player to remove</param>
        public void RemovePlayer(NetPeer playerID)
        {
            if (queuedPlayerMessages.TryRemove(playerID, out ServerSideReducablePlayer playerData))
            {
                // Dispose of the timer to free resources
                playerData.timer.Dispose();
            }
        }
        public struct ClientPayload
        {
            public NetPeer localClient;
            public NetPeer dataCameFromThisUser;
        }
        /// <summary>
        /// Callback function to send player data at regular intervals.
        /// </summary>
        /// <param name="state">The player ID (passed from the timer)</param>
        private void SendPlayerData(object state)
        {
            if (state is ClientPayload playerID)
            {
                if (SyncBoolArray.GetBool(playerID.dataCameFromThisUser.Id))
                {
                    if (queuedPlayerMessages.TryGetValue(playerID.dataCameFromThisUser, out ServerSideReducablePlayer playerData))
                    {
                        if (PlayerSync.TryGetValue(playerID.localClient, out SyncedToPlayerPulse pulse))
                        {
                            try
                            {
                                Vector3 from = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(pulse.lastPlayerInformation);
                                Vector3 to = BasisNetworkCompressionExtensions.DecompressAndProcessAvatar(playerData.serverSideSyncPlayerMessage);
                                // Calculate the distance between the two points
                                float activeDistance = Distance(from, to);
                                // Adjust the timer interval based on the new syncRateMultiplier
                                int adjustedInterval = (int)(Configuration.BSRSMillisecondDefaultInterval * (Configuration.BSRBaseMultiplier + (activeDistance * Configuration.BSRSIncreaseRate)));
                                if (adjustedInterval > byte.MaxValue)
                                {
                                    adjustedInterval = byte.MaxValue;
                                }
                                //  Console.WriteLine("Adjusted Interval is" + adjustedInterval);
                                playerData.timer.Change(adjustedInterval, adjustedInterval);
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
                        SyncBoolArray.SetBool(playerID.dataCameFromThisUser.Id, false);
                    }
                }
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
    public struct ServerSideReducablePlayer
    {
        public System.Threading.Timer timer;//create a new timer
        public ServerSideSyncPlayerMessage serverSideSyncPlayerMessage;
        public NetDataWriter Writer;
    }
}
