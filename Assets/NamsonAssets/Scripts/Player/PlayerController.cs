using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;

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
    [SerializeField] private int originX = -1;
    [SerializeField] private int originY = -1;

    private List<RaycastResult> hitObjects = new List<RaycastResult>();

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
        if (IsMouseKeyboardScheme)
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
        if (GetRaycastResults().Count > 0)
        {
            foreach (RaycastResult raycastResult in hitObjects)
            {
                if (raycastResult.gameObject.TryGetComponent(out UISpawnItemSlot spawnSlot))
                {
                    selectedSpawnSlot = spawnSlot;
                    selectedDraggable = spawnSlot.SpawnDraggableItem();
                    selectedDraggable.transform.position = spawnSlot.transform.position;
                    originPosition = spawnSlot.transform.position;

                    inventory.CheckWeight(selectedDraggable.GetItemData());

                    isDragging = true;
                    Cursor.visible = false;
                }

                if (raycastResult.gameObject.TryGetComponent(out UIInventorySlot inventorySlot))
                {
                    if (inventory.CheckRepeatedSlot(inventorySlot.X, inventorySlot.Y))
                        return;

                    if (!inventorySlot.HasItem())
                        return;

                    Debug.Log($"Check slot [{inventorySlot.X}, {inventorySlot.Y}] | Item: {inventorySlot.GetItem().GetItemData().itemName}");

                    originPosition = inventorySlot.transform.position;
                    originX = inventorySlot.X;
                    originY = inventorySlot.Y;
                    selectedDraggable = inventorySlot.GetItem();
                    selectedInventorySlot = inventorySlot;

                    inventory.RemoveItem(selectedDraggable);

                    isDragging = true;
                    Cursor.visible = false;
                }
            }
        }

    }

    private void HoldDrag()
    {
        Debug.Log($"Hold drag: {selectedDraggable == null}");
        if (selectedDraggable != null)
        {
            selectedDraggable.transform.position = GetGridPosition(playerInputAction.point, selectedDraggable.GetItemData(), selectedDraggable.RotationStapes);

            if (GetRaycastResults().Where(r => r.gameObject.layer == 6).Count() > 0)
            {
                foreach (RaycastResult raycastResult in hitObjects)
                {
                    if (raycastResult.gameObject.TryGetComponent(out UIInventorySlot inventorySlot))
                    {
                        if (inventory.CheckRepeatedSlot(inventorySlot.X, inventorySlot.Y))
                            return;

                        Debug.Log($"Check slot [{inventorySlot.X}, {inventorySlot.Y}]");
                        DraggableItem item = selectedDraggable;
                        inventory.CanPlaceItem(item.GetItemData(), inventorySlot.X, inventorySlot.Y, item.GetShape());

                        selectedInventorySlot = inventorySlot;
                    }
                }
            }
            else if (selectedInventorySlot != null)
            {
                selectedInventorySlot = null;
                inventory.UpdateAllInventorySlotHighlight();
                inventory.ResetRepeatedSlotCheck();
            }
        }
    }

    private void RotateDrag()
    {
        Debug.Log($"Rotate drag");
        if (selectedDraggable != null)
        {
            selectedDraggable.Rotate();
            inventory.ResetRepeatedSlotCheck();
        }
        playerInputAction.rightClick = false;
    }

    

    private void CancelDrag()
    {
        Debug.Log($"Cancel drag");
        // Drag from inventory slot
        if (selectedDraggable != null)
        {
            // Destroy item in drop zone
            if (GetRaycastResults().Count > 0)
            {
                foreach (RaycastResult raycastResult in hitObjects)
                {
                    if (raycastResult.gameObject.TryGetComponent(out UIDropZone dropZone))
                    {
                        dropZone.DropItem(selectedDraggable);
                        originX = -1;
                        originY = -1;
                    }
                }
            }

            // Place item in inventory
            if (selectedInventorySlot != null)
            {
                if (inventory.PlaceItem(selectedDraggable, selectedInventorySlot.X, selectedInventorySlot.Y))
                {
                    ItemData item = selectedDraggable.GetItemData();
                    selectedDraggable.transform.position = GetGridPosition(selectedInventorySlot.transform.position, item, selectedDraggable.RotationStapes);
                }
                else
                {
                    Destroy(selectedDraggable.gameObject);
                }
            }
            else if(originX >= 0 && originY >= 0)
            {
                inventory.PlaceItem(selectedDraggable, originX, originY);
                ItemData item = selectedDraggable.GetItemData();
                selectedDraggable.transform.position = GetGridPosition(originPosition, item, selectedDraggable.RotationStapes);
            }
            else
            {
                Destroy(selectedDraggable.gameObject);
            }

            if (selectedSpawnSlot != null)
            {
                selectedSpawnSlot.ClearSpawnSlot();
                selectedSpawnSlot = null;
            }

            // Reset inventory grid
            inventory.UpdateAllInventorySlotHighlight();
            inventory.ResetRepeatedSlotCheck();

            // Reset player reference
            selectedInventorySlot = null;
            selectedDraggable = null;
            originPosition = Vector3.zero;
            originX = -1;
            originY = -1;
        }

        isDragging = false;
        Cursor.visible = true;
    }
    #region Helper

    private List<RaycastResult> GetRaycastResults()
    {
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = playerInputAction.point;
        EventSystem.current.RaycastAll(pointer, hitObjects);

        return hitObjects;
    }

    private float gridSize = 128f;
    private Vector3 GetGridPosition(Vector3 position, ItemData item, int rotate)
    {
        Vector3 newPosition = position;

        float customX = gridSize * item.offsetX;
        float customY = gridSize * item.offsetY;

        if (item.shapeType == ShapeType.LShape)
        {
            switch (rotate)
            {
                case 1:
                    customX = -customX;
                    break;
                case 2:
                    customX = 0;
                    customY = 0;
                    break;
                case 3:
                    customY = 0;
                    break;
            }
        }
        else if(item.shapeType == ShapeType.Rectangle)
        {
            switch (rotate)
            {
                case 1:
                case 3:
                    customX = 0;
                    customY = 0;
                    break;
            }
        }

        float offsetX = (item.width - 1) * (gridSize * 0.5f) + customX;
        float offsetY = (item.height - 1) * (gridSize * 0.5f) + customY;

        newPosition.x += offsetX;
        newPosition.y -= offsetY;
        newPosition.z = 0;

        return newPosition;
    }

    #endregion
}
