using System;
using System.Linq;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

// Needs Rigidbody for hover sphere `OnTriggerStay`
[System.Serializable]
public abstract class InteractableObject: MonoBehaviour 
{
    public InputSources Inputs = new(0);

    [Header("Interactable Settings")]

    [SerializeField]
    private bool disableInteract = false;
    // NOTE: unity editor will not use the set function so setting disabling Interact in play will not cleanup inputs
    public bool DisableInteract {
        get => disableInteract;
        set {
            // remove hover and interacting on disable
            if (value)
            {
                foreach (var input in Inputs.ToArray())
                {
                    if (input.Source != null)
                    {
                        if (IsHoveredBy(input.Source))
                        {
                            OnHoverEnd(input.Source, false);
                        }
                        if (IsInteractingWith(input.Source))
                        {
                            OnInteractEnd(input.Source);
                        }
                    }
                }
                OnInteractDisable?.Invoke();
            }
            else
            {
                OnInteractEnable?.Invoke();
            }
            disableInteract = value;
        }
    }
    public float InteractRange = 1.0f;
    [Space(5)]
    public bool CanEquip = false;
    public Vector3 equipPos;
    public Quaternion equipRot;
    [NonSerialized]
    public bool RequiresUpdateLoop;

    // Delegates for interaction events
    public Action<BasisInput> OnInteractStartEvent;
    public Action<BasisInput> OnInteractEndEvent;
    public Action<BasisInput> OnHoverStartEvent;
    public Action<BasisInput, bool> OnHoverEndEvent;
    public Action OnInteractEnable;
    public Action OnInteractDisable; 

    /// <summary>
    /// Check if object is within range based on its transform and Interact Range.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public virtual bool IsWithinRange(Vector3 source)
    {
        Collider collider = GetCollider();
        if (collider != null)
        {
            return Vector3.Distance(collider.ClosestPoint(source), source) <= InteractRange;
        }
        // Fall back to object transform distance
        return Vector3.Distance(transform.position, source) <= InteractRange;
    }

    /// <summary>
    /// Gets collider on self, override with cached get whenever possible.
    /// </summary>
    public virtual Collider GetCollider()
    {
        if (TryGetComponent(out Collider col))
        {
            return col;
        }
        return null;
    }

    public abstract bool CanHover(BasisInput input);
    public abstract bool IsHoveredBy(BasisInput input);

    public abstract bool CanInteract(BasisInput input);
    public abstract bool IsInteractingWith(BasisInput input);

    public virtual void OnInteractStart(BasisInput input)
    {
        OnInteractStartEvent?.Invoke(input);
    }

    public virtual void OnInteractEnd(BasisInput input)
    {
        OnInteractEndEvent?.Invoke(input);
    }

    public virtual void OnHoverStart(BasisInput input)
    {
        OnHoverStartEvent?.Invoke(input);
    }

    public virtual void OnHoverEnd(BasisInput input, bool willInteract)
    {
        OnHoverEndEvent?.Invoke(input, willInteract);
    }

    public abstract void InputUpdate();

    public struct BasisInputWrapper
    {
        public BasisInputWrapper(BasisInput source, bool isInteracting)
        {
            Source = source;
            IsInteracting = isInteracting;
        }

        public BasisInput Source { get; set; }

        /// <summary>
        /// - true: source interacting with object
        /// - false: source hovering
        /// If not either, this source should not be in the list!
        /// </summary>
        public bool IsInteracting { get; set; }
    }

    public struct InputSources {
        public BasisInputWrapper desktopCenterEye, leftHand, rightHand;
        public BasisInputWrapper[] extras;
        
        public InputSources(uint extrasCount)
        {
            desktopCenterEye = default;
            leftHand = default;
            rightHand = default;
            extras = new BasisInputWrapper[(int)extrasCount];
        }

        public readonly bool AnyInteracting(bool skipExtras = true)
        {
            bool interacting = leftHand.Source != null && leftHand.IsInteracting || 
                            rightHand.Source != null && rightHand.IsInteracting || 
                            desktopCenterEye.Source != null && desktopCenterEye.IsInteracting;
            if (!skipExtras)
            {
                interacting |= extras.Any(x => x.Source != null && x.IsInteracting);
            }
            return interacting;
        }

        public readonly BasisInputWrapper? Find(BasisInput input)
        {
            if (input == null)
                return null;
            string inUDI = input.UniqueDeviceIdentifier;
            var found = Array.Find(ToArray(), x => x.Source != null && x.Source.UniqueDeviceIdentifier == inUDI);
            // not found
            if (found.Source == null)
                return null;
            return found;
        }
        public readonly bool Contains(BasisInput input, bool skipExtras = true)
        {
            string inUDI = input != null ? input.UniqueDeviceIdentifier : "";

            bool contains = leftHand.Source != null && leftHand.Source.UniqueDeviceIdentifier == inUDI || 
                            rightHand.Source != null && rightHand.Source.UniqueDeviceIdentifier == inUDI || 
                            desktopCenterEye.Source != null && desktopCenterEye.Source.UniqueDeviceIdentifier == inUDI;

            if (!skipExtras)
            {
                contains |= extras.Any(x => x.Source != null && x.Source.UniqueDeviceIdentifier == inUDI);
            }
            return contains;
        }

        public readonly BasisInputWrapper[] ToArray()
        {
            BasisInputWrapper[] primary = new BasisInputWrapper[] {
                desktopCenterEye,
                leftHand,
                rightHand,
            };
            if (extras.Length != 0)
                return primary.Concat(extras).ToArray();
            return primary;
        }

        public bool AddInputByRole(BasisInput input, bool isInteracting)
        {
            if (input.TryGetRole(out BasisBoneTrackedRole role))
            {
                switch (role)
                {
                    case BasisBoneTrackedRole.CenterEye:
                        desktopCenterEye = new BasisInputWrapper(input, isInteracting);
                        return true;
                    case BasisBoneTrackedRole.LeftHand:
                        leftHand = new BasisInputWrapper(input, isInteracting);
                        return true;
                    case BasisBoneTrackedRole.RightHand:
                        rightHand = new BasisInputWrapper(input, isInteracting);
                        return true;
                    default:
                        return false;
                }
            }
            return false;

        }
        public readonly bool TryGetByRole(BasisBoneTrackedRole role, out BasisInputWrapper input)
        {
            input = default;
            switch (role)
            {
                case BasisBoneTrackedRole.CenterEye:
                    input = desktopCenterEye;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    input = leftHand;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    input = rightHand;
                    return true;
                default:
                    return false;
            }
        }

        public bool RemoveByRole(BasisBoneTrackedRole role)
        {
            switch (role)
            {
                case BasisBoneTrackedRole.CenterEye:
                    desktopCenterEye = default;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    leftHand = default;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    rightHand = default;
                    return true;
                default:
                    return false;
            }
        }
    }
}
