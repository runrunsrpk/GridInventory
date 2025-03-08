using UnityEngine;
using UnityEngine.UI;

public class UISpawnItemSlot : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private Image icon;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private DraggableItem draggablePrefab;

    private UISpawnItem uiSpawnItem;
    private Canvas canvas;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        uiSpawnItem = GetComponentInParent<UISpawnItem>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetItemData(ItemData itemData)
    {
        this.itemData = itemData;
        icon.sprite = itemData.icon;
    }

    public void SetBoxColliderEnable(bool isEnable)
    {
        boxCollider.enabled = isEnable;
    }

    public DraggableItem SpawnDraggableItem()
    {
        uiSpawnItem.SetChildSlotEnable(false);
        
        Color color = Color.white;
        color.a = 0.5f;
        icon.color = color;

        DraggableItem item = Instantiate(draggablePrefab, canvas.transform);
        item.Init(itemData);
        return item;
    }

    public void ClearSpawnSlot()
    {
        uiSpawnItem.SetChildSlotEnable(true);

        icon.color = Color.white;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size * 0.9f);
    }
}
