using Basis.Scripts.BasisSdk.Players;
public static class BasisBundleConversionNetwork
{
    // Converts AvatarNetworkLoadInformation to BasisLoadableBundle
    public static BasisLoadableBundle ConvertFromNetwork(AvatarNetworkLoadInformation AvatarNetworkLoadInformation)
    {
        BasisLoadableBundle BasisLoadableBundle = new BasisLoadableBundle
        {
            BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle
            {
                MetaURL = AvatarNetworkLoadInformation.AvatarMetaUrl,
                BundleURL = AvatarNetworkLoadInformation.AvatarBundleUrl
            },
            BasisBundleInformation = new BasisBundleInformation(),
            BasisLocalEncryptedBundle = new BasisStoredEncyptedBundle(),
            UnlockPassword = AvatarNetworkLoadInformation.UnlockPassword
        };

        return BasisLoadableBundle;
    }

    // Converts BasisLoadableBundle to AvatarNetworkLoadInformation
    public static AvatarNetworkLoadInformation ConvertToNetwork(BasisLoadableBundle BasisLoadableBundle)
    {
        AvatarNetworkLoadInformation AvatarNetworkLoadInformation = new AvatarNetworkLoadInformation
        {
            AvatarMetaUrl = BasisLoadableBundle.BasisRemoteBundleEncrypted.MetaURL,
            AvatarBundleUrl = BasisLoadableBundle.BasisRemoteBundleEncrypted.BundleURL,
            UnlockPassword = BasisLoadableBundle.UnlockPassword
        };
        return AvatarNetworkLoadInformation;
    }

    // Converts byte array (serialized AvatarNetworkLoadInformation) to AvatarNetworkLoadInformation
    public static AvatarNetworkLoadInformation ConvertToNetwork(byte[] BasisLoadableBundle)
    {
        return AvatarNetworkLoadInformation.DecodeFromBytes(BasisLoadableBundle);
    }

    // Converts byte array (serialized AvatarNetworkLoadInformation) to BasisLoadableBundle
    public static BasisLoadableBundle ConvertNetworkBytesToBasisLoadableBundle(byte[] BasisLoadableBundle)
    {
        AvatarNetworkLoadInformation ANLI = AvatarNetworkLoadInformation.DecodeFromBytes(BasisLoadableBundle);
        return ConvertFromNetwork(ANLI);
    }

    // Converts AvatarNetworkLoadInformation to byte array (serialization)
    public static byte[] ConvertNetworkToByte(AvatarNetworkLoadInformation AvatarNetworkLoadInformation)
    {
        return AvatarNetworkLoadInformation.EncodeToBytes();
    }

    // Converts BasisLoadableBundle to byte array (serialization)
    public static byte[] ConvertBasisLoadableBundleToBytes(BasisLoadableBundle BasisLoadableBundle)
    {
        AvatarNetworkLoadInformation AvatarNetworkLoadInformation = ConvertToNetwork(BasisLoadableBundle);
        return AvatarNetworkLoadInformation.EncodeToBytes();
    }

    // Converts byte array (serialized BasisLoadableBundle) to BasisLoadableBundle
    public static BasisLoadableBundle ConvertBytesToBasisLoadableBundle(byte[] BasisLoadableBundleBytes)
    {
        AvatarNetworkLoadInformation ANLI = AvatarNetworkLoadInformation.DecodeFromBytes(BasisLoadableBundleBytes);
        return ConvertFromNetwork(ANLI);
    }
}
