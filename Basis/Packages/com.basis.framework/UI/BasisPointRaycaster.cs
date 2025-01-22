using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Basis.Scripts.UI
{
    public partial class BasisPointRaycaster : BaseRaycaster
    {
        public Vector3 Direction = Vector3.forward;
        public float MaxDistance = 30;
        public LayerMask Mask;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.UseGlobal;
        public bool UseWorldPosition = true;

        /// <summary>
        /// Modified externally by Eye Input
        /// </summary>
        public Vector2 ScreenPoint { get; set; }
        public Ray ray { get; private set; }
        public RaycastHit[] PhysicHits { get; private set; }
        public int PhysicHitCount { get; private set; }
        const int k_MaxPhysicHitCount = 8;


        public BasisDeviceMatchSettings BasisDeviceMatchableNames;
        public BasisInput BasisInput;

        public override Camera eventCamera => BasisLocalCameraDriver.Instance.Camera;
        const string k_PlayerLayer = "Player";
        const string k_IgnoreRayCastLayer = "Ignore Raycast";
        const string k_LocalPlayerAvatarLayer = "LocalPlayerAvatar";

        public void Initialize(BasisInput basisInput)
        {
            BasisInput = basisInput;
            BasisDeviceMatchableNames = BasisInput.BasisDeviceMatchableNames;
            PhysicHits = new RaycastHit[k_MaxPhysicHitCount];

            // Get the layer number for "Ignore Raycast" layer
            int ignoreRaycastLayer = LayerMask.NameToLayer(k_IgnoreRayCastLayer);

            // Get the layer number for "Player" layer
            int playerLayer = LayerMask.NameToLayer(k_PlayerLayer);

            int LocalPlayerAvatar = LayerMask.NameToLayer(k_LocalPlayerAvatarLayer);

            // Create a LayerMask that includes all layers
            LayerMask allLayers = ~0;

            // Exclude the "Ignore Raycast" and "Player" layers using bitwise AND and NOT operations
            Mask = allLayers & ~(1 << ignoreRaycastLayer) & ~(1 << playerLayer) & ~(1 << LocalPlayerAvatar);

            // Create the ray with the adjusted starting position and direction
            ray = new Ray(Vector3.zero, Direction);
        }

        private Vector3 LastRotation;
        /// <summary>
        /// Run after Input control apply, before `AfterControlApply`
        /// </summary>
        public void UpdateRaycast()
        {
            if (LastRotation != BasisDeviceMatchableNames.RotationRaycastOffset)
            {
                this.transform.localRotation = Quaternion.Euler(BasisDeviceMatchableNames.RotationRaycastOffset);
                LastRotation = BasisDeviceMatchableNames.RotationRaycastOffset;
            }
            if (UseWorldPosition)
            {
                transform.GetPositionAndRotation(out Vector3 Position, out Quaternion Rotation);
                // Create the ray with the adjusted starting position and direction
                ray = new Ray()
                {
                    origin = Position + (Rotation * BasisDeviceMatchableNames.PositionRayCastOffset),
                    direction = transform.forward,
                };
            }
            else
            {
                // TODO: what? where does this come into play?
                ray = BasisLocalCameraDriver.Instance.Camera.ScreenPointToRay(ScreenPoint, Camera.MonoOrStereoscopicEye.Mono);
            }

            PhysicHitCount = Physics.RaycastNonAlloc(ray, PhysicHits, MaxDistance, Mask, TriggerInteraction);
            // order from raycast is undefined, sort by distance
            Array.Sort(PhysicHits, (a, b) => a.distance.CompareTo(b.distance));

            // if (PhysicHitCount > 0)
            // {
            //     var hits = PhysicHits.ToList();

            //     hits.ConvertAll(x => x.collider != null ? x.collider.gameObject.name : null);
            //     BasisDebug.Log("First Hit:" + hits[0].collider.gameObject.name + "\n Raycast Hits: " + string.Join(", ", hits.ToArray()[..PhysicHitCount]));
            // }
        }

        public ReadOnlySpan<RaycastHit> GetHits()
        {
            return PhysicHits.AsSpan()[..PhysicHitCount];
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * MaxDistance);
        }
    }
}
