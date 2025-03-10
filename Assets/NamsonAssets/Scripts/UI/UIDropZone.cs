using UnityEngine;

public class UIDropZone : MonoBehaviour
{
    public void DropItem(DraggableItem item)
    {
        Debug.Log($"Drop item: {item.GetItemData().itemName}");
        Destroy( item.gameObject );
    }
}
