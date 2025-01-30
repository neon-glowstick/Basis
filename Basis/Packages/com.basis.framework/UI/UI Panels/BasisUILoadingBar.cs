using Basis.Scripts.Addressable_Driver;
using Basis.Scripts.Addressable_Driver.Enums;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Drivers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Basis.Scripts.UI.UI_Panels
{
    [Serializable]
    public class LoadingOperationData
    {
        public string Key;
        public float Percentage;
        public string Display;

        public LoadingOperationData(string key, float percentage, string display)
        {
            Key = key;
            Percentage = percentage;
            Display = display;
        }
    }

    public class BasisUILoadingBar : BasisUIBase
    {
        public TextMeshPro TextMeshPro;
        public SpriteRenderer Renderer;
        public static BasisUILoadingBar Instance;
        public const string LoadingBar = "Packages/com.basis.sdk/Prefabs/UI/Loading Bar.prefab";

        public Vector3 Position;
        public Quaternion Rotation;

        [SerializeField]
        private List<LoadingOperationData> loadingOperations = new List<LoadingOperationData>();

        public static void Initalize()
        {
            Debug.Log("Initializing Loading Bar Event Handlers...");
            BasisSceneLoadDriver.progressCallback.OnProgressReport += ProgressReport;
        }

        public static void DeInitalize()
        {
            Debug.Log("DeInitializing Loading Bar Event Handlers...");
            BasisSceneLoadDriver.progressCallback.OnProgressReport -= ProgressReport;
        }

        private static void ProgressReport(string UniqueID, float progress, string info)
        {
            BasisDeviceManagement.EnqueueOnMainThread(() =>
            {
                if (progress == 100)
                {
                    Debug.Log($"Progress Complete - ID: {UniqueID}");
                    Instance?.RemoveDisplay(UniqueID);
                }
                else
                {
                    Debug.Log($"Progress Report - ID: {UniqueID}, Progress: {progress}, Info: {info}");
                    if (Instance == null)
                    {
                        Debug.Log("Creating Loading Bar Instance...");
                        AddressableGenericResource resource = new AddressableGenericResource(LoadingBar, AddressableExpectedResult.SingleItem);
                        BasisUIBase.OpenMenuNow(resource);
                    }
                    Instance.AddOrUpdateDisplay(UniqueID, progress, info);
                }
            });
        }
        public static void CloseLoadingBar()
        {
            BasisDeviceManagement.EnqueueOnMainThread(() =>
            {
                if (Instance != null)
                {
                    Debug.Log("Closing Loading Bar...");
                    GameObject.Destroy(Instance.gameObject);
                    Instance = null;
                }
            });
        }

        public void AddOrUpdateDisplay(string key, float percentage, string display)
        {
            Debug.Log($"Add/Update Display - Key: {key}, Percentage: {percentage}, Display: {display}");
            var operation = loadingOperations.Find(op => op.Key == key);
            if (operation != null)
            {
                Debug.Log($"Updating Existing Operation - Key: {key}");
                operation.Percentage = percentage;
                operation.Display = display;
            }
            else
            {
                Debug.Log($"Adding New Operation - Key: {key}");
                loadingOperations.Add(new LoadingOperationData(key, percentage, display));
            }
            ProcessQueue();
        }

        public void RemoveDisplay(string key)
        {
            Debug.Log($"Removing Display - Key: {key}");
            BasisDeviceManagement.EnqueueOnMainThread(() =>
            {
                var operation = loadingOperations.Find(op => op.Key == key);
                if (operation != null)
                {
                    loadingOperations.Remove(operation);
                    Debug.Log($"Display Removed - Key: {key}");
                }

                if (loadingOperations.Count > 0)
                {
                    ProcessQueue();
                }
                else
                {
                    Debug.Log("No operations left, closing loading bar...");
                    CloseLoadingBar();
                }
            });
        }

        private void ProcessQueue()
        {
            Debug.Log("Processing Queue...");
            if (loadingOperations.Count > 0 && Instance != null)
            {
                var operation = GetFirstLoadingOperation();
                if (operation != null)
                {
                    Debug.Log($"Updating Display with Operation - Percentage: {operation.Percentage}, Display: {operation.Display}");
                    UpdateDisplay(operation.Percentage, operation.Display);
                }
            }
        }

        private LoadingOperationData GetFirstLoadingOperation()
        {
            Debug.Log("Fetching First Loading Operation...");
            return loadingOperations.Count > 0 ? loadingOperations[0] : null;
        }

        private void UpdateDisplay(float percentage, string display)
        {
            Debug.Log($"Updating Display - Percentage: {percentage}, Display: {display}");
            TextMeshPro.text = display;
            float value = percentage / 4f;
            Renderer.size = new Vector2(value, 2);
        }

        public override void InitalizeEvent()
        {
            Debug.Log("Initializing Loading Bar UI Event...");
            Instance = this;
            this.transform.parent = BasisLocalCameraDriver.Instance.transform;
            this.transform.SetLocalPositionAndRotation(Position, Rotation);
        }

        public override void DestroyEvent()
        {
            Debug.Log("Destroying Loading Bar UI Event...");
        }
    }
}
