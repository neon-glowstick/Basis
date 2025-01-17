using Basis.Network.Core;
using Basis.Scripts.Networking;
using LiteNetLib.Utils;
using System.Collections.Concurrent;
using static BasisNetworkCore.Serializable.SerializableBasis;

public static class BasisNetworkNetIDConversion
{
    // ConcurrentDictionary to store network IDs
    public static ConcurrentDictionary<string, ushort> NetworkIds = new ConcurrentDictionary<string, ushort>();

    // Delegate and event for when a new ID is added
    public delegate void NetworkIdAddedHandler(string uniqueId, ushort ushortId);
    public static event NetworkIdAddedHandler OnNetworkIdAdded;

    public static void RequestId(string NetworkId)
    {
        if (NetworkIds.TryGetValue(NetworkId, out ushort Value))
        {
            OnNetworkIdAdded?.Invoke(NetworkId, Value);
        }
        else
        {
            NetDataWriter netDataWriter = new NetDataWriter();

            NetIDMessage ServerUniqueIDMessage = new NetIDMessage();
            ServerUniqueIDMessage.UniqueID = NetworkId;
            ServerUniqueIDMessage.Serialize(netDataWriter);
            BasisNetworkManagement.LocalPlayerPeer.Send(netDataWriter, BasisNetworkCommons.netIDAssign, LiteNetLib.DeliveryMethod.ReliableSequenced);
            //request new one from server
        }
    }
    public static void AddNetworkId(ServerNetIDMessage ServerNetIDMessage)
    {
        // Attempt to add the new network ID
        if (NetworkIds.TryAdd(ServerNetIDMessage.NetIDMessage.UniqueID, ServerNetIDMessage.UshortUniqueIDMessage.UniqueIDUshort))
        {
            BasisDebug.Log($"Ids Updated: Added UniqueID '{ServerNetIDMessage.NetIDMessage.UniqueID}' with UshortUniqueID '{ServerNetIDMessage.UshortUniqueIDMessage.UniqueIDUshort}'", BasisDebug.LogTag.Networking);

            // Trigger the event if it has subscribers
            OnNetworkIdAdded?.Invoke(ServerNetIDMessage.NetIDMessage.UniqueID, ServerNetIDMessage.UshortUniqueIDMessage.UniqueIDUshort);
        }
        else
        {
            // Check if the existing ID has a different value
            if (NetworkIds.TryGetValue(ServerNetIDMessage.NetIDMessage.UniqueID, out ushort existingValue))
            {
                if (ServerNetIDMessage.UshortUniqueIDMessage.UniqueIDUshort != existingValue)
                {
                    BasisDebug.Log($"Conflict Detected: UniqueID '{ServerNetIDMessage.NetIDMessage.UniqueID}' already exists with a different UshortUniqueID '{existingValue}', new value is '{ServerNetIDMessage.UshortUniqueIDMessage.UniqueIDUshort}'", BasisDebug.LogTag.Networking);
                }
                else
                {
                    BasisDebug.Log($"Duplicate Entry: UniqueID '{ServerNetIDMessage.NetIDMessage.UniqueID}' with matching UshortUniqueID '{existingValue}' already exists. No changes made.", BasisDebug.LogTag.Networking);
                }
            }
            else
            {
                BasisDebug.Log($"Unexpected Error: Failed to retrieve UniqueID '{ServerNetIDMessage.NetIDMessage.UniqueID}' despite failing to add it.", BasisDebug.LogTag.Networking);
            }
        }
    }
}
