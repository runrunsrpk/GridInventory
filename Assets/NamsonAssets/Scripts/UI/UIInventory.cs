using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour, IInventory
{
    public static Action<List<BonusData>> OnBonusChanged;

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
    private int placementOrderCounter = 0;

    // Use composition for bonus management
    private BonusManager bonusManager;

    private void Start()
    {
        SpawnInventorySlot();
        SetCurrentWeight(currentWeight);
        SetMaxWeight(maxWeight);

        // Initialize bonus manager
        bonusManager = new BonusManager(grid, width, height);
    }

    // Check if weight limit would be exceeded
    public void CheckWeight(ItemData itemData)
    {
        if (itemData == null) return;

        if (currentWeight + itemData.weight > maxWeight)
        {
            SetAllInventorySlotHighlight(3);
        }
    }

    // Check for repeated slot selection
    public bool CheckRepeatedSlot(int x, int y)
    {
        return currentX == x && currentY == y;
    }

    // Reset slot check
    public void ResetRepeatedSlotCheck()
    {
        currentX = 0; currentY = 0;
    }

    // Checks if an item can be placed at the specified position
    public bool CanPlaceItem(ItemData itemData, int x, int y, bool[,] rotatedShape = null)
    {
        // Check if we have a valid item
        if (itemData == null) return false;

        // Check weight limit
        if (currentWeight + itemData.weight > maxWeight)
            return false;

        currentX = x; currentY = y;

        // Use provided shape or default to item's shape
        bool[,] shape = rotatedShape ?? itemData.shape;
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        UpdateSlotHightlight(x, y, shape);

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
                    SetInventorySlotHighlight(x + i, y + j, 1);
                }
            }
        }

        // Set item properties
        item.SetGridPosition(x, y);
        item.PlacementOrder = ++placementOrderCounter;

        // Update weight
        currentWeight += item.GetItemData().weight;
        SetCurrentWeight(currentWeight);

        // Check for bonuses using the position where the item was placed
        bonusManager.CheckConnections(new Vector2Int(x, y));

        return true;
    }

    // Removes an item from the inventory
    public void RemoveItem(DraggableItem item)
    {
        int x = item.GridX;
        int y = item.GridY;

        bool[,] shape = item.GetShape();
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        // Store positions to check later
        HashSet<Vector2Int> neighborPositions = new HashSet<Vector2Int>();

        // Get neighboring positions to check for connections later
        foreach (var grid in item.OccupiedGrids)
        {
            // Add all adjacent positions
            foreach (var adjPos in bonusManager.GetAdjacentPositions(grid))
            {
                if (bonusManager.IsOccupied(adjPos))
                {
                    neighborPositions.Add(adjPos);
                }
            }
        }

        // Clear grid slots
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (shape[i, j] && x + i < width && y + j < height)
                {
                    grid[x + i, y + j].ClearItem();
                }
            }
        }

        // Let bonus manager handle group removal first
        bonusManager.RemoveItemFromGroups(item);

        // Clear the item
        item.ClearItem();

        // Update weight
        currentWeight -= item.GetItemData().weight;
        SetCurrentWeight(currentWeight);

        // Update highlights
        UpdateAllInventorySlotHighlight();

        // Re-check connections for neighboring items
        bonusManager.RecheckConnections(neighborPositions);
    }

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

    #region Inventory Highlight
    private void UpdateSlotHightlight(int x, int y, bool[,] shape)
    {
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        UpdateAllInventorySlotHighlight();
        bool isDeny = false;

        // Check all highlight slot
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (!shape[i, j])
                    continue;

                if (x + i < 0 || y + j < 0 || x + i >= width || y + j >= height || grid[x + i, y + j].HasItem())
                {
                    isDeny = true;
                    break;
                }
            }
        }

        // Show highlight slot
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (!shape[i, j])
                    continue;

                if (isDeny)
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

    public void UpdateAllInventorySlotHighlight()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].HasItem())
                {
                    SetInventorySlotHighlight(x, y, 1);
                }
                else
                {
                    SetInventorySlotHighlight(x, y, 0);
                }
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
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;

        grid[x, y].SetHighlightColor(highlight);
    }
    #endregion

    #region Inventory Weight
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
    #endregion
}