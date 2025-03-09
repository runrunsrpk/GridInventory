using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BonusManager : IBonusManager
{
    private readonly UIInventorySlot[,] grid;
    private readonly int width;
    private readonly int height;

    // Track connected groups and bonuses
    private Dictionary<int, List<DraggableItem>> connectedGroups = new Dictionary<int, List<DraggableItem>>();
    private Dictionary<int, BonusData> groupBonuses = new Dictionary<int, BonusData>();
    // Track bonuses with their reference count to handle multiple instances
    private Dictionary<string, int> bonusRefCounts = new Dictionary<string, int>();
    //private List<BonusData> currentBonuses = new List<BonusData>();
    private Dictionary<int, BonusData> activeBonuses = new Dictionary<int, BonusData>();
    private int groupCounter = 0;

    // Constructor with dependency injection
    public BonusManager(UIInventorySlot[,] grid, int width, int height)
    {
        this.grid = grid;
        this.width = width;
        this.height = height;
    }

    // Get all current active bonuses for UI display
    public List<BonusData> GetCurrentBonuses()
    {
        // Simply return all active bonuses as a list (with duplicates preserved)
        return activeBonuses.Values.ToList();
    }

    // Notify UI of bonus changes
    private void NotifyBonusChanges()
    {
        UIInventory.OnBonusChanged?.Invoke(GetCurrentBonuses());
    }

    // Check connections starting from a position
    public void CheckConnections(Vector2Int startIndex)
    {
        // Check if position is occupied
        if (!IsOccupied(startIndex)) return;

        // Get the item at the starting position
        DraggableItem startItem = GetItemAt(startIndex);

        if (startItem == null) return;

        // For items already in a group, only check if state allows for connections
        if (startItem.GetConnectionGroupId() != -1 &&
            startItem.ConnectedState != ItemConnectedState.Opened &&
            startItem.ConnectedState != ItemConnectedState.Waiting)
            return;

        // Find all connected items using DFS
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<DraggableItem> connectedItems = new List<DraggableItem>();
        DFS(startIndex, visited, connectedItems);

        // Need at least 2 items to form a connection
        if (connectedItems.Count < 2) return;

        // Check if startItem is already in an existing group
        int existingGroupId = startItem.GetConnectionGroupId();

        // If existing group contains exactly the same items, nothing to do
        if (existingGroupId != -1 && IsIdenticalGroup(existingGroupId, connectedItems))
            return;

        // Clear any previous connections for these items
        foreach (DraggableItem item in connectedItems)
        {
            ClearItemConnections(item);
        }

        // Create new group
        int groupId = ++groupCounter;
        connectedGroups[groupId] = new List<DraggableItem>(connectedItems);

        // Assign new group ID to all items
        foreach (DraggableItem item in connectedItems)
        {
            item.SetConnectionGroupId(groupId);
        }

        // Apply bonus to the new group
        EvaluateAndApplyBonus(groupId, connectedItems);
    }

    // Check if a group contains exactly the same items
    private bool IsIdenticalGroup(int groupId, List<DraggableItem> items)
    {
        if (!connectedGroups.ContainsKey(groupId))
            return false;

        var existingGroup = connectedGroups[groupId];

        if (existingGroup.Count != items.Count)
            return false;

        // Compare item sets (order doesn't matter)
        HashSet<DraggableItem> itemSet = new HashSet<DraggableItem>(items);
        return existingGroup.All(item => itemSet.Contains(item));
    }

    // Clear an item's connection data and remove it from its current group
    private void ClearItemConnections(DraggableItem item)
    {
        int groupId = item.GetConnectionGroupId();
        if (groupId == -1) return;

        if (connectedGroups.ContainsKey(groupId))
        {
            // Remove item from group
            connectedGroups[groupId].Remove(item);

            // If group is now empty, remove it completely
            if (connectedGroups[groupId].Count == 0)
            {
                // Remove bonus
                if (groupBonuses.ContainsKey(groupId))
                {
                    // Remove from active bonuses
                    if (activeBonuses.ContainsKey(groupId))
                    {
                        activeBonuses.Remove(groupId);
                    }

                    groupBonuses.Remove(groupId);
                }

                connectedGroups.Remove(groupId);
            }
            else
            {
                // Re-evaluate the remaining group
                EvaluateAndApplyBonus(groupId, connectedGroups[groupId]);
            }
        }

        // Reset the item's connection data
        item.ClearConnectionGroup();
        item.SetBonus(null, ItemConnectedState.Empty);
    }

    // Remove an item from inventory and handle group changes
    public void RemoveItemFromGroups(DraggableItem item)
    {
        if (item == null) return;

        int groupId = item.GetConnectionGroupId();

        // If item is not in a group, nothing to do
        if (groupId == -1) return;

        // If group doesn't exist, nothing to do
        if (!connectedGroups.ContainsKey(groupId)) return;

        // Get the connected items (excluding the removed item)
        List<DraggableItem> remainingItems = new List<DraggableItem>(connectedGroups[groupId]);
        remainingItems.Remove(item);

        // Clear the item's connection data
        item.ClearConnectionGroup();
        item.SetBonus(null, ItemConnectedState.Empty);

        // Remove the item from the group
        connectedGroups[groupId].Remove(item);

        // If group is now empty or has only one item
        if (remainingItems.Count <= 1)
        {
            // Remove bonus
            if (groupBonuses.ContainsKey(groupId))
            {
                // Remove from active bonuses
                if (activeBonuses.ContainsKey(groupId))
                {
                    activeBonuses.Remove(groupId);
                }

                groupBonuses.Remove(groupId);
            }

            // If one item remains, reset its data
            if (remainingItems.Count == 1)
            {
                DraggableItem lastItem = remainingItems[0];
                lastItem.ClearConnectionGroup();
                lastItem.SetBonus(null, ItemConnectedState.Empty);
            }

            // Remove the group
            connectedGroups.Remove(groupId);
        }
        else
        {
            // Re-evaluate remaining items
            EvaluateAndApplyBonus(groupId, remainingItems);
        }

        // Check for new potential connections in the neighboring items
        HashSet<Vector2Int> positionsToCheck = new HashSet<Vector2Int>();
        foreach (var neighbor in GetAdjacentPositions(item))
        {
            if (IsOccupied(neighbor))
            {
                positionsToCheck.Add(neighbor);
            }
        }

        // Recheck connections
        RecheckConnections(positionsToCheck);

        // Notify UI of bonus changes
        NotifyBonusChanges();
    }

    // Remove an item from its current group
    //private void RemoveItemFromGroup(DraggableItem item)
    //{
    //    int groupId = item.GetConnectionGroupId();
    //    if (groupId == -1) return;

    //    if (connectedGroups.ContainsKey(groupId))
    //    {
    //        // Remove item from group
    //        connectedGroups[groupId].Remove(item);

    //        // If group is now empty, remove it
    //        if (connectedGroups[groupId].Count == 0)
    //        {
    //            BonusData removedBonus = null;
    //            if (groupBonuses.ContainsKey(groupId))
    //            {
    //                removedBonus = groupBonuses[groupId];
    //                groupBonuses.Remove(groupId);
    //            }

    //            connectedGroups.Remove(groupId);

    //            // Remove bonus if no other group uses it
    //            if (removedBonus != null)
    //            {
    //                RemoveBonusIfUnused(removedBonus);
    //            }
    //        }
    //    }

    //    // Clear the item's group info
    //    item.ClearConnectionGroup();
    //}

    // Remove bonus if no groups are using it
    //private void RemoveBonusIfUnused(BonusData bonus)
    //{
    //    if (bonus == null) return;

    //    // Decrement reference count
    //    string bonusId = bonus.bonusName;
    //    if (bonusRefCounts.ContainsKey(bonusId))
    //    {
    //        bonusRefCounts[bonusId]--;

    //        // If reference count reaches zero, remove the bonus
    //        if (bonusRefCounts[bonusId] <= 0)
    //        {
    //            bonusRefCounts.Remove(bonusId);

    //            // Find and remove all instances of this bonus from current bonuses
    //            currentBonuses.RemoveAll(b => b.bonusName == bonusId);

    //            // Notify listeners of change
    //            UIInventory.OnBonusChanged?.Invoke(currentBonuses);
    //            Debug.Log($"Bonus Removed: {bonusId}");
    //        }
    //    }
    //}

    // Check if any group is still using the bonus
    private bool IsAnyGroupStillUsingBonus(BonusData bonus)
    {
        if (bonus == null) return false;
        return bonusRefCounts.ContainsKey(bonus.bonusName) && bonusRefCounts[bonus.bonusName] > 0;
    }

    // Evaluate and apply bonus for a group
    private void EvaluateAndApplyBonus(int groupId, List<DraggableItem> items)
    {
        // Get item IDs for bonus lookup
        List<string> itemIds = items.Select(item => item.GetItemData().itemID).ToList();

        // Get best possible bonus for these items
        BonusData bestBonus = Database.Instance.GetBestBonusData(itemIds);

        // Check if there are higher potential bonuses
        List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(itemIds);
        bool hasHigherBonusPossibility = higherBonuses.Count > 0;

        // Clean up any existing bonus for this group
        if (groupBonuses.ContainsKey(groupId))
        {
            groupBonuses.Remove(groupId);
        }

        if (activeBonuses.ContainsKey(groupId))
        {
            activeBonuses.Remove(groupId);
        }

        // Apply new bonus if available
        if (bestBonus != null)
        {
            // Store bonus for this group
            groupBonuses[groupId] = bestBonus;
            activeBonuses[groupId] = bestBonus;

            // Set state based on higher bonus possibility
            ItemConnectedState state = hasHigherBonusPossibility ?
                ItemConnectedState.Opened : ItemConnectedState.Closed;

            // Apply bonus to all items in the group
            foreach (DraggableItem item in items)
            {
                item.SetBonus(bestBonus, state);
            }

            Debug.Log($"Bonus Activated: {bestBonus.bonusName}, Group: {groupId}, Items: {items.Count}");
        }
        else if (hasHigherBonusPossibility)
        {
            // No bonus yet but potential for higher - set waiting state
            foreach (DraggableItem item in items)
            {
                item.SetBonus(null, ItemConnectedState.Waiting);
            }
            Debug.Log($"Group {groupId} is waiting for higher bonus possibility with {items.Count} items");
        }
        else
        {
            // No bonus and no potential - set empty state
            foreach (DraggableItem item in items)
            {
                item.SetBonus(null, ItemConnectedState.Empty);
            }
        }

        // Notify UI of bonus changes
        NotifyBonusChanges();
    }

    // Recheck connections at specified positions
    public void RecheckConnections(HashSet<Vector2Int> positions)
    {
        // Track checked groups to avoid redundant checks
        HashSet<int> checkedGroups = new HashSet<int>();

        // Collect additional positions to check
        HashSet<Vector2Int> additionalPositions = new HashSet<Vector2Int>();

        // Check primary positions
        foreach (Vector2Int position in positions)
        {
            if (IsOccupied(position))
            {
                DraggableItem item = GetItemAt(position);
                int groupId = item.GetConnectionGroupId();

                // Collect adjacent positions for items with potential connections
                if (item.ConnectedState == ItemConnectedState.Opened ||
                    item.ConnectedState == ItemConnectedState.Waiting)
                {
                    foreach (Vector2Int adjPos in GetAdjacentPositions(item))
                    {
                        if (IsOccupied(adjPos))
                        {
                            additionalPositions.Add(adjPos);
                        }
                    }
                }

                // Check if this group hasn't been checked yet
                if (groupId == -1 || !checkedGroups.Contains(groupId))
                {
                    if (groupId != -1)
                    {
                        checkedGroups.Add(groupId);
                    }

                    // Recheck connections
                    CheckConnections(position);
                }
            }
        }

        // Check additional positions
        foreach (Vector2Int position in additionalPositions)
        {
            if (IsOccupied(position))
            {
                DraggableItem item = GetItemAt(position);
                int groupId = item.GetConnectionGroupId();

                if (groupId == -1 || !checkedGroups.Contains(groupId))
                {
                    if (groupId != -1)
                    {
                        checkedGroups.Add(groupId);
                    }

                    CheckConnections(position);
                }
            }
        }
    }

    // Find common group ID if all items are in the same group
    private int FindCommonGroupId(List<DraggableItem> items)
    {
        if (items.Count == 0) return -1;

        int commonGroupId = items[0].GetConnectionGroupId();
        if (commonGroupId == -1) return -1;

        foreach (var item in items)
        {
            if (item.GetConnectionGroupId() != commonGroupId)
            {
                return -1; // Not all in same group
            }
        }

        return commonGroupId;
    }

    // DFS to find connected items
    private void DFS(Vector2Int index, HashSet<Vector2Int> visited, List<DraggableItem> result)
    {
        if (visited.Contains(index)) return;
        visited.Add(index);

        DraggableItem item = GetItemAt(index);
        if (item == null) return;

        // Skip items that don't allow connections
        if (item.ConnectedState == ItemConnectedState.Closed) return;

        if (!result.Contains(item)) result.Add(item);

        // Check neighbors
        foreach (Vector2Int neighbor in GetNeighbors(item))
        {
            DFS(neighbor, visited, result);
        }
    }

    // Get neighboring positions for an item
    private List<Vector2Int> GetNeighbors(DraggableItem item)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (Vector2Int grid in item.OccupiedGrids)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = grid + dir;
                if (IsValidPosition(neighborPos) && IsOccupied(neighborPos))
                {
                    neighbors.Add(neighborPos);
                }
            }
        }
        return neighbors;
    }

    // Get adjacent positions for an item
    private List<Vector2Int> GetAdjacentPositions(DraggableItem item)
    {
        List<Vector2Int> adjacent = new List<Vector2Int>();

        foreach (Vector2Int grid in item.OccupiedGrids)
        {
            List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjacentPos = grid + dir;
                if (IsValidPosition(adjacentPos))
                {
                    adjacent.Add(adjacentPos);
                }
            }
        }

        return adjacent;
    }

    // Get adjacent positions for a position
    public List<Vector2Int> GetAdjacentPositions(Vector2Int position)
    {
        List<Vector2Int> adjacent = new List<Vector2Int>();
        List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int adjacentPos = position + dir;
            if (IsValidPosition(adjacentPos))
            {
                adjacent.Add(adjacentPos);
            }
        }

        return adjacent;
    }

    

    // Helper methods
    public bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    public bool IsOccupied(Vector2Int index)
    {
        if (!IsValidPosition(index)) return false;
        return grid[index.x, index.y].HasItem();
    }

    public DraggableItem GetItemAt(Vector2Int index)
    {
        return grid[index.x, index.y].GetItem();
    }

    //private List<BonusData> GetFormattedBonuses()
    //{
    //    List<BonusData> formattedBonuses = new List<BonusData>();
    //    HashSet<string> processedBonusIds = new HashSet<string>();

    //    foreach (var bonus in currentBonuses)
    //    {
    //        // Skip if we've already processed this bonus type
    //        if (processedBonusIds.Contains(bonus.bonusID))
    //            continue;

    //        processedBonusIds.Add(bonus.bonusID);

    //        // Since BonusData is a ScriptableObject, we don't need to create new instances
    //        // We just need to ensure we're only adding each unique bonus once
    //        formattedBonuses.Add(bonus);
    //    }

    //    return formattedBonuses;
    //}
}
