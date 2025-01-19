using Basis.Network.Core;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SerializableBasis;

public static class BasisNetworkSpawnItem
{
    public static async Task SpawnScene(LocalLoadResource localLoadResource)
    {
        BasisLoadableBundle loadBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle() { BundleURL = localLoadResource.BundleURL, IsLocal = localLoadResource.IsLocalLoad, MetaURL = localLoadResource.MetaURL },
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
        BasisNetworkManagement.LocalPlayerPeer.Send(Writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }
    public static void RequestGameObjectLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal, UnityEngine.Vector3 Position, UnityEngine.Quaternion Rotation, UnityEngine.Vector3 Scale)
    {
        LocalLoadResource LocalLoadResource = new LocalLoadResource
        {
            LoadedNetID = Guid.NewGuid().ToString(),
            Mode = 0,
            BundleURL = BundleURL,
            UnlockPassword = UnlockPassword,
            MetaURL = MetaURL,
            IsLocalLoad = IsLocal,
             PositionX = Position.x,
             PositionY = Position.y,
             PositionZ = Position.z,
             QuaternionW = Rotation.w,
             QuaternionX = Rotation.x,
             QuaternionY = Rotation.y,
             QuaternionZ = Rotation.z,
             ScaleX = Scale.x,
             ScaleY = Scale.y,
             ScaleZ = Scale.z,
        };
        LiteNetLib.Utils.NetDataWriter Writer = new LiteNetLib.Utils.NetDataWriter();
        LocalLoadResource.Serialize(Writer);
        BasisNetworkManagement.LocalPlayerPeer.Send(Writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }
    public static async Task SpawnGameObject(LocalLoadResource localLoadResource)
    {
        BasisLoadableBundle loadBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle() { BundleURL = localLoadResource.BundleURL, IsLocal = localLoadResource.IsLocalLoad, MetaURL = localLoadResource.MetaURL },
            UnlockPassword = localLoadResource.UnlockPassword,

        };
        BasisProgressReport progressCallback = new BasisProgressReport();
        CancellationToken CancellationToken = new CancellationToken();
        UnityEngine.Vector3 Position = new UnityEngine.Vector3(localLoadResource.PositionX, localLoadResource.PositionY, localLoadResource.PositionZ);
        UnityEngine.Quaternion Rotation = new UnityEngine.Quaternion(localLoadResource.QuaternionX, localLoadResource.QuaternionY, localLoadResource.QuaternionZ, localLoadResource.QuaternionW);

        UnityEngine.Vector3 Scale = new UnityEngine.Vector3(localLoadResource.ScaleX, localLoadResource.ScaleY, localLoadResource.ScaleZ);
        await BasisLoadHandler.LoadGameObjectBundle(loadBundle, true, progressCallback, CancellationToken, Position, Rotation, Scale, true, BasisNetworkManagement.Instance.transform);
    }
    public static void DestroyScene()
    {

    }
    public static void DestroyGameobject()
    {

    }
}
