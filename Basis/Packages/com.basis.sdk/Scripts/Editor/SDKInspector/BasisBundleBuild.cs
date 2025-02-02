using System.Security.Cryptography;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Threading.Tasks;

public static class BasisBundleBuild
{
    public static async Task GameObjectBundleBuild(BasisContentBase BasisContentBase)
    {
        Debug.Log("Starting GameObjectBundleBuild...");

        if (ErrorChecking(BasisContentBase) == false)
        {
            return;
        }

        Debug.Log("Passed error checking for GameObjectBundleBuild...");

        BasisBundleInformation(BasisContentBase, out BasisAssetBundleObject Objects, out BasisBundleInformation Information, out string hexString);
        Debug.Log($"Generated bundle information. Hex string: {hexString}");

        if (CheckIfIL2CPPIsInstalled())
        {
            Debug.Log("IL2CPP is installed. Proceeding to build asset bundle...");
            await BasisAssetBundlePipeline.BuildAssetBundle(BasisContentBase.gameObject, Objects, Information, hexString);
            Debug.Log("Successfully built GameObject asset bundle.");
        }
        else
        {
            Debug.LogError("Missing IL2CPP. Please install from Unity Hub!");
        }
    }

    public static async Task SceneBundleBuild(BasisContentBase BasisContentBase)
    {
        Debug.Log("Starting SceneBundleBuild...");

        if (ErrorChecking(BasisContentBase) == false)
        {
            return;
        }

        Debug.Log("Passed error checking for SceneBundleBuild...");

        BasisBundleInformation(BasisContentBase, out BasisAssetBundleObject Objects, out BasisBundleInformation Information, out string hexString);
        Debug.Log($"Generated bundle information. Hex string: {hexString}");

        if (CheckIfIL2CPPIsInstalled())
        {
            Debug.Log("IL2CPP is installed. Proceeding to build scene asset bundle...");
            Scene activeScene = SceneManager.GetActiveScene();
            await BasisAssetBundlePipeline.BuildAssetBundle(activeScene, Objects, Information, hexString);
            Debug.Log("Successfully built Scene asset bundle.");
        }
        else
        {
            Debug.LogError("Missing IL2CPP. Please install from Unity Hub!");
        }
    }

    public static void BasisBundleInformation(BasisContentBase BasisContentBase, out BasisAssetBundleObject BasisAssetBundleObject, out BasisBundleInformation basisBundleInformation, out string hexString)
    {
        Debug.Log("Fetching BasisBundleInformation...");

        BasisAssetBundleObject = AssetDatabase.LoadAssetAtPath<BasisAssetBundleObject>(BasisAssetBundleObject.AssetBundleObject);
        basisBundleInformation = new BasisBundleInformation
        {
            BasisBundleDescription = BasisContentBase.BasisBundleDescription
        };

        Debug.Log("Generating random bytes for hex string...");
        byte[] randomBytes = GenerateRandomBytes(32);
        hexString = ByteArrayToHexString(randomBytes);
        Debug.Log($"Generated hex string: {hexString}");
    }

    public static bool ErrorChecking(BasisContentBase BasisContentBase)
    {
        Debug.Log("Performing error checking...");

        if (string.IsNullOrEmpty(BasisContentBase.BasisBundleDescription.AssetBundleName))
        {
            Debug.LogError("Name was empty!");
            EditorUtility.DisplayDialog("Missing the name", "Please provide a name in the field", "OK");
            return false;
        }
        if (string.IsNullOrEmpty(BasisContentBase.BasisBundleDescription.AssetBundleDescription))
        {
            Debug.LogError("Description was empty!");
            EditorUtility.DisplayDialog("Missing the description", "Please provide a description in the field", "OK");
            return false;
        }

        Debug.Log("Error checking passed.");
        return true;
    }

    public static bool CheckIfIL2CPPIsInstalled()
    {
        Debug.Log("Checking if IL2CPP is installed...");

        var playbackEndingDirectory = BuildPipeline.GetPlaybackEngineDirectory(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None, false);
        bool isInstalled = !string.IsNullOrEmpty(playbackEndingDirectory) && Directory.Exists(Path.Combine(playbackEndingDirectory, "Variations", "il2cpp"));

        Debug.Log(isInstalled ? "IL2CPP is installed." : "IL2CPP is NOT installed.");
        return isInstalled;
    }

    // Generates a random byte array of specified length
    public static byte[] GenerateRandomBytes(int length)
    {
        Debug.Log($"Generating {length} random bytes...");
        byte[] randomBytes = new byte[length];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        Debug.Log("Random bytes generated successfully.");
        return randomBytes;
    }

    // Converts a byte array to a Base64 encoded string
    public static string ByteArrayToBase64String(byte[] byteArray)
    {
        Debug.Log("Converting byte array to Base64 string...");
        return Convert.ToBase64String(byteArray);
    }

    // Converts a byte array to a hexadecimal string
    public static string ByteArrayToHexString(byte[] byteArray)
    {
        Debug.Log("Converting byte array to hexadecimal string...");
        StringBuilder hex = new StringBuilder(byteArray.Length * 2);
        foreach (byte b in byteArray)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        Debug.Log("Hexadecimal string conversion successful.");
        return hex.ToString();
    }
}
