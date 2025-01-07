using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using LiteNetLib;
using System.Collections.Concurrent;
using BasisNetworkCore;
using LiteNetLib.Utils;
using Basis.Network.Core;
using static Basis.Network.Core.Serializable.SerializableBasis;
using Basis.Network.Server.Ownership;
using Basis.Network.Server.Generic;
using static SerializableBasis;
using System.Collections.Generic;
using System.Linq;

namespace BasisServerHandle
{
    public static class BasisServerHandleEvents
    {
        #region Server Events Setup
        public static void SubscribeServerEvents()
        {
            BasisNetworkServer.listener.ConnectionRequestEvent += HandleConnectionRequest;
            BasisNetworkServer.listener.PeerDisconnectedEvent += HandlePeerDisconnected;
            BasisNetworkServer.listener.NetworkReceiveEvent += HandleNetworkReceiveEvent;
            BasisNetworkServer.listener.NetworkErrorEvent += OnNetworkError;
        }

        public static void UnsubscribeServerEvents()
        {
            BasisNetworkServer.listener.ConnectionRequestEvent -= HandleConnectionRequest;
            BasisNetworkServer.listener.PeerDisconnectedEvent -= HandlePeerDisconnected;
            BasisNetworkServer.listener.NetworkReceiveEvent -= HandleNetworkReceiveEvent;
            BasisNetworkServer.listener.NetworkErrorEvent -= OnNetworkError;
        }

        public static void StopWorker()
        {
            BasisNetworkServer.server?.Stop();
            BasisServerHandleEvents.UnsubscribeServerEvents();
        }
        #endregion

        #region Network Event Handlers

        public static void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            BNL.LogError($"Endpoint {endPoint.ToString()} was reported with error {socketError}");
        }
        #endregion

        #region Peer Connection and Disconnection
        public static void HandlePeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            try
            {
                ushort id = (ushort)peer.Id;
                ClientDisconnect(id, BasisNetworkServer.Peers);

                BasisPlayerArray.RemovePlayer(peer);
                if (BasisNetworkServer.Peers.TryRemove(id, out _))
                {
                    BNL.Log($"Peer removed: {id}");
                }
                else
                {
                    BNL.LogError($"Failed to remove peer: {id}");
                }
                CleanupPlayerData(id, peer);
            }
            catch (Exception e)
            {
                BNL.LogError(e.Message + " " + e.StackTrace);
            }
        }

        public static void CleanupPlayerData(ushort id, NetPeer peer)
        {
            BasisNetworkOwnership.RemovePlayerOwnership(id);
            BasisSavedState.RemovePlayer(peer);
            BasisServerReductionSystem.RemovePlayer(peer);
        }
        #endregion

        #region Utility Methods
        private static void RejectWithReason(ConnectionRequest request, string reason)
        {
            NetDataWriter writer = NetDataWriterPool.GetWriter();
            writer.Put(reason);
            request.Reject(writer);
            BNL.LogError($"Rejected: {reason}");
            NetDataWriterPool.ReturnWriter(writer);
        }

        public static void ClientDisconnect(ushort leaving, ConcurrentDictionary<ushort, NetPeer> authenticatedClients)
        {
            NetDataWriter writer = NetDataWriterPool.GetWriter(sizeof(ushort));
            writer.Put(leaving);

            foreach (var client in authenticatedClients.Values)
            {
                if (client.Id != leaving)
                {
                    client.Send(writer, BasisNetworkCommons.Disconnection, DeliveryMethod.ReliableOrdered);
                }
            }
            NetDataWriterPool.ReturnWriter(writer);
        }
        #endregion

        #region Connection Handling
        public static void HandleConnectionRequest(ConnectionRequest request)
        {
            try
            {
                BNL.Log("Processing Connection Request");
                int ServerCount = BasisNetworkServer.server.ConnectedPeersCount;

                if (ServerCount >= BasisNetworkServer.Configuration.PeerLimit)
                {
                    RejectWithReason(request, "Server is full! Rejected.");
                    return;
                }

                if (!request.Data.TryGetUShort(out ushort ClientVersion))
                {
                    RejectWithReason(request, "Invalid client data.");
                    return;
                }

                if (ClientVersion < BasisNetworkVersion.ServerVersion)
                {
                    RejectWithReason(request, "Outdated client version.");
                    return;
                }

                AuthenticationMessage authMessage = new AuthenticationMessage();
                authMessage.Deserialize(request.Data);

                if (BasisNetworkServer.auth.IsAuthenticated(authMessage) == false)
                {
                    RejectWithReason(request, "Authentication failed, password rejected");
                    return;
                }

                BNL.Log("Player approved. Current count: " + ServerCount);

                NetPeer newPeer = request.Accept();
                if (BasisNetworkServer.Peers.TryAdd((ushort)newPeer.Id, newPeer))
                {
                    BasisPlayerArray.AddPlayer(newPeer);
                    BNL.Log($"Peer connected: {newPeer.Id}");
                    ReadyMessage readyMessage = new ReadyMessage();
                    readyMessage.Deserialize(request.Data);
                    if (readyMessage.WasDeserializedCorrectly())
                    {
                        SendRemoteSpawnMessage(newPeer, readyMessage);
                    }
                    else
                    {
                        RejectWithReason(request, "Payload Provided was invalid!");
                    }
                }
                else
                {
                    RejectWithReason(request, "Peer already exists.");
                }
            }
            catch (Exception e)
            {
                RejectWithReason(request, "Fatal Connection Issue stacktrace on server " + e.Message);
                BNL.LogError(e.StackTrace);
            }
        }
        #endregion

        #region Network Receive Handlers
        private static void HandleNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                switch (channel)
                {
                    case BasisNetworkCommons.FallChannel:
                        if (deliveryMethod == DeliveryMethod.Unreliable)
                        {
                            if (reader.TryGetByte(out byte Byte))
                            {
                                HandleNetworkReceiveEvent(peer, reader, Byte, deliveryMethod);
                            }
                            else
                            {
                                BNL.LogError($"Unknown channel no data remains: {channel} " + reader.AvailableBytes);
                                reader.Recycle();
                            }
                        }
                        else
                        {
                            BNL.LogError($"Unknown channel: {channel} " + reader.AvailableBytes);
                            reader.Recycle();
                        }
                        break;
                    case BasisNetworkCommons.MovementChannel:
                        HandleAvatarMovement(reader, peer);
                        break;
                    case BasisNetworkCommons.VoiceChannel:
                        HandleVoiceMessage(reader, peer);
                        break;
                    case BasisNetworkCommons.AvatarChannel:
                        BasisNetworkingGeneric.HandleAvatar(reader, deliveryMethod, peer);
                        break;
                    case BasisNetworkCommons.SceneChannel:
                        BasisNetworkingGeneric.HandleScene(reader, deliveryMethod, peer);
                        break;
                    case BasisNetworkCommons.AvatarChangeMessage:
                        SendAvatarMessageToClients(reader, peer);
                        break;
                    case BasisNetworkCommons.OwnershipTransfer:
                        BasisNetworkOwnership.OwnershipTransfer(reader, peer);
                        break;
                    case BasisNetworkCommons.OwnershipResponse:
                        BasisNetworkOwnership.OwnershipResponse(reader, peer);
                        break;
                    case BasisNetworkCommons.AudioRecipients:
                        UpdateVoiceReceivers(reader, peer);
                        break;
                    default:
                        BNL.LogError($"Unknown channel: {channel} " + reader.AvailableBytes);
                        reader.Recycle();
                        break;
                }
            }
            catch (Exception e)
            {
                BNL.LogError($"{e.Message} : {e.StackTrace}");
                if (reader != null)
                {
                    reader.Recycle();
                }
            }
        }
        #endregion

        #region Avatar and Voice Handling
        public static void SendAvatarMessageToClients(NetPacketReader Reader, NetPeer Peer)
        {
            ClientAvatarChangeMessage ClientAvatarChangeMessage = new ClientAvatarChangeMessage();
            ClientAvatarChangeMessage.Deserialize(Reader);
            Reader.Recycle();
            ServerAvatarChangeMessage serverAvatarChangeMessage = new ServerAvatarChangeMessage
            {
                clientAvatarChangeMessage = ClientAvatarChangeMessage,
                uShortPlayerId = new PlayerIdMessage
                {
                    playerID = (ushort)Peer.Id
                }
            };
            BasisSavedState.AddLastData(Peer, ClientAvatarChangeMessage);
            NetDataWriter Writer = NetDataWriterPool.GetWriter();
            serverAvatarChangeMessage.Serialize(Writer);
            BasisNetworkServer.BroadcastMessageToClients(Writer, BasisNetworkCommons.AvatarChangeMessage, Peer, BasisPlayerArray.GetSnapshot());
            NetDataWriterPool.ReturnWriter(Writer);
        }
        public static void HandleAvatarMovement(NetPacketReader Reader, NetPeer Peer)
        {
            LocalAvatarSyncMessage LocalAvatarSyncMessage = new LocalAvatarSyncMessage();
            LocalAvatarSyncMessage.Deserialize(Reader);
            Reader.Recycle();
            BasisSavedState.AddLastData(Peer, LocalAvatarSyncMessage);
            ReadOnlySpan<NetPeer> Peers = BasisPlayerArray.GetSnapshot();
            foreach (NetPeer client in Peers)
            {
                if (client.Id == Peer.Id)
                {
                    continue;
                }
                ServerSideSyncPlayerMessage ssspm = CreateServerSideSyncPlayerMessage(LocalAvatarSyncMessage, (ushort)Peer.Id);
                BasisServerReductionSystem.AddOrUpdatePlayer(client, ssspm, Peer);
            }
        }

        public static ServerSideSyncPlayerMessage CreateServerSideSyncPlayerMessage(LocalAvatarSyncMessage local, ushort clientId)
        {
            return new ServerSideSyncPlayerMessage
            {
                playerIdMessage = new PlayerIdMessage { playerID = clientId },
                avatarSerialization = local
            };
        }

        public static void HandleVoiceMessage(NetPacketReader Reader, NetPeer peer)
        {
            AudioSegmentDataMessage audioSegment = new AudioSegmentDataMessage();
            audioSegment.Deserialize(Reader);
            Reader.Recycle();
            ServerAudioSegmentMessage ServerAudio = new ServerAudioSegmentMessage
            {
                audioSegmentData = audioSegment
            };
            SendVoiceMessageToClients(ServerAudio, BasisNetworkCommons.VoiceChannel, peer);
        }

        public static void SendVoiceMessageToClients(ServerAudioSegmentMessage audioSegment, byte channel, NetPeer sender)
        {
            if (BasisSavedState.GetLastVoiceReceivers(sender, out VoiceReceiversMessage data))
            {
                if (data.users == null || data.users.Length == 0)
                {
                    return;
                }

                List<NetPeer> endPoints = data.users.Select(user => BasisNetworkServer.Peers.GetValueOrDefault(user)).Where(client => client != null).ToList();

                if (endPoints.Count == 0)
                {
                    return;
                }

                audioSegment.playerIdMessage = new PlayerIdMessage
                {
                    playerID = (ushort)sender.Id
                };

                NetDataWriter NetDataWriter = NetDataWriterPool.GetWriter();
                audioSegment.Serialize(NetDataWriter);
                BasisNetworkServer.BroadcastMessageToClients(NetDataWriter, channel, ref endPoints, DeliveryMethod.Sequenced);
                NetDataWriterPool.ReturnWriter(NetDataWriter);
            }
            else
            {
                BNL.Log("Error unable to find " + sender.Id + " in the data store!");
            }
        }

        public static void UpdateVoiceReceivers(NetPacketReader Reader, NetPeer Peer)
        {
            VoiceReceiversMessage VoiceReceiversMessage = new VoiceReceiversMessage();
            VoiceReceiversMessage.Deserialize(Reader);
            Reader.Recycle();
            BasisSavedState.AddLastData(Peer, VoiceReceiversMessage);
        }
        #endregion

        #region Spawn and Client List Handling
        public static void SendRemoteSpawnMessage(NetPeer authClient, ReadyMessage readyMessage)
        {
            ServerReadyMessage serverReadyMessage = LoadInitialState(authClient, readyMessage);
            NotifyExistingClients(serverReadyMessage, authClient);
            SendClientListToNewClient(authClient);
        }

        public static ServerReadyMessage LoadInitialState(NetPeer authClient, ReadyMessage readyMessage)
        {
            ServerReadyMessage serverReadyMessage = new ServerReadyMessage
            {
                localReadyMessage = readyMessage,
                playerIdMessage = new PlayerIdMessage() { playerID = (ushort)authClient.Id }
            };
            BasisSavedState.AddLastData(authClient, readyMessage);
            return serverReadyMessage;
        }

        public static void NotifyExistingClients(ServerReadyMessage serverSideSyncPlayerMessage, NetPeer authClient)
        {
            NetDataWriter Writer = NetDataWriterPool.GetWriter();
            serverSideSyncPlayerMessage.Serialize(Writer);
            ReadOnlySpan<NetPeer> Peers = BasisPlayerArray.GetSnapshot();

            foreach (NetPeer client in Peers)
            {
                if (client != authClient)
                {
                    client.Send(Writer, BasisNetworkCommons.CreateRemotePlayer, DeliveryMethod.ReliableOrdered);
                }
            }
            NetDataWriterPool.ReturnWriter(Writer);
        }

        public static void SendClientListToNewClient(NetPeer authClient)
        {
            if (BasisNetworkServer.Peers.Count > ushort.MaxValue)
            {
                BNL.Log($"authenticatedClients count exceeds {ushort.MaxValue}");
                return;
            }

            List<ServerReadyMessage> copied = new List<ServerReadyMessage>();

            IEnumerable<NetPeer> clientsToNotify = BasisNetworkServer.Peers.Values.Where(client => client != authClient);
            BNL.Log("Notifing Newly Connected Client about " + clientsToNotify.Count());
            foreach (NetPeer client in clientsToNotify)
            {
                ServerReadyMessage serverReadyMessage = new ServerReadyMessage();

                if (!BasisSavedState.GetLastAvatarChangeState(client, out var ChangeState)) ChangeState = new ClientAvatarChangeMessage();
                if (!BasisSavedState.GetLastAvatarSyncState(client, out var SyncState)) SyncState = new LocalAvatarSyncMessage() { array = new byte[386] };
                if (!BasisSavedState.GetLastPlayerMetaData(client, out var MetaData)) MetaData = new PlayerMetaDataMessage() { playerDisplayName = "Error", playerUUID = string.Empty };

                serverReadyMessage.localReadyMessage = new ReadyMessage
                {
                    localAvatarSyncMessage = SyncState,
                    clientAvatarChangeMessage = ChangeState,
                    playerMetaDataMessage = MetaData,
                };
                serverReadyMessage.playerIdMessage = new PlayerIdMessage() { playerID = (ushort)client.Id };
                copied.Add(serverReadyMessage);
            }

            CreateAllRemoteMessage remoteMessages = new CreateAllRemoteMessage
            {
                serverSidePlayer = copied.ToArray(),
            };
            NetDataWriter Writer = NetDataWriterPool.GetWriter();
            remoteMessages.Serialize(Writer);
            BNL.Log($"Sending list of clients to {authClient.Id}");
            authClient.Send(Writer, BasisNetworkCommons.CreateRemotePlayers, DeliveryMethod.ReliableOrdered);
            NetDataWriterPool.ReturnWriter(Writer);
        }
        #endregion
    }
}
