using LiteNetLib.Utils;
using static BasisNetworkCore.Serializable.SerializableBasis;
public static partial class SerializableBasis
{
    public struct ReadyMessage
    {
        public PlayerMetaDataMessage playerMetaDataMessage;
        public ClientAvatarChangeMessage clientAvatarChangeMessage;
        public LocalAvatarSyncMessage localAvatarSyncMessage;
        public void Deserialize(NetDataReader Writer, bool AttemptAdditionalData)
        {
            playerMetaDataMessage.Deserialize(Writer);
            clientAvatarChangeMessage.Deserialize(Writer);
            localAvatarSyncMessage.Deserialize(Writer, AttemptAdditionalData);
        }
        public void Serialize(NetDataWriter Writer, bool AttemptAdditionalData)
        {
            playerMetaDataMessage.Serialize(Writer);
            clientAvatarChangeMessage.Serialize(Writer);
            localAvatarSyncMessage.Serialize(Writer, AttemptAdditionalData);
        }
        public bool WasDeserializedCorrectly()
        {
            if(clientAvatarChangeMessage.byteArray == null)
            {
                return false;
            }
            if(localAvatarSyncMessage.array == null)
            {
                return false;
            }
            return true;
        }
    }
}
