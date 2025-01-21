using Basis.Network.Core;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SerializableBasis;

public static class BasisNetworkSpawnItem
{
    public static bool RequestSceneLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal,out LocalLoadResource localLoadResource)
    {
        if (string.IsNullOrEmpty(BundleURL) || string.IsNullOrEmpty(MetaURL) || string.IsNullOrEmpty(UnlockPassword))
        {
            BasisDebug.Log("Invalid parameters for scene load request.", BasisDebug.LogTag.Networking);
            localLoadResource = new LocalLoadResource();
            return false;
        }

        BasisDebug.Log("Requesting scene load...", BasisDebug.LogTag.Networking);

        localLoadResource = new LocalLoadResource
        {
            LoadedNetID = Guid.NewGuid().ToString(),
            Mode = 1,
            BundleURL = BundleURL,
            UnlockPassword = UnlockPassword,
            MetaURL = MetaURL,
            IsLocalLoad = IsLocal
        };

        LiteNetLib.Utils.NetDataWriter writer = new LiteNetLib.Utils.NetDataWriter();
        localLoadResource.Serialize(writer);

        BasisDebug.Log($"Sending scene load request with NetID: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisNetworkManagement.LocalPlayerPeer?.Send(writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
        return true;
    }

    public static bool RequestGameObjectLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal, Vector3 Position, Quaternion Rotation, Vector3 Scale, out LocalLoadResource LocalLoadResource)
    {
        if (string.IsNullOrEmpty(BundleURL) || string.IsNullOrEmpty(MetaURL) || string.IsNullOrEmpty(UnlockPassword))
        {
            BasisDebug.Log("Invalid parameters for GameObject load request.", BasisDebug.LogTag.Networking);
            LocalLoadResource = new LocalLoadResource();
            return false;
        }

        BasisDebug.Log("Requesting GameObject load...", BasisDebug.LogTag.Networking);

        LocalLoadResource = new LocalLoadResource
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

        LiteNetLib.Utils.NetDataWriter writer = new LiteNetLib.Utils.NetDataWriter();
        LocalLoadResource.Serialize(writer);

        BasisDebug.Log($"Sending GameObject load request with NetID: {LocalLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisNetworkManagement.LocalPlayerPeer?.Send(writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
        return true;
    }

    public static void RequestGameObjectUnLoad(string LoadedNetID)
    {
        if (string.IsNullOrEmpty(LoadedNetID))
        {
            BasisDebug.Log("Invalid LoadedNetID for GameObject unload.", BasisDebug.LogTag.Networking);
            return;
        }

        UnLoadResource localLoadResource = new UnLoadResource { LoadedNetID = LoadedNetID, Mode = 0 };
        RequestUnload(localLoadResource);
    }

    public static void RequestSceneUnLoad(string LoadedNetID)
    {
        if (string.IsNullOrEmpty(LoadedNetID))
        {
            BasisDebug.Log("Invalid LoadedNetID for scene unload.", BasisDebug.LogTag.Networking);
            return;
        }

        BasisDebug.Log("Requesting scene unload...", BasisDebug.LogTag.Networking);

        UnLoadResource localLoadResource = new UnLoadResource { LoadedNetID = LoadedNetID, Mode = 1 };
        RequestUnload(localLoadResource);
    }

    public static void RequestUnload(UnLoadResource UnLoadResource)
    {
        if (string.IsNullOrEmpty(UnLoadResource.LoadedNetID))
        {
            BasisDebug.Log("Invalid unload request.", BasisDebug.LogTag.Networking);
            return;
        }

        LiteNetLib.Utils.NetDataWriter writer = new LiteNetLib.Utils.NetDataWriter();
        UnLoadResource.Serialize(writer);

        BasisDebug.Log($"Sending unload request with NetID: {UnLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisNetworkManagement.LocalPlayerPeer?.Send(writer, BasisNetworkCommons.UnloadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    public static async Task<Scene> SpawnScene(LocalLoadResource localLoadResource)
    {
        BasisDebug.Log($"Spawning scene with NetID: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisLoadableBundle loadBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle()
            {
                BundleURL = localLoadResource.BundleURL,
                IsLocal = localLoadResource.IsLocalLoad,
                MetaURL = localLoadResource.MetaURL
            },
            UnlockPassword = localLoadResource.UnlockPassword,
        };

        Scene scene = await BasisSceneLoadDriver.LoadSceneAssetBundle(loadBundle);
        SpawnedScenes.TryAdd(localLoadResource.LoadedNetID, scene);

        return scene;
    }

    public static async Task<GameObject> SpawnGameObject(LocalLoadResource localLoadResource)
    {
        BasisDebug.Log($"Spawning GameObject with NetID: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisLoadableBundle loadBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle()
            {
                BundleURL = localLoadResource.BundleURL,
                IsLocal = localLoadResource.IsLocalLoad,
                MetaURL = localLoadResource.MetaURL
            },
            UnlockPassword = localLoadResource.UnlockPassword,
        };

        GameObject reference = await BasisLoadHandler.LoadGameObjectBundle(loadBundle, true, new BasisProgressReport(), new CancellationToken(),
            new Vector3(localLoadResource.PositionX, localLoadResource.PositionY, localLoadResource.PositionZ),
            new Quaternion(localLoadResource.QuaternionX, localLoadResource.QuaternionY, localLoadResource.QuaternionZ, localLoadResource.QuaternionW),
            new Vector3(localLoadResource.ScaleX, localLoadResource.ScaleY, localLoadResource.ScaleZ),
            true, BasisNetworkManagement.Instance.transform);

        SpawnedGameobjects.TryAdd(localLoadResource.LoadedNetID, reference);
        return reference;
    }

    public static void DestroyScene(UnLoadResource resource)
    {
        if (string.IsNullOrEmpty(resource.LoadedNetID))
        {
            BasisDebug.Log("Invalid resource for destroying scene.", BasisDebug.LogTag.Networking);
            return;
        }

        if (SpawnedScenes.TryRemove(resource.LoadedNetID, out Scene value))
        {
            SceneManager.UnloadSceneAsync(value);
        }
    }

    public static void DestroyGameobject(UnLoadResource resource)
    {
        if (string.IsNullOrEmpty(resource.LoadedNetID))
        {
            BasisDebug.Log("Invalid resource for destroying GameObject.", BasisDebug.LogTag.Networking);
            return;
        }

        if (SpawnedGameobjects.TryRemove(resource.LoadedNetID, out GameObject value))
        {
            if (value != null)
                GameObject.Destroy(value);
        }
    }

    public static void Reset()
    {
        SpawnedGameobjects.Clear();
        SpawnedScenes.Clear();
        BasisDebug.Log("All spawned objects and scenes have been cleared.", BasisDebug.LogTag.Networking);
    }

    public static ConcurrentDictionary<string, GameObject> SpawnedGameobjects = new ConcurrentDictionary<string, GameObject>();
    public static ConcurrentDictionary<string, Scene> SpawnedScenes = new ConcurrentDictionary<string, Scene>();
}
