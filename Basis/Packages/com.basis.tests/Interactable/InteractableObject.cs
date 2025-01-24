using Basis.Scripts.Device_Management.Devices;
using System;
using UnityEngine;

// Needs Rigidbody for hover sphere `OnTriggerStay`
[System.Serializable]
public abstract class InteractableObject : MonoBehaviour
{
    public BasisInputWrapper InputSource;

    [Header("Interactable Settings")]
    public float InteractRange = 1.0f;
    public bool CanEquip = false;
    public Vector3 equipPos;
    public Quaternion equipRot;
    public bool RequiresUpdateLoop;

    // Delegates for interaction events
    public delegate void InteractionEventHandler(BasisInput input);
    public event InteractionEventHandler OnInteractStartEvent;
    public event InteractionEventHandler OnInteractEndEvent;
    public event InteractionEventHandler OnHoverStartEvent;
    public event InteractionEventHandler OnHoverEndEvent;
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
        OnHoverEndEvent?.Invoke(input);
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
}
