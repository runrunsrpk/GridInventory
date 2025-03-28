using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UIInventory inventory;

    private Camera mainCamera;
    private PlayerInput playerInput;
    private PlayerInputAction playerInputAction;
    private List<RaycastResult> hitObjects = new List<RaycastResult>();
    private float gridSize = 128f;

    // Item drag state
    private bool isDragging = false;
    private Vector3 originPosition;
    private DraggableItem selectedDraggable;
    private UISpawnItemSlot selectedSpawnSlot;
    private UIInventorySlot selectedInventorySlot;
    private int originX = -1;
    private int originY = -1;
    private bool isWaitingForRotation = false;
    private float rotationDelay = 0.2f;

    // Control scheme property for better readability
    private bool IsMouseKeyboardScheme => playerInput.currentControlScheme == "MouseKeyboard";

    private void Awake()
    {
        mainCamera = mainCamera ?? Camera.main;
    }

    private void Start()
    {
        playerInputAction = GetComponent<PlayerInputAction>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (!IsMouseKeyboardScheme) return;

        HandleDragging();
        HandleRotation();
        HandleExitGame();
    }

    // Process drag operations based on input state
    private void HandleDragging()
    {
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
    }

    // Handle item rotation during drag
    private void HandleRotation()
    {
        // Only allow rotation if explicitly right-clicked during drag
        if (isDragging && playerInputAction.rightClick)
        {
            // Add a small delay to prevent accidental rotation
            if (!isWaitingForRotation)
            {
                isWaitingForRotation = true;
                StartCoroutine(RotateWithDelay());
            }
        }
    }

    private IEnumerator RotateWithDelay()
    {
        // Wait for a short delay to confirm user really wants to rotate
        yield return new WaitForSeconds(rotationDelay);

        // Check if right click is still active
        if (playerInputAction.rightClick && isDragging)
        {
            // Perform rotation
            if (selectedDraggable != null)
            {
                selectedDraggable.Rotate();
                inventory.ResetRepeatedSlotCheck();
            }
        }

        // Reset flag and input state
        isWaitingForRotation = false;
        playerInputAction.rightClick = false;
    }

    private void HandleExitGame()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Debug.Log($"Exit Game");
            Application.Quit();
        }
    }

    // Start dragging an item from spawn slot or inventory
    private void StartDrag()
    {
        if (GetRaycastResults().Count <= 0) return;

        foreach (RaycastResult result in hitObjects)
        {
            if (TryDragFromSpawnSlot(result) || TryDragFromInventory(result))
            {
                isDragging = true;
                Cursor.visible = false;
                break;
            }
        }
    }

    // Try to drag an item from a spawn slot
    private bool TryDragFromSpawnSlot(RaycastResult result)
    {
        if (!result.gameObject.TryGetComponent(out UISpawnItemSlot spawnSlot)) return false;

        selectedSpawnSlot = spawnSlot;
        selectedDraggable = spawnSlot.SpawnDraggableItem();
        selectedDraggable.transform.position = spawnSlot.transform.position;
        originPosition = spawnSlot.transform.position;

        inventory.CheckWeight(selectedDraggable.GetItemData());
        return true;
    }

    // Try to drag an item from the inventory
    private bool TryDragFromInventory(RaycastResult result)
    {
        if (!result.gameObject.TryGetComponent(out UIInventorySlot inventorySlot)) return false;

        if (inventory.CheckRepeatedSlot(inventorySlot.X, inventorySlot.Y) || !inventorySlot.HasItem())
            return false;

        originPosition = inventorySlot.transform.position;
        originX = inventorySlot.X;
        originY = inventorySlot.Y;
        selectedDraggable = inventorySlot.GetItem();
        selectedInventorySlot = inventorySlot;

        // Reset right click flag to prevent accidental rotation
        playerInputAction.rightClick = false;

        inventory.RemoveItem(selectedDraggable);
        return true;
    }

    // Update dragged item position and check placement validity
    private void HoldDrag()
    {
        if (selectedDraggable == null) return;

        // Update item position
        selectedDraggable.transform.position = GetGridAlignedPosition(
            playerInputAction.point,
            selectedDraggable.GetItemData(),
            selectedDraggable.RotationStapes);

        // Check for inventory slot interaction
        bool overInventory = ProcessInventoryPlacement();

        // Reset highlighting if not over inventory
        if (!overInventory && selectedInventorySlot != null)
        {
            selectedInventorySlot = null;
            inventory.UpdateAllInventorySlotHighlight();
            inventory.ResetRepeatedSlotCheck();
        }
    }

    // Process inventory placement during drag
    private bool ProcessInventoryPlacement()
    {
        var inventoryHits = GetRaycastResults().Where(r => r.gameObject.layer == 6);
        if (inventoryHits.Count() <= 0) return false;

        foreach (RaycastResult result in inventoryHits)
        {
            if (result.gameObject.TryGetComponent(out UIInventorySlot inventorySlot))
            {
                if (inventory.CheckRepeatedSlot(inventorySlot.X, inventorySlot.Y))
                    return true;

                DraggableItem item = selectedDraggable;
                inventory.CanPlaceItem(item.GetItemData(), inventorySlot.X, inventorySlot.Y, item.GetShape());
                selectedInventorySlot = inventorySlot;
                return true;
            }
        }

        return false;
    }

    // End dragging and finalize item placement
    private void CancelDrag()
    {
        if (selectedDraggable == null)
        {
            isDragging = false;
            Cursor.visible = true;
            return;
        }

        // Try dropping in a drop zone
        if (!TryDropInDropZone())
        {
            // Try placing in inventory or return to original position
            if (selectedInventorySlot != null)
            {
                PlaceItemInInventorySlot();
            }
            else if (originX >= 0 && originY >= 0)
            {
                ReturnItemToOrigin();
            }
            else
            {
                Destroy(selectedDraggable.gameObject);
            }
        }

        // Clean up spawn slot if needed
        if (selectedSpawnSlot != null)
        {
            selectedSpawnSlot.ClearSpawnSlot();
            selectedSpawnSlot = null;
        }

        // Reset inventory grid
        inventory.UpdateAllInventorySlotHighlight();
        inventory.ResetRepeatedSlotCheck();

        // Reset player references
        ResetDragState();
    }

    // Try to drop the item in a drop zone
    private bool TryDropInDropZone()
    {
        if (GetRaycastResults().Count <= 0) return false;

        foreach (RaycastResult result in hitObjects)
        {
            if (result.gameObject.TryGetComponent(out UIDropZone dropZone))
            {
                dropZone.DropItem(selectedDraggable);
                originX = -1;
                originY = -1;
                return true;
            }
        }

        return false;
    }

    // Place the dragged item in the selected inventory slot
    private void PlaceItemInInventorySlot()
    {
        if (inventory.PlaceItem(selectedDraggable, selectedInventorySlot.X, selectedInventorySlot.Y))
        {
            ItemData item = selectedDraggable.GetItemData();
            selectedDraggable.transform.position = GetGridAlignedPosition(
                selectedInventorySlot.transform.position,
                item,
                selectedDraggable.RotationStapes);
        }
        else if (originX >= 0 && originY >= 0)
        {
            ReturnItemToOrigin();
        }
        else
        {
            Destroy(selectedDraggable.gameObject);
        }
    }

    // Return the item to its original position
    private void ReturnItemToOrigin()
    {
        inventory.PlaceItem(selectedDraggable, originX, originY);
        ItemData item = selectedDraggable.GetItemData();
        selectedDraggable.transform.position = GetGridAlignedPosition(
            originPosition,
            item,
            selectedDraggable.RotationStapes);
    }

    // Reset all drag-related state variables
    private void ResetDragState()
    {
        selectedInventorySlot = null;
        selectedDraggable = null;
        originPosition = Vector3.zero;
        originX = -1;
        originY = -1;
        isDragging = false;
        Cursor.visible = true;
    }

    // Get raycast results for UI elements under cursor
    private List<RaycastResult> GetRaycastResults()
    {
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = playerInputAction.point;
        hitObjects.Clear(); // Clear list to avoid memory allocation
        EventSystem.current.RaycastAll(pointer, hitObjects);
        return hitObjects;
    }

    // Calculate grid-aligned position based on item properties and rotation
    private Vector3 GetGridAlignedPosition(Vector3 position, ItemData item, int rotationStep)
    {
        float screenRatioX = (float)Screen.width / 1920f;
        float screenRatioY = (float)Screen.height / 1080f;

        Vector3 newPosition = position;
        float offsetX = 0, offsetY = 0;
        float customX = gridSize * item.offsetX * screenRatioX;
        float customY = gridSize * item.offsetY * screenRatioY;

        // Adjust offsets based on shape type and rotation
        switch (item.shapeType)
        {
            case ShapeType.LShape:
                ApplyLShapeOffset(ref customX, ref customY, rotationStep);
                break;
            case ShapeType.Rectangle:
                ApplyRectangleOffset(ref customX, ref customY, rotationStep);
                break;
        }

        offsetX = (item.width - 1) * (gridSize * 0.5f * screenRatioX) + customX;
        offsetY = (item.height - 1) * (gridSize * 0.5f * screenRatioY) + customY;

        newPosition.x += offsetX;
        newPosition.y -= offsetY;
        newPosition.z = 0;

        return newPosition;
    }

    // Apply offset adjustments for L-shaped items
    private void ApplyLShapeOffset(ref float customX, ref float customY, int rotationStep)
    {
        switch (rotationStep)
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

    // Apply offset adjustments for rectangle-shaped items
    private void ApplyRectangleOffset(ref float customX, ref float customY, int rotationStep)
    {
        switch (rotationStep)
        {
            case 0:
            case 2:
                customX = 0;
                customY = 0;
                break;
            case 1:
            case 3:
                customX = -customX;
                customY = -customY;
                break;
        }
    }
}