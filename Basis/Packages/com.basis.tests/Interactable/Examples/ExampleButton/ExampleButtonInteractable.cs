using System;
using Basis.Scripts.Device_Management.Devices;
using UnityEngine;
using UnityEngine.Events;

public class ExampleButtonInteractable : InteractableObject
{
    // public BasisObjectSyncNetworking syncNetworking;

    // events other scripts can subscribe to
    public UnityEvent ButtonDown;
    public UnityEvent ButtonUp;

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

    private BasisInputWrapper _inputSource;
    // Ignore provided list localy, but keep it updated for other scripts
    private BasisInputWrapper _InputSource {
        get => _inputSource;
        set {
            if (value.Source != null)
            {
                Inputs = new(0);
                Inputs.AddInputByRole(value.Source, value.IsInteracting);
            }
            else if (value.Source == null)
            {
                Inputs = new(0);
            }
            _inputSource = value;

        }
    }

    void Start()
    {
        _InputSource = new BasisInputWrapper(null, false);
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
        return _InputSource.Source == null && IsWithinRange(input.transform.position) && isEnabled;
    }
    public override bool CanInteract(BasisInput input)
    {
        // must be the same input hovering
        if (!IsCurrentInput(input.UniqueDeviceIdentifier)) return false;
        // dont interact again till after interacting stopped
        if (_InputSource.IsInteracting) return false;

        return IsWithinRange(input.transform.position) && isEnabled;
    }

    public override void OnHoverStart(BasisInput input)
    {
        _InputSource = new BasisInputWrapper(input, false);
        SetColor(HoverColor);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier))
        {
            // leaving hover and wont interact this frame
            if (!willInteract)
            {
                _InputSource = new BasisInputWrapper(null, false);
                SetColor(Color);
            }
            // Oninteract will update color
        }
    }

    public override void OnInteractStart(BasisInput input)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier) && !_InputSource.IsInteracting)
        {
            // Set ownership to the local player
            // syncNetworking.IsOwner = true;
            SetColor(InteractColor);
            _InputSource = new BasisInputWrapper(input, true);
            ButtonDown?.Invoke();
        }
    }

    public override void OnInteractEnd(BasisInput input)
    {
        if (IsCurrentInput(input.UniqueDeviceIdentifier) && _InputSource.IsInteracting)
        {
            SetColor(Color);
            _InputSource = new BasisInputWrapper(null, false);
            ButtonUp?.Invoke();
        }
    }
    public override bool IsInteractingWith(BasisInput input)
    {
        return _InputSource.Source != null &&
            _InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier &&
            _InputSource.IsInteracting;
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        return _InputSource.Source != null &&
            _InputSource.Source.UniqueDeviceIdentifier == input.UniqueDeviceIdentifier &&
            !_InputSource.IsInteracting;
    }

    private bool IsCurrentInput(string uid)
    {
        return _InputSource.Source != null && _InputSource.Source.UniqueDeviceIdentifier == uid;
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
            if (_InputSource.Source != null)
            {
                if (IsHoveredBy(_InputSource.Source))
                {
                    OnHoverEnd(_InputSource.Source, false);
                }
                if (IsInteractingWith(_InputSource.Source))
                {
                    OnInteractEnd(_InputSource.Source);
                }
            }
            // setting same color every frame isnt optimal but fine for example
            SetColor(DisabledColor);
        }
    }
}
