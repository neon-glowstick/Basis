using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.NetworkedAvatar;
using LiteNetLib;
using UnityEngine;

public class BasisObjectSyncNetworking : MonoBehaviour
{
    public ushort MessageIndex = 3321;
    public BasisPositionRotationScale Storeddata = new BasisPositionRotationScale();
    public float LerpMultiplier = 3f;
    public Rigidbody Rigidbody;
    private void Awake()
    {
        if (BasisObjectSyncSystem.Instance != null)
        {
            BasisObjectSyncSystem.Instance.RegisterObject(this);
        }
        BasisScene.OnNetworkMessageReceived += OnNetworkMessageReceived;
        BasisNetworkManagement.OnLocalPlayerJoined += OnLocalPlayerJoined;
        BasisNetworkManagement.OnRemotePlayerJoined += OnRemotePlayerJoined;
        BasisNetworkManagement.OnLocalPlayerLeft += OnLocalPlayerLeft;
        BasisNetworkManagement.OnRemotePlayerLeft += OnRemotePlayerLeft;
    }

    private void OnLocalPlayerLeft(BasisNetworkPlayer player1, BasisLocalPlayer player2)
    {
        ComputeCurrentOwner();
    }

    private void OnRemotePlayerLeft(BasisNetworkPlayer player1, BasisRemotePlayer player2)
    {
        ComputeCurrentOwner();
    }

    private void OnRemotePlayerJoined(BasisNetworkPlayer player1, BasisRemotePlayer player2)
    {
        ComputeCurrentOwner();
    }

    private void OnLocalPlayerJoined(BasisNetworkPlayer player1, BasisLocalPlayer player2)
    {
        ComputeCurrentOwner();
    }
    public void OnNetworkMessageReceived(ushort PlayerID, ushort MessageIndex, byte[] buffer, DeliveryMethod DeliveryMethod = DeliveryMethod.ReliableSequenced)
    {
        ComputeCurrentOwner();
    }
    public void ComputeCurrentOwner()
    {
        ushort OldestPlayerInInstance = BasisNetworkManagement.Instance.GetOldestAvailablePlayerUshort();
        bool IsOwner = OldestPlayerInInstance == BasisNetworkManagement.Instance.Transmitter.NetId;
        Rigidbody.isKinematic = !IsOwner;
    }
    private void OnDestroy()
    {
        if (BasisObjectSyncSystem.Instance != null)
        {
            BasisObjectSyncSystem.Instance.UnregisterObject(this);
        }
        BasisScene.OnNetworkMessageReceived -= OnNetworkMessageReceived;
        BasisNetworkManagement.OnLocalPlayerJoined -= OnLocalPlayerJoined;
        BasisNetworkManagement.OnRemotePlayerJoined -= OnRemotePlayerJoined;
        BasisNetworkManagement.OnLocalPlayerLeft -= OnLocalPlayerLeft;
        BasisNetworkManagement.OnRemotePlayerLeft -= OnRemotePlayerLeft;
    }

    public void UpdateStoredData(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Storeddata.Position = position;
        Storeddata.Rotation = rotation;
        Storeddata.Scale = scale;
    }
}
