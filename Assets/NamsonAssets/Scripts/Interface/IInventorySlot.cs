using UnityEngine;

public interface IInventorySlot
{
    void SetItem(DraggableItem item);
    void ClearItem();
    DraggableItem GetItem();
    bool HasItem();
    void SetHighlightColor(int highlightIndex);
    void SetIndex(int x, int y);
}
