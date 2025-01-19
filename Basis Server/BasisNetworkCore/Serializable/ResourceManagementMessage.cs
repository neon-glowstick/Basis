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
        public void Deserialize(NetDataReader Writer)
        {
            Mode = Writer.GetByte();
            LoadedNetID = Writer.GetString();
            UnlockPassword = Writer.GetString();
            MetaURL = Writer.GetString();
            BundleURL = Writer.GetString();
            IsLocalLoad = Writer.GetBool();
        }
        public void Serialize(NetDataWriter Writer)
        {
            Writer.Put(Mode);
            Writer.Put(LoadedNetID);
            Writer.Put(UnlockPassword);
            Writer.Put(MetaURL);
            Writer.Put(BundleURL);
            Writer.Put(IsLocalLoad);
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
