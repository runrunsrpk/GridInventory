using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Misc
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public float weight;
    public ItemType type;
    [HideInInspector] public bool[,] shape;

    // create default shape
    private void OnEnable()
    {
        if (shape == null)
        {
            shape = new bool[1, 1];
            shape[0, 0] = true;
        }
    }

    public ItemData Clone()
    {
        ItemData clone = Instantiate(this);
        clone.name = this.name;

        // คลอน shape array
        bool[,] shapeClone = new bool[shape.GetLength(0), shape.GetLength(1)];
        for (int x = 0; x < shape.GetLength(0); x++)
        {
            for (int y = 0; y < shape.GetLength(1); y++)
            {
                shapeClone[x, y] = shape[x, y];
            }
        }
        clone.shape = shapeClone;

        return clone;
    }

    public string GetSummary()
    {
        int gridCount = CountGridCells();
        return $"{itemName}\nWeight: {weight} kg\nSize: {gridCount} grid{(gridCount > 1 ? "s" : "")}";
    }

    public int CountGridCells()
    {
        int count = 0;
        for (int x = 0; x < shape.GetLength(0); x++)
        {
            for (int y = 0; y < shape.GetLength(1); y++)
            {
                if (shape[x, y]) count++;
            }
        }
        return count;
    }
}
