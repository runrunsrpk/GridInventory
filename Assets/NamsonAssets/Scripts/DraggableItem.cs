using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemConnectedState
{
    Empty,
    Waiting,
    Closed,
    Opened
}
public class DraggableItem : MonoBehaviour
{
    [SerializeField] private Image image;

    public int GridX => gridX;
    [SerializeField] private int gridX;
    public int GridY => gridY;
    [SerializeField] private int gridY;

    public List<Vector2Int> OccupiedGrids => occupiedGrids;
    private List<Vector2Int> occupiedGrids;

    public ItemConnectedState ConnectedState => connectedState;
    [SerializeField] private ItemConnectedState connectedState;
    [SerializeField] private int connectionGroupId = -1;

    public BonusData BonusData => bonusData;
    private BonusData bonusData;

    public int PlacementOrder {  get => placementOrder; set => placementOrder = value; }
    private int placementOrder;

    public int RotationStapes => rotationSteps;
    private int rotationSteps = 0; // 0, 1, 2, 3 for 0, 90, 180, 270 degrees


    private ItemData itemData;
    private bool[,] rotatedShape;

    public void Init(ItemData itemData)
    {
        this.itemData = itemData;
        image.sprite = itemData.icon;
        rotatedShape = (bool[,])itemData.shape.Clone();
        connectedState = ItemConnectedState.Empty;

        UpdateImageScale();

        //DebugShape();
    }

    public void Rotate()
    {
        rotationSteps = (rotationSteps + 1) % 4;
        UpdateRotatedShape();
    }

    public void SetGridPosition(int gridX, int gridY)
    {
        this.gridX = gridX;
        this.gridY = gridY;

        SetOccupiedGrids(new Vector2Int(gridX, gridY));
    }

    public void SetBonus(BonusData bonus, ItemConnectedState state)
    {
        this.bonusData = bonus;
        this.connectedState = state;
    }

    public BonusData GetBonusData()
    {
        return bonusData;
    }

    public ItemData GetItemData()
    {
        return itemData;
    }

    public bool[,] GetShape()
    {
        return rotatedShape;
    }

    public int GetConnectionGroupId()
    {
        return connectionGroupId;
    }

    public void SetConnectionGroupId(int id)
    {
        connectionGroupId = id;
    }

    public void ClearItem()
    {
        connectedState = ItemConnectedState.Empty;
        occupiedGrids.Clear();
        ClearConnectionGroup();
    }

    public void ClearConnectionGroup()
    {
        connectionGroupId = -1;
    }

    private void SetOccupiedGrids(Vector2Int gridIndex)
    {
        if (occupiedGrids == null)
        {
            occupiedGrids = new List<Vector2Int>();
        }
        else
        {
            occupiedGrids.Clear();
        }

        bool[,] shape = rotatedShape;
        int shapeWidth = shape.GetLength(0);
        int shapeHeight = shape.GetLength(1);

        // Place item in grid
        for (int i = 0; i < shapeWidth; i++)
        {
            for (int j = 0; j < shapeHeight; j++)
            {
                if (shape[i, j])
                {
                    occupiedGrids.Add(new Vector2Int(gridIndex.x + i, gridIndex.y + j));
                }
            }
        }
    }

    private void UpdateImageScale()
    {
        float scaleX = 1f;
        float scaleY = 1f;
        if (itemData.shapeType == ShapeType.Square)
        {
            scaleX = (itemData.width == 1) ? 0.5f : 1f;
            scaleY = (itemData.height == 1) ? 0.5f : 1f;
        }
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void UpdateRotatedShape()
    {
        bool[,] originShape = itemData.shape;
        int originWidth = originShape.GetLength(0);
        int originHeight = originShape.GetLength(1);

        // Clear previous occupied grids when rotating
        if (occupiedGrids != null)
        {
            occupiedGrids.Clear();
        }

        switch (rotationSteps)
        {
            case 0: // No rotation (0 degrees)
                rotatedShape = (bool[,])originShape.Clone();
                break;

            case 1: // 90 degrees clockwise
                rotatedShape = new bool[originHeight, originWidth];
                for (int x = 0; x < originWidth; x++)
                {
                    for (int y = 0; y < originHeight; y++)
                    {
                        // Convert coordinates properly for 270-degree rotation
                        rotatedShape[originHeight - 1 - y, x] = originShape[x, y];
                    }
                }
                break;

            case 2: // 180 degrees
                rotatedShape = new bool[originWidth, originHeight];
                for (int x = 0; x < originWidth; x++)
                {
                    for (int y = 0; y < originHeight; y++)
                    {
                        // Convert coordinates properly for 180-degree rotation
                        rotatedShape[originWidth - 1 - x, originHeight - 1 - y] = originShape[x, y];
                    }
                }
                break;

            case 3: // 270 degrees clockwise
                rotatedShape = new bool[originHeight, originWidth];
                for (int x = 0; x < originWidth; x++)
                {
                    for (int y = 0; y < originHeight; y++)
                    {
                        // Convert coordinates properly for 90-degree rotation
                        rotatedShape[y, originWidth - 1 - x] = originShape[x, y];
                    }
                }
                break;
        }

        // If the item is placed on the grid, update occupied grids
        if (gridX >= 0 && gridY >= 0)
        {
            SetOccupiedGrids(new Vector2Int(gridX, gridY));
        }

        // Update visual rotation
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Rotate the visual representation to match the rotated shape
        transform.rotation = Quaternion.Euler(0, 0, -90f * rotationSteps);
    }

    private void DebugShape()
    {
        int width = rotatedShape.GetLength(0);
        int height = rotatedShape.GetLength(1);
        string debugText = "";

        for (int x = 0; x < width; x++)
        {
            for(int y = 0;y < height; y++)
            {
                debugText += $"[{x},{y}] - {rotatedShape[x, y]} |";
            }
        }

        Debug.Log(debugText);
    }
}
