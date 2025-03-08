using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour
{
    public int GridX => gridX;
    [SerializeField] private int gridX;
    public int GridY => gridY;
    [SerializeField] private int gridY;
    [SerializeField] private Image image;

    public int PlacementOrder {  get => placementOrder; set => placementOrder = value; }
    private int placementOrder;
    private ItemData itemData;
    private bool[,] rotatedShape;
    public int RotationStapes => rotationSteps;
    private int rotationSteps = 0; // 0, 1, 2, 3 for 0, 90, 180, 270 degrees

    public void Init(ItemData itemData)
    {
        this.itemData = itemData;
        image.sprite = itemData.icon;
        rotatedShape = (bool[,])itemData.shape.Clone();

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
    }

    public ItemData GetItemData()
    {
        return itemData;
    }

    public bool[,] GetShape()
    {
        return rotatedShape;
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

        switch (rotationSteps)
        {
            case 0: // No rotation
                rotatedShape = (bool[,])originShape.Clone();
                break;

            case 1: // 90 degrees clockwise
                rotatedShape = new bool[originHeight, originWidth];
                for (int x = 0; x < originWidth; x++)
                {
                    for (int y = 0; y < originHeight; y++)
                    {
                        rotatedShape[y, originWidth - 1 - x] = originShape[x, y];
                    }
                }
                break;

            case 2: // 180 degrees
                rotatedShape = new bool[originWidth, originHeight];
                for (int x = 0; x < originWidth; x++)
                {
                    for (int y = 0; y < originHeight; y++)
                    {
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
                        rotatedShape[originHeight - 1 - y, x] = originShape[x, y];
                    }
                }
                break;
        }

        // Update visual to match the new rotation
        UpdateVisual();
        //DebugShape();
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
