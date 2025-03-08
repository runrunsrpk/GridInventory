using UnityEngine;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour
{
    [SerializeField] private Image image;

    private int placementOrder;
    private ItemData itemData;
    private bool[,] rotatedShape;
    private int rotationSteps = 0; // 0, 1, 2, 3 for 0, 90, 180, 270 degrees

    public void Init(ItemData itemData)
    {
        this.itemData = itemData;
        image.sprite = itemData.icon;
        rotatedShape = (bool[,])itemData.shape.Clone();
        //DebugShape();
    }

    public void Rotate()
    {
        rotationSteps = (rotationSteps + 1) % 4;
        UpdateRotatedShape();
    }

    public ItemData GetItemData()
    {
        return itemData;
    }

    public bool[,] GetShape()
    {
        return rotatedShape;
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
