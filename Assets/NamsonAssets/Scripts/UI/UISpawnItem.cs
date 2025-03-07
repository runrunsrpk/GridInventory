using UnityEngine;

public class UISpawnItem : MonoBehaviour
{
    [SerializeField] private UISpawnItemSlot slotPrefab;
    [SerializeField] private Transform parent;

    private void OnEnable()
    {
        Database.OnDatabaseLoaded += SpawnItemSlots;
    }

    private void SpawnItemSlots()
    {
        if (slotPrefab != null)
        {
            // start with id 001
            for (int i = 1; i < Database.Instance.GetMaxItemsCount() + 1; i++)
            {
                string id = (i < 10) ? $"00{i}" : (i < 100) ? $"0{i}" : $"{i}";
                UISpawnItemSlot slot = Instantiate(slotPrefab, parent);
                slot.SetItemData(Database.Instance.GetItemData(id));
            }
        }
    }

    public void SetChildSlotEnable(bool isEnable)
    {
        foreach(Transform child in parent)
        {
            child.GetComponent<UISpawnItemSlot>().SetBoxColliderEnable(isEnable);
        }
    }
}
