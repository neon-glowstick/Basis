using LiteNetLib.Utils;
using System;

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
                    Console.Error.WriteLine($"Unable to read remaining bytes: {bytes}");
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
                    Console.Error.WriteLine("Unable to serialize. Field was null or empty.");
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
                    Console.Error.WriteLine($"Unable to read remaining bytes: {bytes}");
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
                    Messages = new ServerNetIDMessage[MessageCount];
                    for (int i = 0; i < MessageCount; i++)
                    {
                        Messages[i] = new ServerNetIDMessage();
                        Messages[i].Deserialize(reader);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Unable to read remaining bytes for MessageCount. Available: {bytes}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                if (Messages != null)
                {
                    writer.Put((ushort)Messages.Length);
                    foreach (var message in Messages)
                    {
                        message.Serialize(writer);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unable to serialize. Messages array was null.");
                }
            }
        }
    }
}
