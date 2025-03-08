using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Misc
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject, ISerializationCallbackReceiver
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public int width;
    public int height;
    public float weight;
    public ItemType type;

    [HideInInspector] public bool[,] shape;
    [HideInInspector, SerializeField] private bool[] serializedShape;

    public void OnBeforeSerialize()
    {
        serializedShape = new bool[width * height];
        if (shape != null)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    serializedShape[x + y * width] = shape[x, y];
        }
    }

    public void OnAfterDeserialize()
    {
        shape = new bool[width, height];
        if (serializedShape != null && serializedShape.Length == width * height)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    shape[x, y] = serializedShape[x + y * width];
        }
    }
}
