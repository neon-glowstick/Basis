using System.Linq;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.Device_Management.Devices.Desktop;
using Basis.Scripts.Drivers;
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

    [Header("References")]
    public Collider ColliderRef;
    public Rigidbody RigidRef;

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
    public ParentConstraint ConstraintRef;
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
            if (TryGetComponent(out ConstraintRef))
            {
            }
            else
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
    public void HighlightObject(bool highlight)
    {
        if (ColliderRef && HighlightClone)
        {
            HighlightClone.SetActive(highlight);
        }
    }

    private int InputIndex(BasisInput input)
    {
        return InputSources.FindIndex(x => x.Source != null && x.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier);
    }

    public override bool CanHover(BasisInput input)
    {
        return InputSources.All(x => !x.IsInteracting) && InputIndex(input) == -1 && IsWithinRange(input.transform.position);
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

    public override bool CanInteract(BasisInput input)
    {
        // currently hovering with this input
        return InputSources.All(x => !x.IsInteracting) &&
            InputIndex(input) != -1 &&
            IsWithinRange(input.transform.position);
    }


    public override void OnHoverStart(BasisInput input)
    {
        int i = InputIndex(input);
        if (i == -1)
        {
            InputSources.Add(new BasisInputWrapper(input, false));
        }
        else 
        {
            InputSources[i] = new BasisInputWrapper(input, false);
            BasisDebug.LogWarning("Pickup Interactable found input source in list OnHover, this shouldn't happen");
        }
        HighlightObject(true);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        int i = InputIndex(input);
        if (i != -1)
        {
            if (!willInteract)
            {
                InputSources.RemoveAt(i);
            }
            HighlightObject(false);
        }
    }
    public override void OnInteractStart(BasisInput input)
    {
        int i = InputIndex(input);
        if (i == -1)
            return;
        var wrapper = InputSources[i];
        // same input that was highlighting previously
        if (wrapper.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier && !wrapper.IsInteracting)
        {

            if (RigidRef != null && KinematicWhileInteracting)
            {
                _previousKinematicValue = RigidRef.isKinematic;
                RigidRef.isKinematic = true;
            }

            // Set ownership to the local player
            // syncNetworking.IsOwner = true;
            InputSources[i] = new BasisInputWrapper(input, true);
            RequiresUpdateLoop = true;
            //  this.transform.parent = BasisLocalPlayer.Instance.transform;
            SetParentConstraint(input.transform);
        }
        else
        {
            Debug.LogWarning("Input source interacted with ReparentInteractable without highlighting first.");
        }
    }

    public override void OnInteractEnd(BasisInput input)
    {
        int i = InputIndex(input);
        if (i == -1)
            return;
        var wrapper = InputSources[i];

        if (wrapper.IsInteracting && wrapper.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier)
        {

            InputSources.RemoveAt(i);

            if (KinematicWhileInteracting && RigidRef != null)
            {
                RigidRef.isKinematic = _previousKinematicValue;
            }

            RequiresUpdateLoop = false;
            // cleanup Desktop Manipulation since InputUpdate isnt run again till next pickup
            if (lockLook)
            {
                BasisAvatarEyeInput.Instance.PauseLook = false;
                targetOffset = Vector3.zero;
                currentZoopVelocity = Vector3.zero;
                lockLook = false;
            }
            // syncNetworking.IsOwner = false;
            // this.transform.parent = null;
            SetParentConstraint(null);
        }
    }
    public override void InputUpdate()
    {
        var interactingInputIndex = InputSources.FindIndex(x => x.IsInteracting && x.Source != null);
        if (interactingInputIndex != -1)
        {

            if (IsDesktopCenterEye(InputSources[interactingInputIndex].Source))
            {
                PollDesktopManipulation();
            }
        }
    }


    public override bool IsInteractingWith(BasisInput input)
    {
        return InputSources.Any(x => 
            x.IsInteracting && 
            x.Source != null && 
            x.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier
        );
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        return InputSources.Any(x => 
            !x.IsInteracting && 
            x.Source != null && 
            x.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier
        );
    }

    // this is cached, use it
    public override Collider GetCollider()
    {
        return ColliderRef;
    }

    private bool IsDesktopCenterEye(BasisInput input)
    {
        return input.TryGetRole(out Basis.Scripts.TransformBinders.BoneControl.BasisBoneTrackedRole role) && role == Basis.Scripts.TransformBinders.BoneControl.BasisBoneTrackedRole.CenterEye;
    }

    private bool lockLook = false;
    private Vector3 targetOffset = Vector3.zero;
    private Vector3 currentZoopVelocity = Vector3.zero;
    private void PollDesktopManipulation()
    {
        if (Mouse.current.middleButton.isPressed)
        {
            lockLook = true;
            BasisAvatarEyeInput.Instance.PauseLook = true;

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
                targetOffset = currentOffset;
            
            if (mouseScroll != 0)
            {
                Transform sourceTransform = ConstraintRef.GetSource(0).sourceTransform;

                Vector3 movement = DesktopZoopSpeed * mouseScroll * BasisLocalCameraDriver.Forward();
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
        else if (lockLook)
        {
            BasisAvatarEyeInput.Instance.PauseLook = false;
            lockLook = false;
        }
    }

    // TODO: netsync
    // public void OnOwnershipTransfer(bool isOwner)
    // {
    //     // remove ourselves from influece
    //     if (!isOwner)
    //     {
    //         transform.SetParent(null);
    //         InputSources[0] = new InputSource(null, true);
    //     }
    //     // dont care otherwise, wait for hover/interact
    // }

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
