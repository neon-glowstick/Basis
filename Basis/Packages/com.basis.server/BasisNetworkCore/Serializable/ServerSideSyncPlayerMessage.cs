using LiteNetLib.Utils;
public static partial class SerializableBasis
{
    public struct ServerSideSyncPlayerMessage
    {
        public PlayerIdMessage playerIdMessage;
        public byte interval;
        public LocalAvatarSyncMessage avatarSerialization;
        public void Deserialize(NetDataReader Writer,bool AttemptAdditionalData)
        {
            playerIdMessage.Deserialize(Writer);//2bytes
            Writer.Get(out interval);//1 bytes
            avatarSerialization.Deserialize(Writer, AttemptAdditionalData);
        }
        public void Serialize(NetDataWriter Writer, bool AttemptAdditionalData)
        {
            playerIdMessage.Serialize(Writer);
            Writer.Put(interval);
            avatarSerialization.Serialize(Writer, AttemptAdditionalData);
        }
    }
}
