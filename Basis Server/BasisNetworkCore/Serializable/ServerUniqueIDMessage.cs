using LiteNetLib.Utils;
namespace BasisNetworkCore.Serializable
{
    public static partial class SerializableBasis
    {
        public struct ServerNetIDMessage
        {
            public NetIDMessage NetIDMessage;
            public UshortUniqueIDMessage UshortUniqueIDMessage;
            public void Deserialize(NetDataReader reader)
            {
                NetIDMessage.Deserialize(reader);
                UshortUniqueIDMessage.Deserialize(reader);
            }

            public void Serialize(NetDataWriter writer)
            {
                NetIDMessage.Serialize(writer);
                UshortUniqueIDMessage.Serialize(writer);
            }
        }
        public struct NetIDMessage
        {
            public string UniqueID;

            public void Deserialize(NetDataReader reader)
            {
                int bytes = reader.AvailableBytes;
                if (bytes != 0)
                {
                    UniqueID = reader.GetString();
                }
                else
                {
                  BNL.LogError($"Unable to read remaining bytes: {bytes}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                if (!string.IsNullOrEmpty(UniqueID))
                {
                    writer.Put(UniqueID);
                }
                else
                {
                    BNL.LogError("Unable to serialize. Field was null or empty.");
                }
            }
        }

        public struct UshortUniqueIDMessage
        {
            public ushort UniqueIDUshort;

            public void Deserialize(NetDataReader reader)
            {
                int bytes = reader.AvailableBytes;
                if (bytes != 0)
                {
                    UniqueIDUshort = reader.GetUShort();
                }
                else
                {
                    BNL.LogError($"Unable to read remaining bytes: {bytes}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(UniqueIDUshort);
            }
        }

        public struct ServerUniqueIDMessages
        {
            public ushort MessageCount;
            public ServerNetIDMessage[] Messages;

            public void Deserialize(NetDataReader reader)
            {
                int bytes = reader.AvailableBytes;
                if (bytes >= sizeof(ushort))
                {
                    MessageCount = reader.GetUShort();
                    if (Messages == null || Messages.Length != MessageCount)
                    {
                        Messages = new ServerNetIDMessage[MessageCount];
                    }
                    for (int Index = 0; Index < MessageCount; Index++)
                    {
                        Messages[Index] = new ServerNetIDMessage();
                        Messages[Index].Deserialize(reader);
                    }
                }
                else
                {
                    Messages = null;
                    BNL.LogError($"Unable to read remaining bytes for MessageCount. Available: {bytes}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                if (Messages != null)
                {
                    writer.Put((ushort)Messages.Length);
                    for (int Index = 0; Index < Messages.Length; Index++)
                    {
                        ServerNetIDMessage message = Messages[Index];
                        message.Serialize(writer);
                    }
                }
                else
                {
                    BNL.LogError("Unable to serialize. Messages array was null.");
                }
            }
        }
    }
}
