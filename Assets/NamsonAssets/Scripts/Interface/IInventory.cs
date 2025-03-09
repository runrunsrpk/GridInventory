using UnityEngine;

public interface IInventory
{
    public bool CanPlaceItem(ItemData itemData, int x, int y, bool[,] rotatedShape = null);
    public bool PlaceItem(DraggableItem item, int x, int y);
    public void RemoveItem(DraggableItem item);
    public void UpdateAllInventorySlotHighlight();
}
