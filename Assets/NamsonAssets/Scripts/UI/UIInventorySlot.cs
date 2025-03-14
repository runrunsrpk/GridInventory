using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventorySlot : MonoBehaviour, IInventorySlot
{
    public int X => x;
    private int x;
    public int Y => y;
    private int y;

    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text indexText;

    [Header("Slot")]
    [SerializeField] private DraggableItem currentItem;
    [SerializeField] private Color slotDefaultColor;
    [SerializeField] private Color slotPlacedColor;
    [SerializeField] private Color slotAllowColor;
    [SerializeField] private Color slotDenyColor;

    public bool HasItem()
    {
        return currentItem != null;
    }

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        indexText.text = $"({x},{y})";
        itemNameText.text = "";
    }

    public void SetHighlightColor(int highlight)
    {
        switch (highlight)
        {
            case 0:
                image.color = slotDefaultColor;
                break;
            case 1:
                image.color = slotPlacedColor;
                break;
            case 2:
                image.color = slotAllowColor;
                break;
            case 3:
                image.color = slotDenyColor;
                break;
            default:
                image.color = slotDefaultColor;
                break;
        }
    }

    public void SetItem(DraggableItem item)
    {
        currentItem = item;
        itemNameText.text = item.GetItemData().itemName;
    }

    public DraggableItem GetItem()
    {
        return currentItem;
    }

    public void ClearItem()
    {
        currentItem = null;
        itemNameText.text = "";
    }
}
