using Basis.Network.Core;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using static SerializableBasis;

public static class BasisNetworkSpawnItem
{
    public static async Task SpawnScene(LocalLoadResource localLoadResource)
    {
        // Deserialize the incoming byte array
        byte[] load = localLoadResource.LoadInformation;
        BasisLoadableBundle loadBundle = SerializationHelper.FromByteArray(load);

        // Simulate scene loading
        await BasisSceneLoadDriver.LoadSceneAssetBundle(BundledContentHolder.Instance.DefaultScene);
    }
    public static void RequestSceneLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal)
    {
        BasisRemoteEncyptedBundle BasisRemoteEncyptedBundle = new BasisRemoteEncyptedBundle
        {
            MetaURL = UnlockPassword,
            BundleURL = BundleURL,
            IsLocal = IsLocal
        };

        // Serialize back to a byte array if needed
        byte[] updatedLoad = SerializationHelper.ToByteArray(UnlockPassword, BasisRemoteEncyptedBundle);

        LocalLoadResource LocalLoadResource = new LocalLoadResource
        {
            LoadInformation = updatedLoad,
            LoadedNetID = Guid.NewGuid().ToString()
        };
        LiteNetLib.Utils.NetDataWriter Writer = new LiteNetLib.Utils.NetDataWriter();
        LocalLoadResource.Serialize(Writer);
        BasisNetworkManagement.LocalPlayerPeer.Send(Writer,BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }
    public static void DestroyScene()
    {

    }
}
public static class SerializationHelper
{
    public static byte[] ToByteArray(string UnlockPassword, BasisRemoteEncyptedBundle BasisRemoteBundleEncrypted)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, UnlockPassword);
            formatter.Serialize(ms, BasisRemoteBundleEncrypted);
            return ms.ToArray();
        }
    }

    public static BasisLoadableBundle FromByteArray(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (BasisLoadableBundle)formatter.Deserialize(ms);
        }
    }
}
