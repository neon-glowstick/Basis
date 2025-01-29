using Basis.Scripts.Addressable_Driver;
using Basis.Scripts.Addressable_Driver.Enums;
using Basis.Scripts.Drivers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Basis.Scripts.UI.UI_Panels
{
    public class BasisUILoadingBar : BasisUIBase
    {
        public TextMeshPro TextMeshPro;
        public SpriteRenderer Renderer;
        public static BasisUILoadingBar Instance;
        public const string LoadingBar = "Packages/com.basis.sdk/Prefabs/UI/Loading Bar.prefab";

        public Vector3 Position;
        public Quaternion Rotation;
        // Dictionary to manage multiple loading operations by unique keys
        private static readonly Dictionary<string, LoadingOperation> loadingOperations = new Dictionary<string, LoadingOperation>();

        // Class to encapsulate each loading operation
        private class LoadingOperation
        {
            public float Percentage;
            public string Display;

            public LoadingOperation(float percentage, string display)
            {
                Percentage = percentage;
                Display = display;
            }
        }
        public static void Initalize()
        {
            BasisSceneLoadDriver.progressCallback.OnProgressReport += ProgresReport;
            BasisSceneLoadDriver.progressCallback.OnProgressStart += StartProgress;
            BasisSceneLoadDriver.progressCallback.OnProgressComplete += OnProgressComplete;
        }
        public static void DeInitalize()
        {
            BasisSceneLoadDriver.progressCallback.OnProgressReport -= ProgresReport;
            BasisSceneLoadDriver.progressCallback.OnProgressStart -= StartProgress;
            BasisSceneLoadDriver.progressCallback.OnProgressComplete -= OnProgressComplete;
        }

        private static void ProgresReport(string UniqueID, float progress, string info)
        {
            TryGetInstance();
            Instance.AddOrUpdateDisplay(info, progress, info);
        }
        private static void StartProgress(string UniqueID)
        {
            TryGetInstance();
            //   Instance.AddOrUpdateDisplay("SceneLoad", 0, "SceneLoad");
        }

        private static void OnProgressComplete(string UniqueID)
        {
            if (Instance != null)
            {
                Instance.RemoveDisplay(UniqueID);
            }
        }

        public static void TryGetInstance()
        {
            if (Instance == null)
            {
                AddressableGenericResource resource = new AddressableGenericResource(LoadingBar, AddressableExpectedResult.SingleItem);
                BasisUIBase.OpenMenuNow(resource);
            }
        }

        public static void CloseLoadingbar()
        {
            if (Instance != null)
            {
                Instance = null;
                GameObject.Destroy(Instance.gameObject);
            }
        }

        public void AddOrUpdateDisplay(string key, float percentage, string display)
        {
            EnqueueOnMainThread(() =>
            {
                if (loadingOperations.ContainsKey(key))
                {
                    // Update existing operation
                    loadingOperations[key].Percentage = percentage;
                    loadingOperations[key].Display = display;
                }
                else
                {
                    // Add new operation
                    loadingOperations[key] = new LoadingOperation(percentage, display);
                }
                ProcessQueue();
            });
        }

        public void RemoveDisplay(string key)
        {
            EnqueueOnMainThread(() =>
            {
                if (loadingOperations.ContainsKey(key))
                {
                    loadingOperations.Remove(key);
                }

                if (loadingOperations.Count > 0)
                {
                    ProcessQueue();
                }
                else
                {
                    CloseLoadingbar(); // No operations left, destroy the loading bar
                }
            });
        }

        private void ProcessQueue()
        {
            if (loadingOperations.Count > 0 && Instance != null)
            {
                // Get the first operation in the dictionary
                var operation = GetFirstLoadingOperation();
                if (operation != null)
                {
                    UpdateDisplay(operation.Percentage, operation.Display);
                }
            }
        }

        private LoadingOperation GetFirstLoadingOperation()
        {
            foreach (var operation in loadingOperations.Values)
            {
                return operation;
            }
            return null;
        }

        private void UpdateDisplay(float percentage, string display)
        {
            TextMeshPro.text = display;
            float value = percentage * 25;
            Renderer.size = new Vector2(value, 2);
        }

        public override void InitalizeEvent()
        {
            Instance = this;
            this.transform.parent = BasisLocalCameraDriver.Instance.transform;
            this.transform.SetLocalPositionAndRotation(Position, Rotation);
        }

        public override void DestroyEvent()
        {
        }

        // Queue to hold actions that need to be run on the main thread
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();

        private void Update()
        {
            // Process actions on the main thread
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count != 0)
                {
                    mainThreadActions.Dequeue()?.Invoke();
                }
            }
        }

        // Helper method to enqueue actions to be executed on the main thread
        private static void EnqueueOnMainThread(Action action)
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(action);
            }
        }
    }
}
