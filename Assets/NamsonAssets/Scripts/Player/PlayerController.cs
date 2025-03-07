using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Camera mainCamera;
    private PlayerInput playerInput;
    private PlayerInputAction playerInputAction;

    private bool isDragging = false;

    private bool IsMouseKeyboardScheme
    {
        get
        {
            return playerInput.currentControlScheme == "MouseKeyboard";
        }
    }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        playerInputAction = GetComponent<PlayerInputAction>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if(IsMouseKeyboardScheme)
        {
            // handle drag
            if (playerInputAction.leftClick && !isDragging)
            {
                StartDrag();
            }
            else if (playerInputAction.leftClick && isDragging)
            {
                HoldDrag();
            }
            else if (!playerInputAction.leftClick && isDragging)
            {
                CancelDrag();
            }

            // handle rotate
            if (playerInputAction.leftClick && playerInputAction.rightClick && isDragging)
            {
                RotateDrag();
            }
        }
    }

    private void StartDrag()
    {
        Debug.Log($"Start drag");
        isDragging = true;
    }

    private void HoldDrag()
    {
        Debug.Log($"Hold drag");
    }

    private void RotateDrag()
    {
        Debug.Log($"Rotate drag");
        playerInputAction.rightClick = false;
    }

    private void CancelDrag()
    {
        Debug.Log($"Cancel drag");
        isDragging = false;
    }
}
