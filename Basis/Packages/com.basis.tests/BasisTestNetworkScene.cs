using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.NetworkedAvatar;
using LiteNetLib;
using UnityEngine;
using static SerializableBasis;

public class BasisTestNetworkScene : MonoBehaviour
{
    public byte[] SendingData;
    public ushort[] Recipients;
    public ushort MessageIndex;
    public bool SceneLoadTest = false;
    public bool GameobjectLoadTest = false;
    public LocalLoadResource Scene;
    public LocalLoadResource Gameobject;
    public bool IsPersistent;
    public void Awake()
    {
        BasisNetworkManagement.OnLocalPlayerJoined += OnLocalPlayerJoined;
        BasisNetworkManagement.OnRemotePlayerJoined += OnRemotePlayerJoined;
    }
    public void OnEnable()
    {
        if (SceneLoadTest)
        {
            BasisNetworkSpawnItem.RequestSceneLoad("Scene",
               "https://BasisFramework.b-cdn.net/Worlds/DX11/3dd6aa45-a685-4ed2-ba6d-2d9c2f3c1765_638652274774362697.BasisEncyptedBundle",
               "https://BasisFramework.b-cdn.net/Worlds/DX11/3dd6aa45-a685-4ed2-ba6d-2d9c2f3c1765_638652274774362697.BasisEncyptedMeta",
               false, IsPersistent, out Scene);
        }
        if (GameobjectLoadTest)
        {
            BasisNetworkSpawnItem.RequestGameObjectLoad("Aurellia",
                 "https://BasisFramework.b-cdn.net/Avatars/DX11/ThirdParty/84df873f-4857-47da-88ea-c7b604793489_638661962010243564.BasisEncyptedBundle",
                 "https://BasisFramework.b-cdn.net/Avatars/DX11/ThirdParty/84df873f-4857-47da-88ea-c7b604793489_638661962010243564.BasisEncyptedMeta",
                 false, BasisLocalPlayer.Instance.transform.position, Quaternion.identity, Vector3.one, IsPersistent, out Gameobject);
        }
    }
    public void OnDisable()
    {
        if (SceneLoadTest)
        {
            BasisNetworkSpawnItem.RequestSceneUnLoad(Scene.LoadedNetID);
        }
        if (GameobjectLoadTest)
        {
            BasisNetworkSpawnItem.RequestGameObjectUnLoad(Gameobject.LoadedNetID);
        }
    }
    /// <summary>
    /// this runs after a remote user connects and passes all there local checks and balances with the server
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    private void OnRemotePlayerJoined(BasisNetworkPlayer player1, BasisRemotePlayer player2)
    {

    }
    /// <summary>
    /// this is called once
    /// level is loaded
    /// network is connected
    /// player is created
    /// player is authenticated
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    public void OnLocalPlayerJoined(BasisNetworkPlayer player1, BasisLocalPlayer player2)
    {
        BasisScene.OnNetworkMessageReceived += OnNetworkMessageReceived;
        BasisScene.OnNetworkMessageSend(MessageIndex, SendingData, DeliveryMethod.ReliableOrdered, Recipients);
    }
    private void OnNetworkMessageReceived(ushort PlayerID, ushort MessageIndex, byte[] buffer, DeliveryMethod Method = DeliveryMethod.ReliableOrdered)
    {

    }
}
