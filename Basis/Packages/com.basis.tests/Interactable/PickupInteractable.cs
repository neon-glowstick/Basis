using System;
using System.Linq;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Device_Management.Devices.Desktop;
using Basis.Scripts.Drivers;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PickupInteractable : InteractableObject
{
    [Header("Reparent Settings")]
    public bool KinematicWhileInteracting = true;
    public float DesktopRotateSpeed = 0.1f;
    [Tooltip("Unity units per scroll step")]
    public float DesktopZoopSpeed = 0.2f;
    public float DesktopZoopMinDistance = 0.2f;
    [Tooltip("Generate a mesh on start to approximate the referenced collider")]
    public bool GenerateColliderMesh = true;

    [Header("References")]
    public Collider ColliderRef;
    public Rigidbody RigidRef;
    public ParentConstraint ConstraintRef;

    // internal values
    private GameObject HighlightClone;
    private AsyncOperationHandle<Material> asyncOperationHighlightMat;
    private Material ColliderHighlightMat;
    private bool _previousKinematicValue = true;

    // constants
    const string k_LoadMaterialAddress = "Interactable/InteractHighlightMat.mat";
    const string k_CloneName = "HighlightClone";
    const float k_DesktopZoopSmoothing = 0.2f;
    const float k_DesktopZoopMaxVelocity = 10f;

    // events
    public Action OnPickup;
    public Action OnDrop;
    public Action OnPickupHoverStart;
    public Action<bool> OnPickupHoverEnd;

    public void Start()
    {
        if (RigidRef == null)
        {
            TryGetComponent(out RigidRef);
        }
        if (ColliderRef == null)
        {
            TryGetComponent(out ColliderRef);
        }
        if (ConstraintRef == null)
        {
            if (!TryGetComponent(out ConstraintRef))
            {
                ConstraintRef = gameObject.AddComponent<ParentConstraint>();
            }
            var nullSource = new ConstraintSource()
            {
                sourceTransform = null,
                weight = 1,
            };
            ConstraintRef.AddSource(nullSource);
        }

        AsyncOperationHandle<Material> op = Addressables.LoadAssetAsync<Material>(k_LoadMaterialAddress);
        ColliderHighlightMat = op.WaitForCompletion();
        asyncOperationHighlightMat = op;

        if (GenerateColliderMesh)
        {
            // NOTE: Collider mesh highlight position and size is only updated on Start(). 
            //      If you wish to have the highlight update at runtime do that elsewhere or make a different InteractableObject Script
            HighlightClone = ColliderClone.CloneColliderMesh(ColliderRef, gameObject.transform, k_CloneName);

            if (HighlightClone != null)
            {
                if (HighlightClone.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    meshRenderer.material = ColliderHighlightMat;
                }
                else
                {
                    BasisDebug.LogWarning("Pickup Interactable could not find MeshRenderer component on mesh clone. Highlights will be broken");
                }
            }
        }
    }
    public void HighlightObject(bool highlight)
    {
        if (ColliderRef && HighlightClone)
        {
            HighlightClone.SetActive(highlight);
        }
    }

    public void SetParentConstraint(Transform source)
    {
        // ignore source count, only modify the 0 index
        var newSource = new ConstraintSource()
        {
            sourceTransform = source,
            weight = 1,
        };
        ConstraintRef.SetSource(0, newSource);

        if (CanEquip)
        {
            ConstraintRef.SetTranslationOffset(0, equipPos);
            ConstraintRef.SetRotationOffset(0, equipRot.eulerAngles);
        }
        else if (source != null)
        {
            ConstraintRef.SetTranslationOffset(0, source.InverseTransformPoint(transform.position));
            ConstraintRef.SetRotationOffset(0, (Quaternion.Inverse(source.rotation) * transform.rotation).eulerAngles);
        }


        // force constraint weight
        ConstraintRef.weight = 1;
        ConstraintRef.constraintActive = source != null;
    }

    public override bool CanHover(BasisInput input)
    {
        return !Inputs.AnyInteracting() && 
            input.TryGetRole(out BasisBoneTrackedRole role) && 
            Inputs.TryGetByRole(role, out BasisInputWrapper found) &&
            found.Source == null && 
            !found.IsInteracting &&
            IsWithinRange(input.transform.position);
    }
    public override bool CanInteract(BasisInput input)
    {
        // currently hovering can interact only, only one interacting at a time
        return !Inputs.AnyInteracting() && 
            Inputs.Find(input) != null &&
            input.TryGetRole(out BasisBoneTrackedRole role) &&
            Inputs.TryGetByRole(role, out BasisInputWrapper found) &&
            found.Source != null && 
            !found.IsInteracting &&
            IsWithinRange(input.transform.position);
    }

    public override void OnHoverStart(BasisInput input)
    {
        var found = Inputs.Find(input);
        var added = Inputs.AddInputByRole(input, false);
        if (found != null)
            BasisDebug.LogWarning(nameof(PickupInteractable) + " found input source in list OnHover, this shouldn't happen");
        if (!added)
            BasisDebug.LogWarning(nameof(PickupInteractable) + " did not find role for input on hover");
        
        OnPickupHoverStart?.Invoke();
        HighlightObject(true);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        if (input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out _))
        {
            if (!willInteract)
            {
                if (!Inputs.RemoveByRole(role))
                {
                    BasisDebug.LogWarning(nameof(PickupInteractable) + " found input by role but could not remove by it, this is a bug.");
                }
            }
            OnPickupHoverEnd?.Invoke(willInteract);
            HighlightObject(false);
        }
    }
    public override void OnInteractStart(BasisInput input)
    {
        if(input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out BasisInputWrapper wrapper))
        {
            // same input that was highlighting previously
            if (!wrapper.IsInteracting)
            {
                if (RigidRef != null && KinematicWhileInteracting)
                {
                    _previousKinematicValue = RigidRef.isKinematic;
                    RigidRef.isKinematic = true;
                }

                // Set ownership to the local player
                // syncNetworking.IsOwner = true;
                Inputs.AddInputByRole(input, true);
                RequiresUpdateLoop = true;
                OnPickup?.Invoke();
                SetParentConstraint(input.transform);
            }
            else
            {
                Debug.LogWarning("Input source interacted with ReparentInteractable without highlighting first.");
            }
        }
        else
        {
            BasisDebug.LogWarning(nameof(PickupInteractable) + " did not find role for input on Interact start");
        }
    }

    public override void OnInteractEnd(BasisInput input)
    {
        if(input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out BasisInputWrapper wrapper))
        {
            if (wrapper.IsInteracting)
            {
                Inputs.RemoveByRole(role);

                if (KinematicWhileInteracting && RigidRef != null)
                {
                    RigidRef.isKinematic = _previousKinematicValue;
                }

                RequiresUpdateLoop = false;
                // cleanup Desktop Manipulation since InputUpdate isnt run again till next pickup
                if (pauseHead)
                {
                    BasisAvatarEyeInput.Instance.UnPauseHead(nameof(PickupInteractable) + ": " + gameObject.name);
                    targetOffset = Vector3.zero;
                    currentZoopVelocity = Vector3.zero;
                    pauseHead = false;
                }
                // syncNetworking.IsOwner = false;
                OnDrop?.Invoke();
                SetParentConstraint(null);
            }
        }

        
    }
    public override void InputUpdate()
    {
        if (Inputs.AnyInteracting())
        {
            // Optionally, match the rotation.
            //  transform.rotation = target.rotation;
            if (Basis.Scripts.Device_Management.BasisDeviceManagement.IsUserInDesktop())
            {
                PollDesktopManipulation();
            }
        }
    }


    public override bool IsInteractingWith(BasisInput input)
    {
        var found = Inputs.Find(input);
        return found.HasValue && found.Value.IsInteracting;
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        var found = Inputs.Find(input);
        return found.HasValue && !found.Value.IsInteracting;
    }

    // this is cached, use it
    public override Collider GetCollider()
    {
        return ColliderRef;
    }

    private bool pauseHead = false;
    private Vector3 targetOffset = Vector3.zero;
    private Vector3 currentZoopVelocity = Vector3.zero;
    private void PollDesktopManipulation()
    {
        if (Mouse.current.middleButton.isPressed)
        {
            if(!pauseHead)
            {
                BasisAvatarEyeInput.Instance.PauseHead(nameof(PickupInteractable) + ": " + gameObject.name);
                pauseHead = true;
            }

            // drag rotate
            var delta = Mouse.current.delta.ReadValue();
            Quaternion yRotation = Quaternion.AngleAxis(delta.x * DesktopRotateSpeed, Vector3.up);
            Quaternion xRotation = Quaternion.AngleAxis(-delta.y * DesktopRotateSpeed, Vector3.right);

            var rotation = yRotation * xRotation * Quaternion.Euler(ConstraintRef.rotationOffsets[0]);
            ConstraintRef.SetRotationOffset(0, rotation.eulerAngles);

            // scroll zoop
            float mouseScroll = Mouse.current.scroll.ReadValue().y; // only ever 1, 0, -1

            Vector3 currentOffset = ConstraintRef.translationOffsets[0];
            if (targetOffset == Vector3.zero)
            {
                // BasisDebug.Log("Setting initial target to current offset:" + targetOffset + " : " + currentOffset);
                targetOffset = currentOffset;
            }
            
            if (mouseScroll != 0)
            {
                Transform sourceTransform = ConstraintRef.GetSource(0).sourceTransform;

                Vector3 movement = DesktopZoopSpeed * mouseScroll * BasisLocalCameraDriver.Instance.Camera.transform.forward;
                Vector3 newTargetOffset = targetOffset + sourceTransform.InverseTransformVector(movement);

                // moving towards camera, ignore moving closer if less than min distance
                // NOTE: this is cheating a bit since its assuming desktop camera is the constraint source, but its a lot faster than doing a bunch of world/local space transforms.
                //      This also does not set offset to min distance to avoid calculating min offset position, meaning this is effectively (distance > minDistance + ZoopSpeed).
                if (mouseScroll < 0 && newTargetOffset.z > DesktopZoopMinDistance)
                {
                    targetOffset = newTargetOffset;
                }
                // moving away from camera
                else if (mouseScroll > 0)
                {
                    targetOffset = newTargetOffset;
                }
            }                

            var dampendOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref currentZoopVelocity, k_DesktopZoopSmoothing, k_DesktopZoopMaxVelocity);
            ConstraintRef.SetTranslationOffset(0, dampendOffset);

            // BasisDebug.Log("Destop manipulate Pickup zoop: " + dampendOffset + " rotate: " + delta);                
        }
        else if (pauseHead)
        {
            targetOffset = Vector3.zero;
            pauseHead = false;
            if(!BasisAvatarEyeInput.Instance.UnPauseHead(nameof(PickupInteractable) + ": " + gameObject.name))
            {
                BasisDebug.LogWarning(nameof(PickupInteractable) + " was unable to un-pause head movement, this is a bug!");
            }
        }
        else
        {   
            // shouldn't need this here since pauseHead is used as a switch, but just in case...
            targetOffset = Vector3.zero;
        }
    }

    void OnDestroy()
    {
        Destroy(HighlightClone);
        if (asyncOperationHighlightMat.IsValid())
        {
            asyncOperationHighlightMat.Release();
        }
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        string errPrefix = "ReparentInteractable needs component defined on self or given a reference for ";
        if (RigidRef == null && !TryGetComponent(out Rigidbody _))
        {
            Debug.LogWarning(errPrefix + "Rigidbody", gameObject);
        }
        if (ColliderRef == null && !TryGetComponent(out Collider _))
        {
            Debug.LogWarning(errPrefix + "Collider", gameObject);
        }
        if (ConstraintRef == null && !TryGetComponent(out ParentConstraint _))
        {
            Debug.LogWarning(errPrefix + "ParentConstraint", gameObject);
        }
    }
#endif
}
