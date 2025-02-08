using System;
using Basis.Scripts.BasisSdk.Players;
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
    [Space(10)]
    public bool Equippable = false;
    [Tooltip("Provide an offset for interactable to use, e.g holding position")]
    public Vector3 StrictPosition;
    public Quaternion StrictRotation;

    [NonSerialized]
    public bool RequiresUpdateLoop = false;
    /// <summary>
    /// 1. to block interaction when puppeted.
    /// 2. (example) iskinematic set
    /// depending on puppeted state.
    /// </summary>
    // [HideInInspector]
    public bool IsPuppeted = false;
    // Delegates for interaction events
    public Action<BasisInput> OnInteractStartEvent;
    public Action<BasisInput> OnInteractEndEvent;
    public Action<BasisInput> OnHoverStartEvent;
    public Action<BasisInput, bool> OnHoverEndEvent;
    public Action OnInteractEnable;
    public Action OnInteractDisable; 

    // Having Start/OnDestroy as virtuals is icky but I cant think of a more elegant way of doing this.
    // We already recommend calling the base method for Interact/Hover Start/End, so hopefully it wont be too big an issue.
    public virtual void Awake()
    {
        if (BasisLocalPlayer.PlayerReady)
            SetupInputs();
        else
            BasisLocalPlayer.OnLocalPlayerCreatedAndReady += SetupInputs;
    }

    private void SetupInputs()
    {
        var Devices = Basis.Scripts.Device_Management.BasisDeviceManagement.Instance.AllInputDevices;
        Devices.OnListAdded += OnInputAdded;
        Devices.OnListItemRemoved += OnInputRemoved;        
        foreach (BasisInput device in Devices)
        {
            OnInputAdded(device);
        }
    }

    public virtual void OnDestroy()
    {
        var Devices = Basis.Scripts.Device_Management.BasisDeviceManagement.Instance.AllInputDevices;
        Devices.OnListAdded -= OnInputAdded;
        Devices.OnListItemRemoved -= OnInputRemoved;
    }

    private void OnInputAdded(BasisInput input)
    {
        // dont expect to add non-role inputs
        // NOTE: when extra (non role) inputs are needed we are going to need to rewrite this
        if(!input.TryGetRole(out Basis.Scripts.TransformBinders.BoneControl.BasisBoneTrackedRole r))
            return;

        
        if(!Inputs.SetInputByRole(input, InteractInputState.Ignored))
            BasisDebug.LogError("New input added not setup as expected by InteractableObject");
        else
        {
            // BasisDebug.Log($"{gameObject.name}: Input added. {Inputs.TryGetByRole(r, out BasisInputWrapper w)}, {w.Source.gameObject.name}, {w.GetState()}", input.gameObject);
        }
    }

    private void OnInputRemoved(BasisInput input)
    {        
        if (input.TryGetRole(out Basis.Scripts.TransformBinders.BoneControl.BasisBoneTrackedRole role))
            if (!Inputs.RemoveByRole(role))
                BasisDebug.LogError("Something went wrong while removing input");
    }

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
