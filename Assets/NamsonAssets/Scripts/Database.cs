using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Database : MonoBehaviour
{
    public static Database Instance;

    public static Action OnDatabaseLoaded;

    [SerializeField] private Dictionary<string, ItemData> itemDatabase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Start()
    {
        LoadItemDatabase();

        OnDatabaseLoaded?.Invoke();
    }

    private void LoadItemDatabase()
    {
        itemDatabase = new Dictionary<string, ItemData>();
        ItemData[] loadedItem = Resources.LoadAll<ItemData>("Items");

        foreach (ItemData item in loadedItem)
        {
            if(!itemDatabase.ContainsKey(item.itemID))
            {
                itemDatabase.Add(item.itemID, item);
            }
        }

        Debug.Log($"Load items data success: {itemDatabase.Count} items");
    }

    public ItemData GetItemData(string id)
    {
        return itemDatabase[id];
    }

    public int GetMaxItemsCount()
    {
        return itemDatabase.Count;
    }
}
