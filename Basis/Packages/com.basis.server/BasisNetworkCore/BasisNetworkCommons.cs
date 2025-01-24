namespace Basis.Network.Core
{
    public static class BasisNetworkCommons
    {
        public const int NetworkIntervalPoll = 15;
        /// <summary>
        /// when adding a new message we need to increase this
        /// will function up to 64
        /// </summary>
        public const byte TotalChannels = 16;
        /// <summary>
        /// channel zero is only used for unreliable methods
        /// we fall it through to stop bugs
        /// </summary>
        public const byte FallChannel = 0;
        /// <summary>
        /// this is normally avatar movement only can be used once!
        /// </summary>
        public const byte MovementChannel = 1;
        /// <summary>
        /// this is what people use voice data only can be used once!
        /// </summary>
        public const byte VoiceChannel = 2;
        /// <summary>
        /// this is what people use to send data on the scene network
        /// </summary>
        public const byte SceneChannel = 3;
        /// <summary>
        /// this is what people use to send data on there avatar
        /// </summary>
        public const byte AvatarChannel = 4;
        /// <summary>
        /// Message to create a remote player entity
        /// </summary>
        public const byte CreateRemotePlayer = 5;
        /// <summary>
        /// message to swap to a different avatar
        /// </summary>
        public const byte AvatarChangeMessage = 6;
        /// <summary>
        /// Ownership Response is when we get the current owner
        /// </summary>
        public const byte GetCurrentOwnerRequest = 7;
        /// <summary>
        /// changes current owner of a string
        /// </summary>
        public const byte ChangeCurrentOwnerRequest = 8;
        /// <summary>
        /// Remove Current Ownership
        /// </summary>
        public const byte RemoveCurrentOwnerRequest = 9;
        /// <summary>
        /// the audio recipients that can here
        /// </summary>
        public const byte AudioRecipients = 10;
        /// <summary>
        /// Removes a players entity
        /// </summary>
        public const byte Disconnection = 11;
        /// <summary>
        /// assign a net id (string to ushort)
        /// </summary>
        public const byte netIDAssign = 12;
        /// <summary>
        /// assign a array of net id (string to ushort)
        /// </summary>
        public const byte NetIDAssigns = 13;
        /// <summary>
        /// load a resource (scene,gameobject,script,asset) whatever the implementation is
        /// </summary>
        public const byte LoadResourceMessage = 14;
        /// <summary>
        /// Unload a Resource
        /// </summary>
        public const byte UnloadResourceMessage = 15;
    }
}
