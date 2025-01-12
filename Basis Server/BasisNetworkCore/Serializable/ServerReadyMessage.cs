using LiteNetLib.Utils;
public static partial class SerializableBasis
{
    public struct ServerReadyMessage
    {
        public PlayerIdMessage playerIdMessage;
        public ReadyMessage localReadyMessage;
        public void Deserialize(NetDataReader Writer, bool AttemptAdditionalData)
        {
            playerIdMessage.Deserialize(Writer);
             localReadyMessage.Deserialize(Writer, AttemptAdditionalData);
        }
        public void Serialize(NetDataWriter Writer, bool AttemptAdditionalData)
        {
            playerIdMessage.Serialize(Writer);
            localReadyMessage.Serialize(Writer, AttemptAdditionalData);
        }
    }
}
