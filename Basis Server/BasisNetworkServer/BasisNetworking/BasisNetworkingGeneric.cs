using Basis.Network.Core;
using BasisNetworkCore;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static SerializableBasis;

namespace Basis.Network.Server.Generic
{
    public static class BasisNetworkingGeneric
    {
        public static void HandleScene(NetPacketReader Reader, DeliveryMethod DeliveryMethod, NetPeer sender)
        {
            SceneDataMessage SceneDataMessage = new SceneDataMessage();
            SceneDataMessage.Deserialize(Reader);
            Reader.Recycle();
            ServerSceneDataMessage serverSceneDataMessage = new ServerSceneDataMessage
            {
                sceneDataMessage = new RemoteSceneDataMessage()
                {
                    messageIndex = SceneDataMessage.messageIndex,
                    payload = SceneDataMessage.payload
                },
                playerIdMessage = new PlayerIdMessage
                {
                    playerID = (ushort)sender.Id,
                }
            };
            byte Channel = BasisNetworkCommons.SceneChannel;
            NetDataWriter Writer = NetDataWriterPool.GetWriter();
            if (DeliveryMethod == DeliveryMethod.Unreliable)
            {
                Writer.Put(Channel);
                Channel = BasisNetworkCommons.FallChannel;
            }
            serverSceneDataMessage.Serialize(Writer);
            if (SceneDataMessage.recipientsSize != 0)
            {
                List<NetPeer> targetedClients = new List<NetPeer>();

                int recipientsLength = SceneDataMessage.recipientsSize;
                //  BNL.Log("Query Recipients " + recipientsLength);
                for (int index = 0; index < recipientsLength; index++)
                {
                    if (BasisNetworkServer.Peers.TryGetValue(SceneDataMessage.recipients[index], out NetPeer client))
                    {
                        BNL.Log("Found Peer! " + SceneDataMessage.recipients[index]);
                        targetedClients.Add(client);
                    }
                    else
                    {
                        BNL.Log("Missing Peer! " + SceneDataMessage.recipients[index]);
                    }
                }

                if (targetedClients.Count > 0)
                {
                    //  BNL.Log("Sending out Target Clients " + targetedClients.Count);
                    BasisNetworkServer.BroadcastMessageToClients(Writer, Channel,ref targetedClients, DeliveryMethod);
                }
            }
            else
            {
                BasisNetworkServer.BroadcastMessageToClients(Writer, Channel, sender,BasisPlayerArray.GetSnapshot(), DeliveryMethod);
            }
            NetDataWriterPool.ReturnWriter(Writer);
        }
        public static void HandleAvatar(NetPacketReader Reader, DeliveryMethod DeliveryMethod, NetPeer sender)
        {
            AvatarDataMessage avatarDataMessage = new AvatarDataMessage();
            avatarDataMessage.Deserialize(Reader);
            Reader.Recycle();
            ServerAvatarDataMessage serverAvatarDataMessage = new ServerAvatarDataMessage
            {
                avatarDataMessage = new RemoteAvatarDataMessage()
                {
                    messageIndex = avatarDataMessage.messageIndex,
                    payload = avatarDataMessage.payload,
                    PlayerIdMessage = avatarDataMessage.PlayerIdMessage
                },
                playerIdMessage = new PlayerIdMessage
                {
                    playerID = (ushort)sender.Id
                }
            };
            byte Channel = BasisNetworkCommons.AvatarChannel;
            NetDataWriter Writer = NetDataWriterPool.GetWriter();
            if (DeliveryMethod == DeliveryMethod.Unreliable)
            {
                Writer.Put(Channel);
                Channel = BasisNetworkCommons.FallChannel;
            }
            serverAvatarDataMessage.Serialize(Writer);
            if (avatarDataMessage.recipientsSize != 0)
            {
                List<NetPeer> targetedClients = new List<NetPeer>();

                int recipientsLength = avatarDataMessage.recipientsSize;
                //  BNL.Log("Query Recipients " + recipientsLength);
                for (int index = 0; index < recipientsLength; index++)
                {
                    if (BasisNetworkServer.Peers.TryGetValue(avatarDataMessage.recipients[index], out NetPeer client))
                    {
                        //   BNL.Log("Found Peer! " + avatarDataMessage.recipients[index]);
                        targetedClients.Add(client);
                    }
                    else
                    {
                        BNL.LogError("Missing Peer! " + avatarDataMessage.recipients[index]);
                    }
                }

                if (targetedClients.Count > 0)
                {
                    //BNL.Log("Sending out Target Clients " + targetedClients.Count);
                    BasisNetworkServer.BroadcastMessageToClients(Writer, Channel,ref targetedClients, DeliveryMethod);
                }
            }
            else
            {
                BasisNetworkServer.BroadcastMessageToClients(Writer, Channel, sender, BasisPlayerArray.GetSnapshot(), DeliveryMethod);
            }
            NetDataWriterPool.ReturnWriter(Writer);
        }
    }
}
