using Basis.Network.Core;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking;
using System;
using System.Threading.Tasks;
using static SerializableBasis;

public static class BasisNetworkSpawnItem
{
    public static async Task SpawnScene(LocalLoadResource localLoadResource)
    {
        BasisLoadableBundle loadBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle() { BundleURL = localLoadResource.BundleURL,IsLocal = localLoadResource.IsLocalLoad, MetaURL = localLoadResource.MetaURL  },
             UnlockPassword = localLoadResource.UnlockPassword,
             
        };
        // Simulate scene loading
        await BasisSceneLoadDriver.LoadSceneAssetBundle(loadBundle);
    }
    public static void RequestSceneLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal)
    {
        LocalLoadResource LocalLoadResource = new LocalLoadResource
        {
            LoadedNetID = Guid.NewGuid().ToString(),
             Mode = 1,
              BundleURL = BundleURL,
               UnlockPassword = UnlockPassword,
                MetaURL = MetaURL,
                 IsLocalLoad = IsLocal
        };
        LiteNetLib.Utils.NetDataWriter Writer = new LiteNetLib.Utils.NetDataWriter();
        LocalLoadResource.Serialize(Writer);
        BasisNetworkManagement.LocalPlayerPeer.Send(Writer,BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }
    public static void DestroyScene()
    {

    }
}
