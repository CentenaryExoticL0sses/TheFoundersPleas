using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputProvider : MonoBehaviour, InputActions.IGameplayActions
{
    public event Action<Vector2> Moved = delegate { };
    public event Action<Vector2> PointerMoved = delegate { };
    public event Action<float> Rotated = delegate { };
    public event Action<float> Zoomed = delegate { };

    public event Action InteractPerformed = delegate { };
    public event Action InteractCancelled = delegate { };
    public event Action SecondActionPerformed = delegate { };
    public event Action SecondActionCancelled = delegate { };
    public event Action ToggleDragPerformed = delegate { };
    public event Action ToggleDragCancelled = delegate { };
    public event Action ToggleRotationPerformed = delegate { };
    public event Action ToggleRotationCancelled = delegate { };

    private InputActions _playerInput;

    private void Awake()
    {
        if (_playerInput == null)
        {
            _playerInput = new InputActions();
            _playerInput.Gameplay.SetCallbacks(this);
        }
        _playerInput.Gameplay.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Moved.Invoke(context.ReadValue<Vector2>());
    }

    public void OnPointerPosition(InputAction.CallbackContext context)
    {
        PointerMoved.Invoke(context.ReadValue<Vector2>());
    }

    public void OnRotateCamera(InputAction.CallbackContext context)
    {
        Rotated.Invoke(context.ReadValue<float>());
    }

    public void OnZoomCamera(InputAction.CallbackContext context)
    {
        Zoomed.Invoke(context.ReadValue<float>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                InteractPerformed.Invoke();
                break;
            case InputActionPhase.Canceled:
                InteractCancelled.Invoke();
                break;
        }
    }

    public void OnSecondAction(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                SecondActionPerformed.Invoke();
                break;
            case InputActionPhase.Canceled:
                SecondActionCancelled.Invoke();
                break;
        }
    }

    public void OnToggleDrag(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                ToggleDragPerformed.Invoke();
                break;
            case InputActionPhase.Canceled:
                ToggleDragCancelled.Invoke();
                break;
        }
    }

    public void OnToggleRotation(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                ToggleRotationPerformed.Invoke();
                break;
            case InputActionPhase.Canceled:
                ToggleRotationCancelled.Invoke();
                break;
        }
    }
}
