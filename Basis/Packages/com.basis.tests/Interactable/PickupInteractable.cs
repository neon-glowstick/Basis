using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management.Devices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.GraphicsBuffer;

public class PickupInteractable : InteractableObject
{
    // public BasisObjectSyncNetworking syncNetworking;
    [Header("Reparent Settings")]
    public bool KinematicWhileInteracting = false;

    [SerializeField]
    private bool LocalOnly = true;

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
    public ParentConstraint ConstraintRef;
    public void Start()
    {
        InputSource = new BasisInputWrapper(null, false);

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
                ConstraintRef = this.gameObject.AddComponent<ParentConstraint>();
            }
            var nullSource = new ConstraintSource()
            {
                sourceTransform = null,
                weight = 1,
            };
            ConstraintRef.AddSource(nullSource);
        }
        // TODO: netsync
        if (!LocalOnly)
        {
            // syncNetworking = GetComponent<BasisObjectSyncNetworking>();
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

    public override bool CanHover(BasisInput input)
    {
        // must be dropped to hover
        return InputSource.Source == null && IsWithinRange(input.transform.position);
    }
    public override bool CanInteract(BasisInput input)
    {
        // currently hovering with this input
        return InputSource.Source != null &&
            !InputSource.IsInteracting &&
            InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier &&
            IsWithinRange(input.transform.position);
    }

    public override void OnHoverStart(BasisInput input)
    {
        InputSource = new BasisInputWrapper(input, false);
        HighlightObject(true);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        if (InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier)
        {
            if (!willInteract)
            {
                InputSource = new BasisInputWrapper(null, false);
            }
            HighlightObject(false);
        }
    }
    public override void OnInteractStart(BasisInput input)
    {
        // same input that was highlighting previously
        if (InputSource.Source != null && InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier && !InputSource.IsInteracting)
        {

            if (RigidRef != null && KinematicWhileInteracting)
            {
                _previousKinematicValue = RigidRef.isKinematic;
                RigidRef.isKinematic = true;
            }

            // Set ownership to the local player
            // syncNetworking.IsOwner = true;
            InputSource = new BasisInputWrapper(input, true);
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
        if (InputSource.IsInteracting && InputSource.Source != null && InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier)
        {

            InputSource = new BasisInputWrapper(null, false);

            if (KinematicWhileInteracting && RigidRef != null)
            {
                RigidRef.isKinematic = _previousKinematicValue;
            }

            RequiresUpdateLoop = false;
            // syncNetworking.IsOwner = false;
            // this.transform.parent = null;
            SetParentConstraint(null);
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
    public override void InputUpdate()
    {
        if (InputSource.IsInteracting && InputSource.Source != null)
        {
            // Optionally, match the rotation.
            //  transform.rotation = target.rotation;
            //     this.transform.SetLocalPositionAndRotation(vector3 + PositionOffset, appliedRotation);

            // Update the networked data (Storeddata) to reflect the position, rotation, and scale
            if (!LocalOnly)
            {
                // syncNetworking.Storeddata.Position = transform.position;
                // syncNetworking.Storeddata.Rotation = transform.rotation;
                // syncNetworking.Storeddata.Scale = transform.localScale;
            }
        }
    }


    public override bool IsInteractingWith(BasisInput input)
    {
        return InputSource.IsInteracting &&
            InputSource.Source != null &&
            InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier;
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        return !InputSource.IsInteracting &&
            InputSource.Source != null &&
            InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier;
    }

    // this is cached, use it
    public override Collider GetCollider()
    {
        return ColliderRef;
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
