using Basis.Scripts.Avatar;
using Basis.Scripts.Common;
using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
namespace Basis.Scripts.TransformBinders.BoneControl
{
    [System.Serializable]
    [BurstCompile]
    public class BasisBoneControl
    {
        [NonSerialized]
        public BasisBoneControl Target;

        public float LerpAmountNormal;
        public float LerpAmountFastMovement;
        public float AngleBeforeSpeedup;
        public bool HasRotationalTarget = false;

        public bool HasLineDraw;
        public int LineDrawIndex;
        public bool HasTarget = false;
        public float3 Offset;
        public float LerpAmount;

        [SerializeField]
        public BasisCalibratedCoords OutGoingData = new BasisCalibratedCoords();
        [SerializeField]
        public BasisCalibratedCoords LastRunData = new BasisCalibratedCoords();
        [SerializeField]
        public BasisCalibratedCoords TposeLocal = new BasisCalibratedCoords();
        [SerializeField]
        public BasisCalibratedOffsetData InverseOffsetFromBone = new BasisCalibratedOffsetData();
        [SerializeField]
        public BasisCalibratedCoords IncomingData = new BasisCalibratedCoords();
        [SerializeField]
        public BasisCalibratedCoords OutgoingWorldData = new BasisCalibratedCoords();
        public int GizmoReference = -1;
        public bool HasGizmo = false;

        public int TposeGizmoReference = -1;
        public bool TposeHasGizmo = false;
        public Action VirtualRun;
        public bool HasVirtualOverride;
        public float trackersmooth = 25;

        public bool IsHintRoleIgnoreRotation = false;
        [BurstCompile]
        public void ComputeMovement(float DeltaTime)
        {
            NotProcessing = !HasBone || Cullable;
            if (NotProcessing)
            {
                return;
            }
            if (HasTracked == BasisHasTracked.HasTracker)
            {

                ///this needs to be refactored to understand each part of the body and a generic mode.
                ///start off with a distance limiter for the hips.
                ///could also be a step at the end for every targeted type
                if (InverseOffsetFromBone.Use)
                {
                    
                    if (IsHintRoleIgnoreRotation == false)
                    {                    // Update the position of the secondary transform to maintain the initial offset
                        OutGoingData.position = Vector3.Lerp(OutGoingData.position, IncomingData.position + math.mul(IncomingData.rotation, InverseOffsetFromBone.position), trackersmooth);
                        // Update the rotation of the secondary transform to maintain the initial offset
                        OutGoingData.rotation = Quaternion.Slerp(OutGoingData.rotation, math.mul(IncomingData.rotation, InverseOffsetFromBone.rotation), trackersmooth);
                    }
                    else
                    {
                        OutGoingData.rotation = Quaternion.identity;
                        // Update the position of the secondary transform to maintain the initial offset
                        OutGoingData.position = Vector3.Lerp(OutGoingData.position, IncomingData.position + math.mul(IncomingData.rotation, InverseOffsetFromBone.position), trackersmooth);
                    }
                }
                else
                {
                    ///this is going to the generic always accurate fake skeleton
                    OutGoingData.rotation = IncomingData.rotation;
                    OutGoingData.position = IncomingData.position;
                }
            }
            else
            {
                if (HasVirtualOverride)
                {
                    VirtualRun?.Invoke();
                }
                else
                {
                    //this is essentially the default behaviour, most of it is normally Virtually Overriden
                    //relying on a one size fits all shoe is wrong and as of such we barely use this anymore.
                    if (HasRotationalTarget)
                    {
                        OutGoingData.rotation = ApplyLerpToQuaternion(DeltaTime, LastRunData.rotation, Target.OutGoingData.rotation);
                    }

                    if (HasTarget)
                    {
                        // Apply the rotation offset using math.mul
                        float3 customDirection = math.mul(Target.OutGoingData.rotation, Offset);

                        // Calculate the target outgoing position with the rotated offset
                        float3 targetPosition = Target.OutGoingData.position + customDirection;

                        float lerpFactor = ClampInterpolationFactor(LerpAmount, DeltaTime);

                        // Interpolate between the last position and the target position
                        OutGoingData.position = math.lerp(LastRunData.position, targetPosition, lerpFactor);
                    }
                }
            }
        }
        [BurstCompile]
        public Quaternion ApplyLerpToQuaternion(float DeltaTime, Quaternion CurrentRotation, Quaternion FutureRotation)
        {
            // Calculate the dot product once to check similarity between rotations
            float dotProduct = math.dot(CurrentRotation, FutureRotation);

            // If quaternions are nearly identical, skip interpolation
            if (dotProduct > 0.999999f)
            {
                return FutureRotation;
            }

            // Calculate angle difference, avoid acos for very small differences
            float angleDifference = math.acos(math.clamp(dotProduct, -1f, 1f));

            // If the angle difference is very small, skip interpolation
            if (angleDifference < math.EPSILON)
            {
                return FutureRotation;
            }

            // Cached LerpAmount values for normal and fast movement
            float lerpAmountNormal = LerpAmountNormal;
            float lerpAmountFastMovement = LerpAmountFastMovement;

            // Timing factor for speed-up
            float timing = math.min(angleDifference / AngleBeforeSpeedup, 1f);

            // Interpolate between normal and fast movement rates based on angle
            float lerpAmount = lerpAmountNormal + (lerpAmountFastMovement - lerpAmountNormal) * timing;

            // Apply frame-rate-independent lerp factor
            float lerpFactor = ClampInterpolationFactor(lerpAmount, DeltaTime);

            // Perform spherical interpolation (slerp) with the optimized factor
            return math.slerp(CurrentRotation, FutureRotation, lerpFactor);
        }
        [BurstCompile]
        private float ClampInterpolationFactor(float lerpAmount, float DeltaTime)
        {
            // Clamp the interpolation factor to ensure it stays between 0 and 1
            return math.clamp(lerpAmount * DeltaTime, 0f, 1f);
        }

        [HideInInspector]
        public bool Cullable = false;
        [SerializeField]
        public string Name;
        [SerializeField]
        [HideInInspector]
        private Color gizmoColor = Color.blue;
        [SerializeField]
        [HideInInspector]
        public bool HasBone = false;
        [SerializeField]
        public Transform BoneTransform;
        [HideInInspector]
        public bool HasEvents = false;
        // Events for property changes
        public System.Action<BasisHasTracked> OnHasTrackerDriverChanged;
        // Backing fields for the properties
        [SerializeField]
        private BasisHasTracked hasTrackerDriver = BasisHasTracked.HasNoTracker;
        // Properties with get/set accessors
        public BasisHasTracked HasTracked
        {
            get => hasTrackerDriver;
            set
            {
                if (hasTrackerDriver != value)
                {
                    // BasisDebug.Log("Setting Tracker To has Tracker Position Driver " + value);
                    hasTrackerDriver = value;
                    OnHasTrackerDriverChanged?.Invoke(value);
                }
            }
        }
        [HideInInspector]
        public bool NotProcessing = false;
        // Events for property changes
        public UnityEvent OnHasRigChanged = new UnityEvent();

        public UnityEvent<float, float> WeightsChanged = new UnityEvent<float, float>();
        // Backing fields for the properties
        [SerializeField]
        private BasisHasRigLayer hasRigLayer = BasisHasRigLayer.HasNoRigLayer;
        // Properties with get/set accessors
        public BasisHasRigLayer HasRigLayer
        {
            get => hasRigLayer;
            set
            {
                if (hasRigLayer != value)
                {
                    hasRigLayer = value;
                    OnHasRigChanged.Invoke();
                }
            }
        }
        [HideInInspector]
        [SerializeField]
        private float positionWeight = 1;
        [HideInInspector]
        [SerializeField]
        private float rotationWeight = 1;
        public float PositionWeight
        {
            get => positionWeight;
            set
            {
                if (positionWeight != value)
                {
                    positionWeight = value;
                    WeightsChanged.Invoke(positionWeight, rotationWeight);
                }
            }
        }
        public float RotationWeight
        {
            get => rotationWeight;
            set
            {
                if (rotationWeight != value)
                {
                    rotationWeight = value;
                    WeightsChanged.Invoke(positionWeight, rotationWeight);
                }
            }
        }
        [HideInInspector]
        public BasisGeneralLocation GeneralLocation;
        public Color Color { get => gizmoColor; set => gizmoColor = value; }
        public void Initialize()
        {
            if (HasBone)
            {
                BoneTransform.GetLocalPositionAndRotation(out Vector3 position, out Quaternion Rotation);
                LastRunData.position = position;
                LastRunData.rotation = Rotation;
            }
        }
        public void ApplyMovement()
        {
            if (NotProcessing)
            {
                return;
            }
            LastRunData.position = OutGoingData.position;
            LastRunData.rotation = OutGoingData.rotation;
            BoneTransform.SetLocalPositionAndRotation(OutGoingData.position, OutGoingData.rotation);
            BoneTransform.GetPositionAndRotation(out Vector3 position, out Quaternion Rotation);

            OutgoingWorldData.position = position;
            OutgoingWorldData.rotation = Rotation;
        }
    }
}
