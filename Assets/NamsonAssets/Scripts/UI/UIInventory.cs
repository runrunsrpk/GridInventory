using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour
{

    [Header("Inventory Setting")]
    [SerializeField, Range(5, 8)] private int width;
    [SerializeField, Range(2, 5)] private int height;
    [SerializeField, Range(8.0f, 99.99f)] private float maxWeight = 8.0f;
    [SerializeField] private UIInventorySlot slotPrefab;
    [SerializeField] private Transform slotParent;

    [Header("UI Setting")]
    [SerializeField] private TMP_Text currentWeightText;
    [SerializeField] private TMP_Text maxWeightText;

    private float currentWeight = 0.0f;
    private UIInventorySlot[,] grid;
    private int currentX = -1;
    private int currentY = -1;

    private void Start()
    {
        SpawnInventorySlot();
        SetCurrentWeight(currentWeight);
        SetMaxWeight(maxWeight);
    }

    public void CheckWeight(ItemData itemData)
    {
        if (itemData == null) return;

        if (currentWeight + itemData.weight > maxWeight)
        {
            SetAllInventorySlotHighlight(3);
        }
    }

    public void ClearInventorySlotHighlight()
    {
        SetAllInventorySlotHighlight(0);
    }

    public bool CheckRepeatedSlot(int x, int y)
    {
        return currentX == x && currentY == y;
    }

    // Checks if an item can be placed at the specified position
    public bool CanPlaceItem(ItemData itemData, int x, int y, bool[,] rotatedShape = null)
    {
        // Check if we have a valid item
        if (itemData == null) return false;

        // Use provided shape or default to item's shape
        bool[,] shape = rotatedShape ?? itemData.shape;
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        // Check highlight slot
        ClearInventorySlotHighlight();
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (shape[i, j])
                {
                    if (x + i >= width || y + j >= height || grid[x + i, y + j].HasItem())
                    {
                        SetInventorySlotHighlight(x + i, y + j, 3);
                    }
                    else
                    {
                        SetInventorySlotHighlight(x + i, y + j, 2);
                    }

                }
            }
        }

        // Check if out of bounds
        if (x < 0 || y < 0 || x + shapeWidth > width || y + shapeHeight > height)
            return false;

        // Check if all required slots are empty
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (shape[i, j])
                {
                    if (x + i >= width || y + j >= height || grid[x + i, y + j].HasItem())
                        return false;
                }
            }
        }

        // Check weight limit
        if (currentWeight + itemData.weight > maxWeight)
            return false;

        return true;
    }

    // Places an item in the inventory
    public bool PlaceItem(DraggableItem item, int x, int y)
    {
        if (!CanPlaceItem(item.GetItemData(), x, y, item.GetShape()))
            return false;

        bool[,] shape = item.GetShape();
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        // Place item in grid
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (shape[i, j])
                {
                    grid[x + i, y + j].SetItem(item);
                }
            }
        }

        // Set item properties
        //item.SetGridPosition(x, y);
        //item.PlacementOrder = ++placementOrderCounter;

        // Add to tracking
        //placedItems.Add(item);

        // Update weight
        currentWeight += item.GetItemData().weight;
        //OnWeightChanged?.Invoke(currentWeight, maxWeight);

        // Notify listeners
        //OnItemPlaced?.Invoke(item);

        return true;
    }

    // Removes an item from the inventory
    //public void RemoveItem(DraggableItem item)
    //{
    //    int x = item.GridX;
    //    int y = item.GridY;

    //    bool[,] shape = item.GetShape();
    //    int shapeWidth = shape.GetLength(0);
    //    int shapeHeight = shape.GetLength(1);

    //    // Clear grid slots
    //    for (int i = 0; i < shapeWidth; i++)
    //    {
    //        for (int j = 0; j < shapeHeight; j++)
    //        {
    //            if (shape[i, j] && x + i < width && y + j < height)
    //            {
    //                grid[x + i, y + j].ClearItem();
    //            }
    //        }
    //    }

    //    // Remove from tracking
    //    //placedItems.Remove(item);

    //    // Update weight
    //    currentWeight -= item.GetItemData().weight;
    //    //OnWeightChanged?.Invoke(currentWeight, maxWeight);

    //    // Notify listeners
    //    //OnItemRemoved?.Invoke(item);
    //}

    private void SpawnInventorySlot()
    {
        slotParent.GetComponent<GridLayoutGroup>().constraintCount = width;
        grid = new UIInventorySlot[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                UIInventorySlot slot = Instantiate(slotPrefab, slotParent);
                slot.name = $"Slot[{x},{y}]";
                slot.SetIndex(x, y);
                grid[x, y] = slot;
            }
        }
    }

    private void SetAllInventorySlotHighlight(int highlight)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SetInventorySlotHighlight(x, y, highlight);
            }
        }
    }

    private void SetInventorySlotHighlight(int x, int y, int highlight)
    {
        grid[x, y].SetHighlightColor(highlight);
    }

    private void SetCurrentWeight(float weight)
    {
        currentWeight = weight;
        currentWeightText.text = weight.ToString("n2");

    }

    private void SetMaxWeight(float weight)
    {
        maxWeight = weight;
        maxWeightText.text = weight.ToString("n2");
    }
}
