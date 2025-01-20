using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[System.Serializable]
public class HoverInteractSphere : MonoBehaviour
{

    public InteractableObject HoverTarget;
    public float TargetDistance = float.PositiveInfinity;

    public Vector3 TargetClosestPoint = Vector3.zero;

    private SphereCollider sphereColliderRef;

    void Start()
    {
        gameObject.TryGetComponent(out sphereColliderRef);
        ResetTarget();
    }

    // trigger stay does our updating so we dont need to manage a list of everything in bounds
    private void OnTriggerStay(Collider other)
    {
        if (sphereColliderRef != null && other.TryGetComponent(out InteractableObject otherInteractable))
        {
            Vector3 sourcePosition = transform.position;
            Vector3 otherClosestPoint = other.ClosestPoint(sourcePosition);
            float otherDistance = Vector3.Distance(otherClosestPoint, sourcePosition);

            if (otherDistance < TargetDistance)
            {
                HoverTarget = otherInteractable;
                TargetDistance = otherDistance;
                TargetClosestPoint = otherClosestPoint;
            }
            // update target distance and closest point
            else if (otherInteractable.GetInstanceID() == HoverTarget.GetInstanceID() && otherClosestPoint != TargetClosestPoint)
            {
                TargetDistance = otherDistance;
                TargetClosestPoint = otherClosestPoint;
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (HoverTarget == null)
            return;
        // current target left
        if (other.TryGetComponent(out InteractableObject otherInteractable) && HoverTarget.GetInstanceID() == otherInteractable.GetInstanceID())
        {
            ResetTarget();
        }
    }

    private void ResetTarget()
    {
        HoverTarget = null;
        TargetClosestPoint = Vector3.zero;
        TargetDistance = float.PositiveInfinity;
    }
}
