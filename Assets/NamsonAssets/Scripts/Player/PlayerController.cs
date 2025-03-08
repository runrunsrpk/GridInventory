using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UIInventory inventory;

    private Camera mainCamera;
    private PlayerInput playerInput;
    private PlayerInputAction playerInputAction;

    private bool isDragging = false;

    [Header("Debug")]
    [SerializeField] private Vector3 originPosition;
    [SerializeField] private DraggableItem selectedDraggable;
    [SerializeField] private UISpawnItemSlot selectedSpawnSlot;
    [SerializeField] private UIInventorySlot selectedInventorySlot;

    private List<RaycastResult> hitObjects = new List<RaycastResult> ();

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
        if(GetRaycastResults().Count > 0)
        {
            foreach(RaycastResult raycastResult in hitObjects)
            {
                if(raycastResult.gameObject.TryGetComponent(out UISpawnItemSlot slot))
                {
                    selectedSpawnSlot = slot;
                    selectedDraggable = slot.SpawnDraggableItem();
                    selectedDraggable.transform.position = slot.transform.position;
                    originPosition = slot.transform.position;

                    inventory.CheckWeight(selectedDraggable.GetItemData());

                    isDragging = true;
                }
            }
        }

    }

    private void HoldDrag()
    {
        Debug.Log($"Hold drag");
        if (selectedDraggable != null)
        {
            Vector3 newPosition = playerInputAction.point;
            newPosition.z = 0;
            selectedDraggable.transform.position = newPosition;

            if (GetRaycastResults().Count > 0)
            {
                foreach (RaycastResult raycastResult in hitObjects)
                {
                    if (raycastResult.gameObject.TryGetComponent(out UIInventorySlot slot))
                    {
                        if (inventory.CheckRepeatedSlot(slot.X, slot.Y))
                            return;

                        Debug.Log($"Check slot [{slot.X}, {slot.Y}]");
                        DraggableItem item = selectedDraggable;
                        inventory.CanPlaceItem(item.GetItemData(), slot.X, slot.Y, item.GetShape());
                    }

                }
            }
        }
    }

    private void RotateDrag()
    {
        Debug.Log($"Rotate drag");
        if (selectedDraggable != null)
        {
            selectedDraggable.Rotate();
        }
        playerInputAction.rightClick = false;
    }

    private void CancelDrag()
    {
        Debug.Log($"Cancel drag");
        if (selectedDraggable != null && selectedSpawnSlot != null)
        {
            inventory.ClearInventorySlotHighlight();
            selectedSpawnSlot.ClearSpawnSlot();
            // TODO: smooth return to origin position
            selectedDraggable.transform.position = originPosition;
            Destroy(selectedDraggable.gameObject);

            selectedSpawnSlot = null;
            selectedDraggable = null;
            originPosition = Vector3.zero;
            isDragging = false;
        }
    }

    private List<RaycastResult> GetRaycastResults()
    {
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = playerInputAction.point;
        EventSystem.current.RaycastAll(pointer, hitObjects);

        return hitObjects;
    }

    private Vector3 GetMousePosition()
    {
        return mainCamera.ScreenToWorldPoint(playerInputAction.point);
    }
}
