using System;
using Basis.Scripts.Device_Management.Devices;
using UnityEngine;
public class ExampleButtonInteractable : InteractableObject
{
    // public BasisObjectSyncNetworking syncNetworking;

    // events other scripts can subscribe to
    public Action ButtonDown;
    public Action ButtonUp;

    [Header("Button Settings")]
    public bool isEnabled;
    [Space(10)]
    public string PropertyName = "_Color";
    public Color Color = Color.white;
    public Color HoverColor = Color.white;
    public Color InteractColor = Color.white;
    public Color DisabledColor = Color.white;

    [Header("References")]
    public Collider ColliderRef;
    public MeshRenderer RendererRef;

    void Start()
    {
        InputSource = new BasisInputWrapper(null, false);
        if (ColliderRef == null)
        {
            TryGetComponent(out ColliderRef);
        }
        if (RendererRef == null)
        {
            TryGetComponent(out RendererRef);
        }

        SetColor(isEnabled ? Color : DisabledColor);
    }

    public override bool CanHover(BasisInput input)
    {
        return InputSource.Source == null && IsWithinRange(input.transform.position) && isEnabled;
    }
    public override bool CanInteract(BasisInput input)
    {
        // must be the same input hovering
        if (!IsCurrentInput(input.UniqueDeviceIdentifier)) return false;
        // dont interact again till after interacting stopped
        if (InputSource.IsInteracting) return false;

        return IsWithinRange(input.transform.position) && isEnabled;
    }

    public override void OnHoverStart(BasisInput input)
    {
        InputSource = new BasisInputWrapper(input, false);
        SetColor(HoverColor);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier))
        {
            // leaving hover and wont interact this frame
            if (!willInteract)
            {
                InputSource = new BasisInputWrapper(null, false);
                SetColor(Color);
            }
            // Oninteract will update color
        }
    }

    public override void OnInteractStart(BasisInput input)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier) && !InputSource.IsInteracting)
        {
            // Set ownership to the local player
            // syncNetworking.IsOwner = true;
            SetColor(InteractColor);
            InputSource = new BasisInputWrapper(input, true);
            ButtonDown?.Invoke();
        }
    }

    public override void OnInteractEnd(BasisInput input)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier) && InputSource.IsInteracting)
        {
            SetColor(Color);
            InputSource = new BasisInputWrapper(null, false);
            ButtonUp?.Invoke();
        }
    }
    public override bool IsInteractingWith(BasisInput input)
    {
        return InputSource.Source != null &&
            InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier &&
            InputSource.IsInteracting;
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        return InputSource.Source != null &&
            InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier &&
            !InputSource.IsInteracting;
    }

    private bool IsCurrentInput(string uid)
    {
        return InputSource.Source != null && InputSource.Source.UniqueDeviceIdentifier == uid;
    }

    // set material property to a color
    private void SetColor(Color color)
    {
        if (RendererRef != null && RendererRef.material != null)
        {
            RendererRef.material.SetColor(Shader.PropertyToID(PropertyName), color);
        }
    }


    // per-frame update, after IK transform
    public override void InputUpdate()
    {
        if (!isEnabled)
        {
            // clean up currently hovering/interacting
            if (InputSource.Source != null)
            {
                if (IsHoveredBy(InputSource.Source))
                {
                    OnHoverEnd(InputSource.Source, false);
                }
                if (IsInteractingWith(InputSource.Source))
                {
                    OnInteractEnd(InputSource.Source);
                }
            }
            // setting same color every frame isnt optimal but fine for example
            SetColor(DisabledColor);
        }
    }
}
