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
    public static void RequestSceneLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal)
    {
        BasisDebug.Log("Requesting scene load...", BasisDebug.LogTag.Networking);

        LocalLoadResource localLoadResource = new LocalLoadResource
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

        BasisNetworkManagement.LocalPlayerPeer.Send(writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    public static void RequestGameObjectLoad(string UnlockPassword, string BundleURL, string MetaURL, bool IsLocal, UnityEngine.Vector3 Position, UnityEngine.Quaternion Rotation, UnityEngine.Vector3 Scale)
    {
        BasisDebug.Log("Requesting GameObject load...", BasisDebug.LogTag.Networking);

        LocalLoadResource localLoadResource = new LocalLoadResource
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
        localLoadResource.Serialize(writer);

        BasisDebug.Log($"Sending GameObject load request with NetID: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);

        BasisNetworkManagement.LocalPlayerPeer.Send(writer, BasisNetworkCommons.LoadResourceMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
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

        BasisDebug.Log($"Scene spawned successfully: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);
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

        BasisProgressReport progressCallback = new BasisProgressReport();
        CancellationToken cancellationToken = new CancellationToken();

        UnityEngine.Vector3 position = new UnityEngine.Vector3(localLoadResource.PositionX, localLoadResource.PositionY, localLoadResource.PositionZ);
        UnityEngine.Quaternion rotation = new UnityEngine.Quaternion(localLoadResource.QuaternionX, localLoadResource.QuaternionY, localLoadResource.QuaternionZ, localLoadResource.QuaternionW);
        UnityEngine.Vector3 scale = new UnityEngine.Vector3(localLoadResource.ScaleX, localLoadResource.ScaleY, localLoadResource.ScaleZ);

        GameObject reference = await BasisLoadHandler.LoadGameObjectBundle(loadBundle, true, progressCallback, cancellationToken, position, rotation, scale, true, BasisNetworkManagement.Instance.transform);

        SpawnedGameobjects.TryAdd(localLoadResource.LoadedNetID, reference);

        BasisDebug.Log($"GameObject spawned successfully: {localLoadResource.LoadedNetID}", BasisDebug.LogTag.Networking);
        return reference;
    }

    public static ConcurrentDictionary<string, GameObject> SpawnedGameobjects = new ConcurrentDictionary<string, GameObject>();
    public static ConcurrentDictionary<string, Scene> SpawnedScenes = new ConcurrentDictionary<string, Scene>();

    public static async void DestroyScene(UnLoadResource resource)
    {
        BasisDebug.Log($"Destroying scene with NetID: {resource.LoadedNetID}", BasisDebug.LogTag.Networking);

        if (SpawnedScenes.TryRemove(resource.LoadedNetID, out Scene value))
        {
            await SceneManager.UnloadSceneAsync(value);
            BasisDebug.Log($"Scene destroyed: {resource.LoadedNetID}", BasisDebug.LogTag.Networking);
        }
        else
        {
            BasisDebug.Log($"Failed to destroy scene: {resource.LoadedNetID} (not found)", BasisDebug.LogTag.Networking);
        }
    }

    public static void DestroyGameobject(UnLoadResource resource)
    {
        BasisDebug.Log($"Destroying GameObject with NetID: {resource.LoadedNetID}", BasisDebug.LogTag.Networking);

        if (SpawnedGameobjects.TryRemove(resource.LoadedNetID, out GameObject value))
        {
            if (value != null)
            {
                GameObject.Destroy(value);
                BasisDebug.Log($"GameObject destroyed: {resource.LoadedNetID}", BasisDebug.LogTag.Networking);
            }
        }
        else
        {
            BasisDebug.Log($"Failed to destroy GameObject: {resource.LoadedNetID} (not found)", BasisDebug.LogTag.Networking);
        }
    }
}
