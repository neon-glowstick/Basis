using LiteNetLib.Utils;
public static partial class SerializableBasis
{
    /// <summary>
    /// we call this from a client to the server
    /// </summary>
    public struct LocalLoadResource
    {
        /// <summary>
        /// 0 = Game object, 1 = Scene,
        /// </summary>
        public byte Mode;
        /// <summary>
        /// this is a unique string that this Object is linked with over the network.
        /// </summary>
        public string LoadedNetID;
        public string UnlockPassword;
        public string MetaURL;
        public string BundleURL;
        public bool IsLocalLoad;

        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public float QuaternionX;
        public float QuaternionY;
        public float QuaternionZ;
        public float QuaternionW;

        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public bool Persist;
        //will never remove this item from the server,
        //if off when player count on server is zero it will be removed.
        public void Deserialize(NetDataReader Writer)
        {
            Mode = Writer.GetByte();
            LoadedNetID = Writer.GetString();
            UnlockPassword = Writer.GetString();
            MetaURL = Writer.GetString();
            BundleURL = Writer.GetString();
            IsLocalLoad = Writer.GetBool();
            Persist = Writer.GetBool();
            if (Mode == 0)
            {
                PositionX = Writer.GetFloat();
                PositionY = Writer.GetFloat();
                PositionZ = Writer.GetFloat();

                QuaternionX = Writer.GetFloat();
                QuaternionY = Writer.GetFloat();
                QuaternionZ = Writer.GetFloat();
                QuaternionW = Writer.GetFloat();

                ScaleX = Writer.GetFloat();
                ScaleY = Writer.GetFloat();
                ScaleZ = Writer.GetFloat();
            }
        }
        public void Serialize(NetDataWriter Writer)
        {
            Writer.Put(Mode);
            Writer.Put(LoadedNetID);
            Writer.Put(UnlockPassword);
            Writer.Put(MetaURL);
            Writer.Put(BundleURL);
            Writer.Put(IsLocalLoad);
            Writer.Put(Persist);
            if (Mode == 0)
            {
                Writer.Put(PositionX);
                Writer.Put(PositionY);
                Writer.Put(PositionZ);

                Writer.Put(QuaternionX);
                Writer.Put(QuaternionY);
                Writer.Put(QuaternionZ);
                Writer.Put(QuaternionW);

                Writer.Put(ScaleX);
                Writer.Put(ScaleY);
                Writer.Put(ScaleZ);
            }
        }
    }
    public struct UnLoadResource
    {
        /// <summary>
        /// 0 = Game object, 1 = Scene,
        /// </summary>
        public byte Mode;
        public string LoadedNetID;
        public void Deserialize(NetDataReader Writer)
        {
            int Bytes = Writer.AvailableBytes;
            if (Bytes != 0)
            {
                Mode = Writer.GetByte();

                LoadedNetID = Writer.GetString();
            }
            else
            {
                BNL.LogError($"Unable to read Remaing bytes where {Bytes}");
            }
        }
        public void Serialize(NetDataWriter Writer)
        {
            Writer.Put(Mode);
            Writer.Put(LoadedNetID);
        }
    }
}
