using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Drivers;
using System.Collections.Generic;
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
        const string k_Player = "Player";
        const string k_IgnoreRayCastLayer = "Ignore Raycast";

        public void Initialize(BasisInput basisInput)
        {
            BasisInput = basisInput;
            BasisDeviceMatchableNames = BasisInput.BasisDeviceMatchableNames;
            PhysicHits = new RaycastHit[k_MaxPhysicHitCount];

            // Get the layer number for "Ignore Raycast" layer
            int ignoreRaycastLayer = LayerMask.NameToLayer(k_IgnoreRayCastLayer);

            // Get the layer number for "Player" layer
            int playerLayer = LayerMask.NameToLayer(k_Player);

            // Create a LayerMask that includes all layers
            LayerMask allLayers = ~0;

            // Exclude the "Ignore Raycast" and "Player" layers using bitwise AND and NOT operations
            Mask = allLayers & ~(1 << ignoreRaycastLayer) & ~(1 << playerLayer);

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
                    direction = Direction,
                };
            }
            else
            {
                // TODO: what? where does this come into play?
                ray = BasisLocalCameraDriver.Instance.Camera.ScreenPointToRay(ScreenPoint, Camera.MonoOrStereoscopicEye.Mono);
            }

            PhysicHitCount = Physics.RaycastNonAlloc(ray, PhysicHits, MaxDistance, Mask, TriggerInteraction);
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
        }
    }
}