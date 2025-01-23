using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.NetworkedAvatar;
using BasisSerializer.OdinSerializer;
using LiteNetLib;
using UnityEngine;
public class BasisObjectSyncNetworking : MonoBehaviour
{
    public ushort MessageIndex = 0;
    public bool HasMessageIndexAssigned;
    public string NetworkId;
    public BasisPositionRotationScale StoredData = new BasisPositionRotationScale();
    public float LerpMultiplier = 3f;
    //public Rigidbody Rigidbody;
    public int TargetFrequency = 10; // Target update frequency in Hz (10 times per second)
    private double _updateInterval; // Time interval between updates
    private double _lastUpdateTime; // Last update timestamp
    public ushort CurrentOwner;
    public bool IsLocalOwner = false;
    public bool HasActiveOwnership = false;
    public void OnEnable()
    {
        HasMessageIndexAssigned = false;
        BasisObjectSyncSystem.Instance?.RegisterObject(this);
        BasisScene.OnNetworkMessageReceived += OnNetworkMessageReceived;
        BasisNetworkManagement.OnLocalPlayerJoined += OnLocalPlayerJoined;
        BasisNetworkManagement.OnRemotePlayerJoined += OnRemotePlayerJoined;
        BasisNetworkManagement.OnLocalPlayerLeft += OnLocalPlayerLeft;
        BasisNetworkManagement.OnRemotePlayerLeft += OnRemotePlayerLeft;
        BasisNetworkManagement.OnOwnershipTransfer += OnOwnershipTransfer;
        BasisNetworkNetIDConversion.OnNetworkIdAdded += OnNetworkIdAdded;
        BasisNetworkNetIDConversion.RequestId(NetworkId);
        _updateInterval = 1f / TargetFrequency; // Calculate interval (1/33 seconds)
        _lastUpdateTime = Time.timeAsDouble;
    }
    public void OnDisable()
    {
        HasMessageIndexAssigned = false;
        BasisObjectSyncSystem.Instance?.UnregisterObject(this);
        BasisScene.OnNetworkMessageReceived -= OnNetworkMessageReceived;
        BasisNetworkManagement.OnLocalPlayerJoined -= OnLocalPlayerJoined;
        BasisNetworkManagement.OnRemotePlayerJoined -= OnRemotePlayerJoined;
        BasisNetworkManagement.OnLocalPlayerLeft -= OnLocalPlayerLeft;
        BasisNetworkManagement.OnRemotePlayerLeft -= OnRemotePlayerLeft;
        BasisNetworkManagement.OnOwnershipTransfer -= OnOwnershipTransfer;
        BasisNetworkNetIDConversion.OnNetworkIdAdded -= OnNetworkIdAdded;
    }
    private void OnOwnershipTransfer(string UniqueEntityID, ushort NetIdNewOwner, bool IsOwner)
    {
        if (NetworkId == UniqueEntityID)
        {
            IsLocalOwner = IsOwner;
            CurrentOwner = NetIdNewOwner;
            HasActiveOwnership = true;
            //Rigidbody.isKinematic = !IsLocalOwner;
        }
    }

    public void OnNetworkIdAdded(string uniqueId, ushort ushortId)
    {
        if (NetworkId == uniqueId)
        {
            MessageIndex = ushortId;
            HasMessageIndexAssigned = true;
            if (HasActiveOwnership == false)
            {
                BasisNetworkManagement.RequestCurrentOwnership(NetworkId);
            }
        }
    }
    public void OnLocalPlayerLeft(BasisNetworkPlayer player1, BasisLocalPlayer player2)
    {
    }
    public void OnRemotePlayerLeft(BasisNetworkPlayer player1, BasisRemotePlayer player2)
    {
    }
    public void OnRemotePlayerJoined(BasisNetworkPlayer player1, BasisRemotePlayer player2)
    {
    }
    public void OnLocalPlayerJoined(BasisNetworkPlayer player1, BasisLocalPlayer player2)
    {
    }
    public void Update()
    {
        if (IsLocalOwner && HasMessageIndexAssigned)
        {
            double DoubleTime = Time.timeAsDouble;
            if (DoubleTime - _lastUpdateTime >= _updateInterval)
            {
                _lastUpdateTime = DoubleTime;
                SendNetworkMessage();
            }
        }
    }
    public void SendNetworkMessage()
    {
        transform.GetLocalPositionAndRotation(out StoredData.Position, out StoredData.Rotation);
        StoredData.Scale = transform.localScale;
        BasisScene.OnNetworkMessageSend?.Invoke(MessageIndex, SerializationUtility.SerializeValue(StoredData, DataFormat.Binary), DeliveryMethod.Sequenced);
    }
    public void OnNetworkMessageReceived(ushort PlayerID, ushort messageIndex, byte[] buffer, DeliveryMethod DeliveryMethod)
    {
        if (HasMessageIndexAssigned && messageIndex == MessageIndex)
        {
            StoredData = SerializationUtility.DeserializeValue<BasisPositionRotationScale>(buffer, DataFormat.Binary);
            transform.SetLocalPositionAndRotation(StoredData.Position, StoredData.Rotation);
            transform.localScale = StoredData.Scale;
        }
    }
}
