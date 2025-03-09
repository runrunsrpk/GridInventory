using UnityEngine;

public interface IInventorySlot
{
    void SetItem(DraggableItem item);
    void ClearItem();
    DraggableItem GetItem();
    bool HasItem();
    void SetHighlightColor(int highlightIndex);
    void Init(int x, int y);
}
