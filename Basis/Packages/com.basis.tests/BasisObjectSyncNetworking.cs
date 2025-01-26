using Basis.Scripts.BasisSdk;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Networking;
using BasisSerializer.OdinSerializer;
using LiteNetLib;
using UnityEngine;
public class BasisObjectSyncNetworking : MonoBehaviour
{
    public ushort MessageIndex = 0;
    public bool HasMessageIndexAssigned;
    public string NetworkId;
    public int TargetFrequency = 10; // Target update frequency in Hz (10 times per second)
    private double _updateInterval; // Time interval between updates
    private double _lastUpdateTime; // Last update timestamp
    public ushort CurrentOwner;
    public bool IsLocalOwner = false;
    public bool HasActiveOwnership = false;

    public BasisPositionRotationScale StoredData = new BasisPositionRotationScale();
    public float LerpMultiplier = 3f;
    public Rigidbody Rigidbody;
    public BasisContentBase ContentConnector;
    public InteractableObject[] InteractableObjects;
    public DeliveryMethod DeliveryMethod = DeliveryMethod.Sequenced;
    public DataFormat DataFormat = DataFormat.Binary;
    public void Awake()
    {
        if (ContentConnector == null && TryGetComponent<BasisContentBase>(out ContentConnector))
        {
        }
        if (Rigidbody == null && TryGetComponent<Rigidbody>(out Rigidbody))
        {
        }
        if (ContentConnector != null)
        {
            ContentConnector.OnNetworkIDSet += OnNetworkIDSet;
        }
        InteractableObjects = this.transform.GetComponentsInChildren<InteractableObject>();
        foreach (InteractableObject obj in InteractableObjects)
        {
            obj.OnInteractStartEvent += OnInteractStartEvent;
            obj.OnInteractEndEvent += OnInteractEndEvent;
        }
    }
    private void OnInteractEndEvent(BasisInput input)
    {
        BasisNetworkManagement.RemoveOwnership(NetworkId);
        BasisObjectSyncSystem.StartApplyRemoteData(this);
    }
    private void OnInteractStartEvent(BasisInput input)
    {
        BasisNetworkManagement.TakeOwnership(NetworkId, (ushort)BasisNetworkManagement.LocalPlayerPeer.RemoteId);
        BasisObjectSyncSystem.StopApplyRemoteData(this);
    }

    private void OnNetworkIDSet(string NetworkID)
    {
        NetworkId = NetworkID;
        BasisNetworkNetIDConversion.RequestId(NetworkId);
    }

    public void OnEnable()
    {
        HasMessageIndexAssigned = false;
        BasisScene.OnNetworkMessageReceived += OnNetworkMessageReceived;
        BasisNetworkManagement.OnOwnershipTransfer += OnOwnershipTransfer;
        BasisNetworkManagement.OwnershipReleased += OwnershipReleased;
        BasisNetworkNetIDConversion.OnNetworkIdAdded += OnNetworkIdAdded;
        _updateInterval = 1f / TargetFrequency; // Calculate interval (1/33 seconds)
        _lastUpdateTime = Time.timeAsDouble;

        StartRemoteControl();
        BasisObjectSyncSystem.StartApplyRemoteData(this);
    }
    public void OnDisable()
    {
        HasMessageIndexAssigned = false;
        BasisScene.OnNetworkMessageReceived -= OnNetworkMessageReceived;
        BasisNetworkManagement.OnOwnershipTransfer -= OnOwnershipTransfer;
        BasisNetworkManagement.OwnershipReleased -= OwnershipReleased;
        BasisNetworkNetIDConversion.OnNetworkIdAdded -= OnNetworkIdAdded;
    }

    private void OwnershipReleased(string UniqueEntityID)
    {
        if (NetworkId == UniqueEntityID)
        {
            IsLocalOwner = false;
            CurrentOwner = 0;
            HasActiveOwnership = false;
            //drop any interactable objects on this transform.
            StartRemoteControl();
            BasisObjectSyncSystem.StartApplyRemoteData(this);
        }
    }
    private void OnOwnershipTransfer(string UniqueEntityID, ushort NetIdNewOwner, bool IsOwner)
    {
        if (NetworkId == UniqueEntityID)
        {
            IsLocalOwner = IsOwner;
            CurrentOwner = NetIdNewOwner;
            HasActiveOwnership = true;
            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = !IsLocalOwner;
            }
            if (IsLocalOwner == false)
            {
                StartRemoteControl();
                BasisObjectSyncSystem.StartApplyRemoteData(this);
            }
            else
            {
                StopRemoteControl();
                BasisObjectSyncSystem.StopApplyRemoteData(this);
            }
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
    public void LateUpdate()
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
        BasisScene.OnNetworkMessageSend?.Invoke(MessageIndex, SerializationUtility.SerializeValue(StoredData, DataFormat),DeliveryMethod);
    }
    public void OnNetworkMessageReceived(ushort PlayerID, ushort messageIndex, byte[] buffer, DeliveryMethod DeliveryMethod)
    {
        if (HasMessageIndexAssigned && messageIndex == MessageIndex)
        {
            StoredData = SerializationUtility.DeserializeValue<BasisPositionRotationScale>(buffer, DataFormat);
        }
    }
    public void StartRemoteControl()
    {
        foreach (InteractableObject obj in InteractableObjects)
        {
            if (obj != null)
            {
                obj.StartRemoteControl();
            }
        }
    }
    public void StopRemoteControl()
    {
        foreach (InteractableObject obj in InteractableObjects)
        {
            if (obj != null)
            {
                obj.StartRemoteControl();
            }
        }
    }
}
