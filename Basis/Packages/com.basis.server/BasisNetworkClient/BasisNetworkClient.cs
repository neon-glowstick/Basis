using Basis.Network.Core;
using BasisNetworkCore;
using LiteNetLib;
using LiteNetLib.Utils;
using static Basis.Network.Core.Serializable.SerializableBasis;
using static SerializableBasis;

public static class BasisNetworkClient
{
    public static NetManager client;
    public static EventBasedNetListener listener;
    private static NetPeer peer;
    private static bool IsInUse;
    public static AuthenticationMessage AuthenticationMessage = new AuthenticationMessage();
    /// <summary>
    /// inital data is typically the 
    /// </summary> 
    /// <param name="IP"></param>
    /// <param name="port"></param>
    /// <param name="ReadyMessage"></param>
    public static NetPeer StartClient(string IP, int port, ReadyMessage ReadyMessage,bool UseNativeSockets = false)
    {
        if (IsInUse == false)
        {
            listener = new EventBasedNetListener();
            client = new NetManager(listener)
            {
                AutoRecycle = false,
                UnconnectedMessagesEnabled = false,
                NatPunchEnabled = true,
                AllowPeerAddressChange = true,
                BroadcastReceiveEnabled = false,
                UseNativeSockets = UseNativeSockets,//unity does not work with this
                ChannelsCount = BasisNetworkCommons.TotalChannels,
                EnableStatistics = true,
                UpdateTime = BasisNetworkCommons.NetworkIntervalPoll,
                PingInterval = 1500,
                UnsyncedEvents = true,
            };
            client.Start();
            NetDataWriter Writer = new NetDataWriter(true,12);
            //this is the only time we dont put key!
            Writer.Put(BasisNetworkVersion.ServerVersion);
            AuthenticationMessage.Serialize(Writer);
            ReadyMessage.Serialize(Writer);
            peer = client.Connect(IP, port, Writer);
            return peer;
        }
        else
        {
            BNL.LogError("Call Shutdown First!");
            return null;
        }
    }
    public static void Disconnect()
    {
        IsInUse = false;
        BNL.Log("Client Called Disconnect from server");
        peer?.Disconnect();
        client?.Stop();

        BNL.Log("Worker thread stopped.");
    }
}
