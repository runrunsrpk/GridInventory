using UnityEngine;
using UnityEngine.UI;

public class UISpawnItemSlot : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private Image icon;
    [SerializeField] private BoxCollider2D boxCollider;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size * 0.9f);
    }
}
