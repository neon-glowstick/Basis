using LiteNetLib;
using System.Collections.Concurrent;
using static SerializableBasis;

namespace Basis.Network.Server.Generic
{
    public static class BasisSavedState
    {
        // Separate dictionaries for each type of data
        private static readonly ConcurrentDictionary<int, LocalAvatarSyncMessage> avatarSyncStates = new ConcurrentDictionary<int, LocalAvatarSyncMessage>();
        private static readonly ConcurrentDictionary<int, ClientAvatarChangeMessage> avatarChangeStates = new ConcurrentDictionary<int, ClientAvatarChangeMessage>();
        private static readonly ConcurrentDictionary<int, PlayerMetaDataMessage> playerMetaDataMessages = new ConcurrentDictionary<int, PlayerMetaDataMessage>();
        private static readonly ConcurrentDictionary<int, VoiceReceiversMessage> voiceReceiversMessages = new ConcurrentDictionary<int, VoiceReceiversMessage>();

        /// <summary>
        /// Removes all state data for a specific player.
        /// </summary>
        public static void RemovePlayer(NetPeer client)
        {
            avatarSyncStates.TryRemove(client.Id, out _);
            avatarChangeStates.TryRemove(client.Id, out _);
            playerMetaDataMessages.TryRemove(client.Id, out _);
            voiceReceiversMessages.TryRemove(client.Id, out _);
        }

        /// <summary>
        /// Adds or updates the LocalAvatarSyncMessage for a player.
        /// </summary>
        public static void AddLastData(NetPeer client, LocalAvatarSyncMessage avatarSyncMessage)
        {
            avatarSyncStates[client.Id] = avatarSyncMessage;
        }

        /// <summary>
        /// Adds or updates the ReadyMessage for a player.
        /// </summary>
        public static void AddLastData(NetPeer client, ReadyMessage readyMessage)
        {
            avatarSyncStates[client.Id] = readyMessage.localAvatarSyncMessage;
            avatarChangeStates[client.Id] = readyMessage.clientAvatarChangeMessage;
            playerMetaDataMessages[client.Id] = readyMessage.playerMetaDataMessage;

            BNL.Log($"Updated {client.Id} with AvatarID {readyMessage.clientAvatarChangeMessage.byteArray.Length}");
        }

        /// <summary>
        /// Adds or updates the VoiceReceiversMessage for a player.
        /// </summary>
        public static void AddLastData(NetPeer client, VoiceReceiversMessage voiceReceiversMessage)
        {
            voiceReceiversMessages[client.Id] = voiceReceiversMessage;
        }

        /// <summary>
        /// Adds or updates the ClientAvatarChangeMessage for a player.
        /// </summary>
        public static void AddLastData(NetPeer client, ClientAvatarChangeMessage avatarChangeMessage)
        {
            avatarChangeStates[client.Id] = avatarChangeMessage;
        }

        /// <summary>
        /// Retrieves the last LocalAvatarSyncMessage for a player.
        /// </summary>
        public static bool GetLastAvatarSyncState(NetPeer client, out LocalAvatarSyncMessage message) =>
            avatarSyncStates.TryGetValue(client.Id, out message);

        /// <summary>
        /// Retrieves the last ClientAvatarChangeMessage for a player.
        /// </summary>
        public static bool GetLastAvatarChangeState(NetPeer client, out ClientAvatarChangeMessage message) =>
            avatarChangeStates.TryGetValue(client.Id, out message);

        /// <summary>
        /// Retrieves the last PlayerMetaDataMessage for a player.
        /// </summary>
        public static bool GetLastPlayerMetaData(NetPeer client, out PlayerMetaDataMessage message) =>
            playerMetaDataMessages.TryGetValue(client.Id, out message);

        /// <summary>
        /// Retrieves the last VoiceReceiversMessage for a player.
        /// </summary>
        public static bool GetLastVoiceReceivers(NetPeer client, out VoiceReceiversMessage message) =>
            voiceReceiversMessages.TryGetValue(client.Id, out message);
    }
}
