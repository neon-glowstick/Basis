using LiteNetLib.Utils;
using System;
public static partial class SerializableBasis
{
    public struct AdditionalAvatarData
    {
        public byte messageIndex;
        public byte[] array;
        public void Deserialize(NetDataReader Writer)
        {
            int Bytes = Writer.AvailableBytes;
            if (Bytes != 0)
            {
                messageIndex = Writer.GetByte();

                byte PayloadSize = Writer.GetByte();
                if (array == null || array.Length != PayloadSize)
                {
                    array = new byte[PayloadSize];
                }
                Writer.GetBytes(array, PayloadSize);
                //89 * 2 = 178 + 12 + 14 = 204
                //now 178 for muscles, 3*4 for position 12, 4*4 for rotation 16-2 (W is half) = 204
            }
            else
            {
                BNL.LogError($"Unable to read Remaing bytes where {Bytes}");
            }
        }
        public void Serialize(NetDataWriter Writer)
        {
            if (array == null)
            {
                BNL.LogError("array was null!!");
            }
            else
            {
                Writer.Put(messageIndex);
                byte Size = (byte)array.Length;
                Writer.Put(Size);
                Writer.Put(array);
            }
        }
    }
}
