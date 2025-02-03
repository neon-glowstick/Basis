using Basis.Scripts.BasisSdk;
using System;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class BasisBundleLoadAsset
{
    public static async Task<GameObject> LoadFromWrapper(BasisTrackedBundleWrapper BasisLoadableBundle, bool UseContentRemoval, Vector3 Position, Quaternion Rotation,bool ModifyScale,Vector3 Scale, Transform Parent = null)
    {
        bool Incremented = false;
        if (BasisLoadableBundle.AssetBundle != null)
        {
            BasisLoadableBundle output = BasisLoadableBundle.LoadableBundle;
            switch (output.BasisBundleInformation.BasisBundleGenerated.AssetMode)
            {
                case "GameObject":
                    {
                        AssetBundleRequest Request = BasisLoadableBundle.AssetBundle.LoadAssetAsync<GameObject>(output.BasisBundleInformation.BasisBundleGenerated.AssetToLoadName);
                        await Request;
                        GameObject loadedObject = Request.asset as GameObject;
                        if (loadedObject == null)
                        {
                            BasisDebug.LogError("Unable to proceed, null Gameobject");
                            BasisLoadableBundle.DidErrorOccur = true;
                            await BasisLoadableBundle.AssetBundle.UnloadAsync(true);
                            return null;
                        }
                        ChecksRequired ChecksRequired = new ChecksRequired();
                        if (loadedObject.TryGetComponent<BasisAvatar>(out BasisAvatar BasisAvatar))
                        {
                            ChecksRequired.DisableAnimatorEvents = true;
                        }
                        ChecksRequired.UseContentRemoval = UseContentRemoval;
                        GameObject CreatedCopy = ContentPoliceControl.ContentControl(loadedObject, ChecksRequired, Position, Rotation, ModifyScale, Scale, Parent);
                        Incremented = BasisLoadableBundle.Increment();
                        return CreatedCopy;
                    }
                default:
                    BasisDebug.LogError("Requested type " + output.BasisBundleInformation.BasisBundleGenerated.AssetMode + " has no handler");
                    return null;
            }
        }
        else
        {
            BasisDebug.LogError("Missing Bundle!");
        }
        BasisDebug.LogError("Returning unable to load gameobject!");
        return null;
    }
    public static async Task<Scene> LoadSceneFromBundleAsync(BasisTrackedBundleWrapper bundle, bool MakeActiveScene, BasisProgressReport progressCallback)
    {
        string UniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        bool AssignedIncrement = false;
        string[] scenePaths = bundle.AssetBundle.GetAllScenePaths();
        if (scenePaths.Length == 0)
        {
            BasisDebug.LogError("No scenes found in AssetBundle.");
            return new Scene();
        }
        if (scenePaths.Length > 1)
        {
            BasisDebug.LogError("More then one scene was found in The Asset Bundle, Please Correct!");
            return new Scene();
        }

        if (!string.IsNullOrEmpty(scenePaths[0]))
        {
            // Load the scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenePaths[0], LoadSceneMode.Additive);
            // Track scene loading progress
            while (!asyncLoad.isDone)
            {
                progressCallback.ReportProgress(UniqueID,50 + asyncLoad.progress * 50, "loading scene"); // Progress from 50 to 100 during scene load
                await Task.Yield();
            }

            BasisDebug.Log("Scene loaded successfully from AssetBundle.");
            Scene loadedScene = SceneManager.GetSceneByPath(scenePaths[0]);
            bundle.MetaLink = loadedScene.path;
            // Set the loaded scene as the active scene
            if (loadedScene.IsValid())
            {
                if (MakeActiveScene)
                {
                    SceneManager.SetActiveScene(loadedScene);
                    AssignedIncrement = bundle.Increment();
                }
                BasisDebug.Log("Scene set as active: " + loadedScene.name);
                progressCallback.ReportProgress(UniqueID, 100, "loading scene"); // Set progress to 100 when done
                return loadedScene;
            }
            else
            {
                BasisDebug.LogError("Failed to get loaded scene.");
            }
        }
        else
        {
            BasisDebug.LogError("Path was null or empty! this should not be happening!");
        }
        return new Scene();
    }
}
