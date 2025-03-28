using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Database : MonoBehaviour
{
    public static Database Instance;

    public static Action OnDatabaseLoaded;

    [SerializeField] private Dictionary<string, ItemData> itemDatabase;
    [SerializeField] private Dictionary<string, BonusData> bonusDatabase;

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
        LoadBonusDatabase();

        //List<string> playerItems = new List<string> { "002", "002"};

        //Debug.Log($"Best bonuse: {GetBestBonusData(playerItems).bonusName}");

        //List<BonusData> bonuses = GetAnyBonusDatas(playerItems);
        //Debug.Log($"Bonuses search: {bonuses.Count} bonuses");
        
        //List<BonusData> higherBonuses = GetHigherBonusDatas(playerItems);
        //Debug.Log($"Higher bonuses search: {higherBonuses.Count} bonuses");

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

    private void LoadBonusDatabase()
    {
        bonusDatabase = new Dictionary<string, BonusData>();
        BonusData[] loadedBonus = Resources.LoadAll<BonusData>("Bonuses");

        foreach (BonusData bonus in loadedBonus)
        {
            if (!bonusDatabase.ContainsKey(bonus.bonusID))
            {
                bonusDatabase.Add(bonus.bonusID, bonus);
            }
        }

        Debug.Log($"Load bonus data success: {bonusDatabase.Count} bonuses");
    }

    public BonusData GetBonusData(string id)
    {
        return bonusDatabase[id];
    }

    public List<BonusData> GetAnyBonusDatas(List<string> itemIds)
    {
        List<BonusData> bonusDatas = new List<BonusData>();

        // Check if input has duplicates
        bool hasDuplicates = itemIds.Count != itemIds.Distinct().Count();

        // All input with duplicates
        if (hasDuplicates)
        {
            bonusDatas = bonusDatabase.Values.Where(bonus => IsBonusMatch(bonus.bonusRequirement, itemIds)).ToList();
        }
        // All input with no duplicate
        else
        {
            bonusDatas = bonusDatabase.Values.Where(bonus => itemIds.All(item => bonus.bonusRequirement.Contains(item))).ToList();
        }

        return bonusDatas;
    }

    // Check repeated materials
    private bool IsBonusMatch(List<string> requiredItems, List<string> playerItems)
    {
        Dictionary<string, int> requiredCount = requiredItems.GroupBy(item => item).ToDictionary(g => g.Key, g => g.Count());
        Dictionary<string, int> playerCount = playerItems.GroupBy(item => item).ToDictionary(g => g.Key, g => g.Count());

        foreach (var pair in playerCount)
        {
            if (!requiredCount.TryGetValue(pair.Key, out int count) || count < pair.Value)
            {
                return false;
            }
        }
        return true;
    }

    public List<BonusData> GetHigherBonusDatas(List<string> itemIds)
    {
        List<BonusData> bonusDatas = new List<BonusData>();
        bonusDatas = GetAnyBonusDatas(itemIds).Where(bonus => bonus.bonusRequirement.Count > itemIds.Count).ToList();

        return bonusDatas;
    }

    public BonusData GetBestBonusData(List<string> itemIds)
    {
        // Group items by name and count occurrences
        Dictionary<string, int> playerCount = itemIds.GroupBy(item => item).ToDictionary(g => g.Key, g => g.Count());

        // Find all bonuses that match exact requirements
        List<BonusData> bonusDatas = bonusDatabase.Values.Where(bonus =>
        {
            // Group bonus requirements and count occurrences
            Dictionary<string, int> requiredCount = bonus.bonusRequirement.GroupBy(item => item).ToDictionary(g => g.Key, g => g.Count());

            // Check if counts are the same (same number of unique items)
            if (playerCount.Count != requiredCount.Count)
                return false;

            // Check if every player item exists in requirements with exact same count
            foreach (var pair in playerCount)
            {
                if (!requiredCount.TryGetValue(pair.Key, out int count) || count != pair.Value)
                    return false;
            }

            return true;
        }).ToList();

        // Return the bonus with lowest priority
        if (bonusDatas.Any())
        {
            return bonusDatas.OrderBy(bonus => bonus.priority).First();
        }

        return null;
    }

    public List<string> GetRequiredItemsForBonus(BonusData bonus)
    {
        return bonus.bonusRequirement;
    }
}
