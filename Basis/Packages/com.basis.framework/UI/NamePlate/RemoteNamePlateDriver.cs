using Basis.Scripts.Drivers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basis.Scripts.UI.NamePlate
{
    public class RemoteNamePlateDriver : MonoBehaviour
    {
        // Use an array for better performance
        private BasisNamePlate[] basisRemotePlayers = new BasisNamePlate[0];
        private int count = 0; // Track the number of active elements
        public static readonly Queue<Action> actions = new Queue<Action>();
        public static RemoteNamePlateDriver Instance;

        public void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Adds a new BasisNamePlate to the array.
        /// </summary>
        public void AddNamePlate(BasisNamePlate newNamePlate)
        {
            if (newNamePlate == null) return;

            // Check if it already exists
            for (int i = 0; i < count; i++)
            {
                if (basisRemotePlayers[i] == newNamePlate) return;
            }

            // Resize if necessary
            if (count >= basisRemotePlayers.Length)
            {
                ResizeArray(basisRemotePlayers.Length == 0 ? 4 : basisRemotePlayers.Length * 2);
            }

            // Add the new nameplate
            basisRemotePlayers[count++] = newNamePlate;
        }

        /// <summary>
        /// Removes an existing BasisNamePlate from the array.
        /// </summary>
        public void RemoveNamePlate(BasisNamePlate namePlateToRemove)
        {
            if (namePlateToRemove == null) return;

            for (int i = 0; i < count; i++)
            {
                if (basisRemotePlayers[i] == namePlateToRemove)
                {
                    // Shift elements down to remove the nameplate
                    for (int j = i; j < count - 1; j++)
                    {
                        basisRemotePlayers[j] = basisRemotePlayers[j + 1];
                    }

                    basisRemotePlayers[--count] = null; // Clear the last element
                    break;
                }
            }
        }

        /// <summary>
        /// Removes a BasisNamePlate by index.
        /// </summary>
        public void RemoveNamePlateAt(int index)
        {
            if (index < 0 || index >= count) return;

            // Shift elements down to remove the nameplate
            for (int i = index; i < count - 1; i++)
            {
                basisRemotePlayers[i] = basisRemotePlayers[i + 1];
            }

            basisRemotePlayers[--count] = null; // Clear the last element
        }

        /// <summary>
        /// Resizes the internal array.
        /// </summary>
        private void ResizeArray(int newSize)
        {
            BasisNamePlate[] newArray = new BasisNamePlate[newSize];
            for (int i = 0; i < count; i++)
            {
                newArray[i] = basisRemotePlayers[i];
            }

            basisRemotePlayers = newArray;
        }
        public float x;
        public float z;
        public void LateUpdate()
        {
            Vector3 Position = BasisLocalCameraDriver.Position;
            for (int i = 0; i < count; i++)
            {
                BasisNamePlate NamePlate =  basisRemotePlayers[i];
                NamePlate.cachedDirection = NamePlate.HipTarget.OutgoingWorldData.position;
                NamePlate.cachedDirection.y += NamePlate.MouthTarget.TposeLocal.position.y / NamePlate.YHeightMultiplier;
                NamePlate.dirToCamera = Position - NamePlate.cachedDirection;
              //  Vector3 Euler = NamePlate.transform.rotation.eulerAngles;
                NamePlate.cachedRotation = Quaternion.Euler(x, Mathf.Atan2(NamePlate.dirToCamera.x, NamePlate.dirToCamera.z) * Mathf.Rad2Deg, z);
                NamePlate.transform.SetPositionAndRotation(NamePlate.cachedDirection, NamePlate.cachedRotation);
            }

            if (actions.Count > 0)
            {
                lock (actions)
                {
                    while (actions.Count > 0)
                    {
                        actions.Dequeue()?.Invoke();
                    }
                }
            }
        }
    }
}
