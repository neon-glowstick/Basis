using System;
using Basis.Scripts.Device_Management.Devices;
using UnityEngine;

// Needs Rigidbody for hover sphere `OnTriggerStay`
[Serializable]
public abstract partial class InteractableObject: MonoBehaviour 
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
                Clear();
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
    /// <summary>
    /// 1. to block interaction when puppeted.
    /// 2. (example) iskinematic set
    /// depending on puppeted state.
    /// </summary>
    [HideInInspector]
    public bool IsPuppeted = false;
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

    /// <summary>
    /// clear is the generic,
    /// a ungeneric would be drop
    /// </summary>
    public virtual void Clear()
    {
        BasisInputWrapper[] InputArray = Inputs.ToArray();
        int count = InputArray.Length;
        for (int i = 0; i < count; i++)
        {
            BasisInputWrapper input = InputArray[i];
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
    }
    public virtual void StartRemoteControl()
    {
        IsPuppeted = true;
    }
    public virtual void StopRemoteControl()
    {
        IsPuppeted = false;
    }
}
