using Basis.Network.Core;
using BasisNetworkCore;
using LiteNetLib.Utils;
using System.Collections.Concurrent;
using System.Linq;
using static SerializableBasis;

public static class BasisNetworkResourceManagement
{
    public static ConcurrentDictionary<string, LocalLoadResource> UshortNetworkDatabase = new ConcurrentDictionary<string, LocalLoadResource>();
    public static void Reset()
    {
        LocalLoadResource[] Resource = UshortNetworkDatabase.Values.ToArray();
        int length = Resource.Length;
        for (int Index = 0; Index < length; Index++)
        {
            LocalLoadResource LLR = Resource[Index];
            if (LLR.Persist == false)
            {
                UnLoadResource UnloadResource = new UnLoadResource
                {
                    Mode = LLR.Mode,
                    LoadedNetID = LLR.LoadedNetID
                };
                NetDataWriter Writer = new NetDataWriter(true);
                UnloadResource.Serialize(Writer);
                BasisNetworkServer.BroadcastMessageToClients(Writer, BasisNetworkCommons.LoadResourceMessage, BasisPlayerArray.GetSnapshot(), LiteNetLib.DeliveryMethod.ReliableSequenced);
            }
        }
        UshortNetworkDatabase.Clear();
    }
    public static void SendOutAllResources(LiteNetLib.NetPeer NewConnection)
    {
        LocalLoadResource[] Resource = UshortNetworkDatabase.Values.ToArray();
        int length = Resource.Length;
        for (int Index = 0; Index < length; Index++)
        {
            LocalLoadResource LLR = Resource[Index];
            NetDataWriter Writer = new NetDataWriter(true);
            LLR.Serialize(Writer);
            NewConnection.Send(Writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableSequenced);
        }
    }
    public static void LoadResource(LocalLoadResource LocalLoadResource)
    {
        if (UshortNetworkDatabase.ContainsKey(LocalLoadResource.LoadedNetID) == false)
        {
            NetDataWriter Writer = new NetDataWriter(true);
            LocalLoadResource.Serialize(Writer);
            if (UshortNetworkDatabase.TryAdd(LocalLoadResource.LoadedNetID, LocalLoadResource))
            {
                BNL.Log("Adding Object " + LocalLoadResource.LoadedNetID);

                BasisNetworkServer.BroadcastMessageToClients(Writer, BasisNetworkCommons.LoadResourceMessage, BasisPlayerArray.GetSnapshot(), LiteNetLib.DeliveryMethod.ReliableSequenced);
            }
            else
            {
                BNL.LogError("Try Add Failed Already have Object Loaded With " + LocalLoadResource.LoadedNetID);
            }
        }
        else
        {
            BNL.LogError("Already have Object Loaded With " + LocalLoadResource.LoadedNetID);
        }
    }
    public static void UnloadResource(UnLoadResource UnLoadResource)
    {
        if (UshortNetworkDatabase.TryRemove(UnLoadResource.LoadedNetID,out LocalLoadResource Resource))
        {
            NetDataWriter Writer = new NetDataWriter(true);
            UnLoadResource.Serialize(Writer);
            BNL.Log("Removing Object " + UnLoadResource.LoadedNetID);
            BasisNetworkServer.BroadcastMessageToClients(Writer, BasisNetworkCommons.UnloadResourceMessage, BasisPlayerArray.GetSnapshot(), LiteNetLib.DeliveryMethod.ReliableSequenced);
        }
        else
        {
            BNL.LogError($"Trying to unload a object that does not exist! ID Proved was [{UnLoadResource.LoadedNetID}]");
        }
    }
}
