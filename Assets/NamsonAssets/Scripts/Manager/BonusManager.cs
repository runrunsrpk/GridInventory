using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BonusManager : IBonusManager
{
    private readonly UIInventorySlot[,] grid;
    private readonly int width;
    private readonly int height;

    // Track connected groups and bonuses
    private Dictionary<int, List<DraggableItem>> connectedGroups = new Dictionary<int, List<DraggableItem>>();
    private Dictionary<int, BonusData> groupBonuses = new Dictionary<int, BonusData>();
    private int groupCounter = 0;

    // Constructor with dependency injection
    public BonusManager(UIInventorySlot[,] grid, int width, int height)
    {
        this.grid = grid;
        this.width = width;
        this.height = height;
    }

    // Class to track bonus group candidates
    private class BonusGroupCandidate
    {
        public List<DraggableItem> Items;
        public BonusData Bonus;
        public int BonusPriority;
        public int EarliestPlacement;
        public bool IsExistingGroup;
        public int ExistingGroupId = -1;
    }

    #region Check and remove bonus
    public Dictionary<int, List<DraggableItem>> GetConnectedGroups()
    {
        // Return a copy of the connected groups dictionary
        return new Dictionary<int, List<DraggableItem>>(connectedGroups);
    }

    // Get all current bonuses for UI display
    public List<BonusData> GetCurrentBonuses()
    {
        return groupBonuses.Values.ToList();
    }


    // Main entry point for checking connections
    public void CheckConnections(Vector2Int startIndex)
    {
        if (!IsOccupied(startIndex)) return;

        Debug.Log($"Checking connections starting from {startIndex}");

        DraggableItem item = GetItemAt(startIndex);

        // Rule 1: If item already has a bonus, don't disturb it at all
        if (IsBonus(item))
        {
            Debug.Log($"Item at {startIndex} already has bonus. Preserving it.");
            return;
        }

        // Rule 2: Check if this item would break existing bonuses
        List<DraggableItem> adjacentItems = GetAdjacentItems(item);
        List<int> adjacentBonusGroups = new List<int>();

        Debug.Log($"AdjacentItems count: {adjacentItems.Count}");

        foreach (var adjItem in adjacentItems)
        {
            if (IsBonus(adjItem) && !adjacentBonusGroups.Contains(adjItem.GetConnectionGroupId()))
            {
                adjacentBonusGroups.Add(adjItem.GetConnectionGroupId());
            }
        }

        // If this item is adjacent to only one bonus group, try to add to that group
        if (adjacentBonusGroups.Count == 1)
        {
            int groupId = adjacentBonusGroups[0];
            if (TryAddToExistingGroup(item, groupId))
            {
                Debug.LogWarning($"AddToExistingGroup");
                return;
            }
        }

        // If adjacent to multiple bonus groups, try adding to the one with lowest ID first
        else if (adjacentBonusGroups.Count > 1)
        {
            // Sort groups by ID (lower first)
            adjacentBonusGroups.Sort();

            // Try each group in order
            foreach (int groupId in adjacentBonusGroups)
            {
                if (TryAddToExistingGroup(item, groupId))
                {
                    Debug.LogWarning($"AddToExistingGroup lowest ID");
                    return;
                }
            }
        }

        // If this item isn't part of any group yet, try to form a new group
        // without breaking existing bonuses
        if (item.GetConnectionGroupId() == -1)
        {
            if (TryFormNewGroupWithoutBreakingExisting(item, adjacentItems))
            {
                Debug.LogWarning($"FormNewGroup");
                return;
            }
        }

        // If we get here, we need to do a complete rebuild, but with strict
        // preservation of existing bonuses
        RebuildWithStrictBonusPreservation(item);
    }

    // Remove an item from inventory
    public void RemoveItemFromGroups(DraggableItem item)
    {
        if (item == null) return;

        int groupId = item.GetConnectionGroupId();

        // If item is in a group, handle removal
        if (groupId != -1 && connectedGroups.ContainsKey(groupId))
        {
            List<DraggableItem> group = connectedGroups[groupId];

            // Remove item from group
            group.Remove(item);
            item.ClearConnectionGroup();
            item.SetBonus(null, ItemConnectedState.Empty);

            // If group still has 2+ items, check if bonus is still valid
            if (group.Count >= 2)
            {
                List<string> itemIds = group.Select(i => i.GetItemData().itemID).ToList();
                BonusData bonus = Database.Instance.GetBestBonusData(itemIds);

                if (bonus != null && IsGroupConnected(group))
                {
                    // Update group with new bonus
                    groupBonuses[groupId] = bonus;

                    // Update all items
                    foreach (var groupItem in group)
                    {
                        List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(itemIds);
                        ItemConnectedState state = higherBonuses.Count > 0 ?
                            ItemConnectedState.Opened : ItemConnectedState.Closed;

                        groupItem.SetBonus(bonus, state);
                    }
                }
                else
                {
                    // No valid bonus, remove group
                    foreach (var groupItem in group)
                    {
                        groupItem.ClearConnectionGroup();
                        groupItem.SetBonus(null, ItemConnectedState.Empty);
                    }

                    connectedGroups.Remove(groupId);
                    groupBonuses.Remove(groupId);
                }
            }
            else if (group.Count == 1)
            {
                // Only one item left, remove group
                DraggableItem remainingItem = group[0];
                remainingItem.ClearConnectionGroup();
                remainingItem.SetBonus(null, ItemConnectedState.Empty);

                connectedGroups.Remove(groupId);
                groupBonuses.Remove(groupId);
            }
            else
            {
                // No items left, remove group
                connectedGroups.Remove(groupId);
                groupBonuses.Remove(groupId);
            }

            NotifyBonusChanges();
        }
    }
    #endregion

    #region Public helper methods
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
    #endregion

    private bool TryAddToExistingGroup(DraggableItem item, int groupId)
    {
        if (!connectedGroups.ContainsKey(groupId))
            return false;

        // Don't add to closed groups
        if (connectedGroups[groupId][0].ConnectedState == ItemConnectedState.Closed)
            return false;

        List<DraggableItem> group = connectedGroups[groupId];
        List<string> currentIds = group.Select(i => i.GetItemData().itemID).ToList();
        List<string> newIds = new List<string>(currentIds) { item.GetItemData().itemID };

        // Check if adding this item would form a valid bonus
        BonusData newBonus = Database.Instance.GetBestBonusData(newIds);
        if (newBonus == null)
            return false;

        // Update the group
        List<DraggableItem> newGroup = new List<DraggableItem>(group) { item };
        connectedGroups[groupId] = newGroup;
        groupBonuses[groupId] = newBonus;

        // Update the item
        item.SetConnectionGroupId(groupId);

        // Set bonus state
        List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(newIds);
        ItemConnectedState state = higherBonuses.Count > 0 ?
            ItemConnectedState.Opened : ItemConnectedState.Closed;

        item.SetBonus(newBonus, state);

        // Update other items in group
        foreach (var groupItem in group)
        {
            groupItem.SetBonus(newBonus, state);
        }

        NotifyBonusChanges();
        return true;
    }

    private bool TryFormNewGroupWithoutBreakingExisting(DraggableItem item, List<DraggableItem> adjacentItems)
    {
        // Find adjacent items that aren't part of any bonus
        List<DraggableItem> availableItems = new List<DraggableItem>();

        foreach (var adjItem in adjacentItems)
        {
            if (adjItem.GetConnectionGroupId() == -1)
            {
                availableItems.Add(adjItem);
            }
        }

        // No available items to form a group
        if (availableItems.Count == 0)
            return false;

        // Try to form a bonus with available items
        List<DraggableItem> potential = new List<DraggableItem> { item };

        // Try adding all available item
        foreach (var adjItem in adjacentItems)
        {
            potential.Add(adjItem);
        }

        // Check if this forms a valid bonus
        List<string> ids = potential.Select(i => i.GetItemData().itemID).ToList();
        BonusData bonus = Database.Instance.GetBestBonusData(ids);

        if (bonus != null)
        {
            // Create new group
            int groupId = ++groupCounter;
            connectedGroups[groupId] = new List<DraggableItem>(potential);
            groupBonuses[groupId] = bonus;

            // Set group on all items
            foreach (var potentialItem in potential)
            {
                potentialItem.SetConnectionGroupId(groupId);

                // Determine connection state
                List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(ids);
                ItemConnectedState state = higherBonuses.Count > 0 ?
                    ItemConnectedState.Opened : ItemConnectedState.Closed;

                potentialItem.SetBonus(bonus, state);
            }

            NotifyBonusChanges();
            return true;
        }

        // Clear potential
        potential.Clear();
        potential.Add(item);

        // Try adding each available item
        foreach (var availableItem in availableItems)
        {
            potential.Add(availableItem);

            // Check if this forms a valid bonus
            ids = potential.Select(i => i.GetItemData().itemID).ToList();
            bonus = Database.Instance.GetBestBonusData(ids);

            if (bonus != null)
            {
                // Create new group
                int groupId = ++groupCounter;
                connectedGroups[groupId] = new List<DraggableItem>(potential);
                groupBonuses[groupId] = bonus;

                // Set group on all items
                foreach (var potentialItem in potential)
                {
                    potentialItem.SetConnectionGroupId(groupId);

                    // Determine connection state
                    List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(ids);
                    ItemConnectedState state = higherBonuses.Count > 0 ?
                        ItemConnectedState.Opened : ItemConnectedState.Closed;

                    potentialItem.SetBonus(bonus, state);
                }

                NotifyBonusChanges();
                return true;
            }
        }

        return false;
    }

    private void RebuildWithStrictBonusPreservation(DraggableItem newItem)
    {
        // Save all existing bonus groups before rebuilding
        Dictionary<int, List<DraggableItem>> existingBonusGroups = new Dictionary<int, List<DraggableItem>>();
        Dictionary<int, BonusData> existingBonuses = new Dictionary<int, BonusData>();

        foreach (var kvp in connectedGroups)
        {
            // Only preserve groups with valid bonuses
            if (groupBonuses.ContainsKey(kvp.Key) &&
                kvp.Value[0].GetBonusData() != null)
            {
                existingBonusGroups[kvp.Key] = new List<DraggableItem>(kvp.Value);
                existingBonuses[kvp.Key] = groupBonuses[kvp.Key];
            }
        }

        // Clear connections for the new item
        if (newItem.GetConnectionGroupId() != -1)
        {
            int groupId = newItem.GetConnectionGroupId();
            if (connectedGroups.ContainsKey(groupId))
            {
                connectedGroups[groupId].Remove(newItem);
            }
            newItem.ClearConnectionGroup();
            newItem.SetBonus(null, ItemConnectedState.Empty);
        }

        // Get all items not in existing bonuses
        HashSet<int> preservedItemIds = new HashSet<int>();
        foreach (var group in existingBonusGroups.Values)
        {
            foreach (var item in group)
            {
                preservedItemIds.Add(item.GetInstanceID());
            }
        }

        List<DraggableItem> freeItems = GetAllItemsInGrid()
            .Where(item => !preservedItemIds.Contains(item.GetInstanceID()))
            .ToList();

        // Clear these free items
        foreach (var item in freeItems)
        {
            if (item.GetConnectionGroupId() != -1)
            {
                int groupId = item.GetConnectionGroupId();
                if (connectedGroups.ContainsKey(groupId))
                {
                    connectedGroups[groupId].Remove(item);
                }
            }
            item.ClearConnectionGroup();
            item.SetBonus(null, ItemConnectedState.Empty);
        }

        // Try to form new groups with free items
        List<List<DraggableItem>> components = FindConnectedComponents(freeItems);
        List<BonusGroupCandidate> candidates = new List<BonusGroupCandidate>();

        foreach (var component in components)
        {
            bool isLinear = component.Count >= 3 && IsLinearArrangement(component);

            if (isLinear)
            {
                var sortedItems = SortItemsByPosition(component);
                var linearCandidates = FindOptimalPartitioning(sortedItems);
                candidates.AddRange(linearCandidates);
            }
            else
            {
                var subsetCandidates = FindAllValidSubsets(component);
                candidates.AddRange(subsetCandidates);
            }
        }

        // Assign groups from candidates
        HashSet<int> assignedItems = new HashSet<int>();

        // First, restore existing groups
        foreach (var kvp in existingBonusGroups)
        {
            int groupId = kvp.Key;
            List<DraggableItem> group = kvp.Value;
            BonusData bonus = existingBonuses[groupId];

            connectedGroups[groupId] = group;
            groupBonuses[groupId] = bonus;

            foreach (var item in group)
            {
                assignedItems.Add(item.GetInstanceID());

                // Restore the exact same state
                item.SetConnectionGroupId(groupId);
                item.SetBonus(bonus, item.ConnectedState);
            }
        }

        // Then assign new groups
        foreach (var candidate in candidates)
        {
            // Skip if any item is already assigned
            if (candidate.Items.Any(item => assignedItems.Contains(item.GetInstanceID())))
                continue;

            // Create new group
            int groupId = ++groupCounter;
            connectedGroups[groupId] = new List<DraggableItem>(candidate.Items);
            groupBonuses[groupId] = candidate.Bonus;

            // Assign items to group
            foreach (var item in candidate.Items)
            {
                item.SetConnectionGroupId(groupId);

                // Determine connection state
                List<string> itemIds = candidate.Items.Select(i => i.GetItemData().itemID).ToList();
                List<BonusData> higherBonuses = Database.Instance.GetHigherBonusDatas(itemIds);

                ItemConnectedState state = ItemConnectedState.Closed;
                if (higherBonuses.Count > 0)
                {
                    bool hasDirectUpgradePath = higherBonuses.Any(bonus => {
                        List<string> requiredIds = Database.Instance.GetRequiredItemsForBonus(bonus);
                        return requiredIds.Count == itemIds.Count + 1 &&
                               itemIds.All(id => requiredIds.Contains(id));
                    });

                    state = hasDirectUpgradePath ? ItemConnectedState.Opened : ItemConnectedState.Closed;
                }

                item.SetBonus(candidate.Bonus, state);
                assignedItems.Add(item.GetInstanceID());
            }
        }

        NotifyBonusChanges();
    }


    private void CheckGroupCandidate(List<DraggableItem> items, List<BonusGroupCandidate> candidates)
    {
        // Verify the group is connected
        if (!IsGroupConnected(items))
            return;

        // Get item IDs for bonus lookup
        List<string> itemIds = items.Select(item => item.GetItemData().itemID).ToList();

        // Check for valid bonus
        BonusData bonus = Database.Instance.GetBestBonusData(itemIds);

        if (bonus != null)
        {
            // Check if this matches an existing group
            bool isExistingGroup = false;
            int existingGroupId = -1;

            // Get set of item instance IDs
            HashSet<int> itemInstanceIds = new HashSet<int>(
                items.Select(item => item.GetInstanceID()));

            // Check against existing groups
            foreach (var kvp in connectedGroups)
            {
                if (kvp.Value.Count != items.Count)
                    continue;

                // Check if all items match
                bool allMatch = true;
                foreach (var groupItem in kvp.Value)
                {
                    if (!itemInstanceIds.Contains(groupItem.GetInstanceID()))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    isExistingGroup = true;
                    existingGroupId = kvp.Key;
                    break;
                }
            }

            // Create a candidate with EarliestPlacement and existing group status
            candidates.Add(new BonusGroupCandidate
            {
                Items = items,
                Bonus = bonus,
                BonusPriority = bonus.priority,
                EarliestPlacement = items.Min(item => item.PlacementOrder),
                IsExistingGroup = isExistingGroup,
                ExistingGroupId = existingGroupId
            });
        }
    }

    // Find all connected components in the grid
    private List<List<DraggableItem>> FindConnectedComponents(List<DraggableItem> allItems)
    {
        List<List<DraggableItem>> components = new List<List<DraggableItem>>();
        HashSet<int> visited = new HashSet<int>();

        foreach (var item in allItems)
        {
            // Skip if already visited
            if (visited.Contains(item.GetInstanceID()))
                continue;

            // Start a new component
            List<DraggableItem> component = new List<DraggableItem>();
            Queue<DraggableItem> queue = new Queue<DraggableItem>();

            queue.Enqueue(item);
            visited.Add(item.GetInstanceID());

            // BFS to find all connected items
            while (queue.Count > 0)
            {
                DraggableItem current = queue.Dequeue();
                component.Add(current);

                // Find all adjacent items
                foreach (var other in allItems)
                {
                    if (visited.Contains(other.GetInstanceID()))
                        continue;

                    if (AreItemsAdjacent(current, other))
                    {
                        queue.Enqueue(other);
                        visited.Add(other.GetInstanceID());
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }


    // Find all possible subsets of a component that form valid bonuses
    private List<BonusGroupCandidate> FindAllValidSubsets(List<DraggableItem> component)
    {
        List<BonusGroupCandidate> candidates = new List<BonusGroupCandidate>();

        // Special handling for small components
        if (component.Count <= 3)
        {
            CheckGroupCandidate(component, candidates);
            return candidates;
        }

        // Try all connected subgroups up to a reasonable size
        // Start with larger groups first
        for (int size = Mathf.Min(6, component.Count); size >= 2; size--)
        {
            FindConnectedSubgroups(component, size, candidates);
        }

        return candidates;
    }

    // Find all connected subgroups of a specific size
    private void FindConnectedSubgroups(List<DraggableItem> items, int size, List<BonusGroupCandidate> candidates)
    {
        // Build adjacency graph
        Dictionary<DraggableItem, List<DraggableItem>> graph = new Dictionary<DraggableItem, List<DraggableItem>>();
        foreach (var item in items)
        {
            graph[item] = new List<DraggableItem>();
        }

        for (int i = 0; i < items.Count; i++)
        {
            for (int j = i + 1; j < items.Count; j++)
            {
                if (AreItemsAdjacent(items[i], items[j]))
                {
                    graph[items[i]].Add(items[j]);
                    graph[items[j]].Add(items[i]);
                }
            }
        }

        // For each item, try to find connected subgroups starting from it
        foreach (var startItem in items)
        {
            List<DraggableItem> currentGroup = new List<DraggableItem> { startItem };
            FindSubgroupsDFS(startItem, currentGroup, size, graph, new HashSet<int>(), candidates);
        }
    }

    // DFS to find connected subgroups
    private void FindSubgroupsDFS(
        DraggableItem current,
        List<DraggableItem> currentGroup,
        int targetSize,
        Dictionary<DraggableItem, List<DraggableItem>> graph,
        HashSet<int> visited,
        List<BonusGroupCandidate> candidates)
    {
        // Mark current item as visited in this DFS path
        visited.Add(current.GetInstanceID());

        // If we've reached the target size, check this group
        if (currentGroup.Count == targetSize)
        {
            // Check if this combination forms a valid bonus
            CheckGroupCandidate(new List<DraggableItem>(currentGroup), candidates);

            // Remove from visited so we can use it in other paths
            visited.Remove(current.GetInstanceID());
            return;
        }

        // Try adding each neighbor
        foreach (var neighbor in graph[current])
        {
            // Skip if already in the current group
            if (visited.Contains(neighbor.GetInstanceID()))
                continue;

            // Add to current group and continue DFS
            currentGroup.Add(neighbor);
            FindSubgroupsDFS(neighbor, currentGroup, targetSize, graph, visited, candidates);
            currentGroup.Remove(neighbor);
        }

        // Remove from visited so we can use it in other paths
        visited.Remove(current.GetInstanceID());
    }

    // Verify a group is fully connected
    private bool IsGroupConnected(List<DraggableItem> items)
    {
        if (items.Count <= 1)
            return true;

        // Use BFS to check connectivity
        HashSet<int> visited = new HashSet<int>();
        Queue<DraggableItem> queue = new Queue<DraggableItem>();

        queue.Enqueue(items[0]);
        visited.Add(items[0].GetInstanceID());

        while (queue.Count > 0)
        {
            DraggableItem current = queue.Dequeue();

            foreach (var other in items)
            {
                if (visited.Contains(other.GetInstanceID()))
                    continue;

                if (AreItemsAdjacent(current, other))
                {
                    queue.Enqueue(other);
                    visited.Add(other.GetInstanceID());
                }
            }
        }

        // All items should be visited if the group is connected
        return visited.Count == items.Count;
    }

    // Check if items form a linear arrangement (horizontal or vertical)
    private bool IsLinearArrangement(List<DraggableItem> items)
    {
        // Need at least 3 items for a meaningful linear arrangement
        if (items.Count < 3) return false;

        // Get center positions of all items
        List<Vector2Int> positions = items.Select(item => GetCenterPosition(item)).ToList();

        // Check if horizontal line
        bool horizontalLine = true;
        int firstY = positions[0].y;
        foreach (var pos in positions)
        {
            if (pos.y != firstY)
            {
                horizontalLine = false;
                break;
            }
        }

        // Check if vertical line
        bool verticalLine = true;
        int firstX = positions[0].x;
        foreach (var pos in positions)
        {
            if (pos.x != firstX)
            {
                verticalLine = false;
                break;
            }
        }

        return horizontalLine || verticalLine;
    }

    // Get center position of an item
    private Vector2Int GetCenterPosition(DraggableItem item)
    {
        // For multi-cell items, calculate center from occupied grids
        if (item.OccupiedGrids.Count > 0)
        {
            int sumX = 0, sumY = 0;
            foreach (var pos in item.OccupiedGrids)
            {
                sumX += pos.x;
                sumY += pos.y;
            }
            return new Vector2Int(sumX / item.OccupiedGrids.Count, sumY / item.OccupiedGrids.Count);
        }

        // Default to grid position for single cell items
        return new Vector2Int(item.GridX, item.GridY);
    }

    // Sort items by position (left to right or top to bottom)
    private List<DraggableItem> SortItemsByPosition(List<DraggableItem> items)
    {
        // Get positions
        List<Vector2Int> positions = items.Select(item => GetCenterPosition(item)).ToList();

        // Check orientation
        bool horizontalLine = true;
        int firstY = positions[0].y;
        foreach (var pos in positions)
        {
            if (pos.y != firstY)
            {
                horizontalLine = false;
                break;
            }
        }

        // Sort based on orientation
        if (horizontalLine)
        {
            // Sort horizontally (left to right)
            return items.OrderBy(item => GetCenterPosition(item).x).ToList();
        }
        else
        {
            // Sort vertically (top to bottom)
            return items.OrderBy(item => GetCenterPosition(item).y).ToList();
        }
    }

    // Dynamic programming approach for finding optimal partitioning of linear arrangements
    private List<BonusGroupCandidate> FindOptimalPartitioning(List<DraggableItem> items)
    {
        int n = items.Count;

        // dp[i] stores optimal partition for items[0...i-1]
        List<List<BonusGroupCandidate>> dp = new List<List<BonusGroupCandidate>>();
        for (int i = 0; i <= n; i++)
        {
            dp.Add(new List<BonusGroupCandidate>());
        }

        // Fill dp table
        for (int end = 1; end <= n; end++)
        {
            List<BonusGroupCandidate> bestPartition = null;
            int bestScore = int.MaxValue;

            // Try all possible last groups
            for (int start = end - 1; start >= 0; start--)
            {
                // Check if items[start...end-1] form a valid group
                List<DraggableItem> group = items.GetRange(start, end - start);
                List<BonusGroupCandidate> groupCandidates = new List<BonusGroupCandidate>();
                CheckGroupCandidate(group, groupCandidates);

                if (groupCandidates.Count > 0)
                {
                    // Combine with best solution for items[0...start-1]
                    List<BonusGroupCandidate> candidatePartition = new List<BonusGroupCandidate>(dp[start]);
                    candidatePartition.AddRange(groupCandidates);

                    // Calculate score (lower is better)
                    int score = candidatePartition.Sum(c => c.BonusPriority);

                    if (bestPartition == null || score < bestScore)
                    {
                        bestPartition = candidatePartition;
                        bestScore = score;
                    }
                }
            }

            // Store best partition
            if (bestPartition != null)
            {
                dp[end] = bestPartition;
            }
            else
            {
                // No valid partition found for this prefix
                dp[end] = new List<BonusGroupCandidate>(dp[end - 1]);
            }
        }

        return dp[n];
    }


    // Helper to get all items in the grid
    private List<DraggableItem> GetAllItemsInGrid()
    {
        List<DraggableItem> result = new List<DraggableItem>();
        HashSet<int> processed = new HashSet<int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsOccupied(pos))
                {
                    DraggableItem item = GetItemAt(pos);
                    if (item != null && !processed.Contains(item.GetInstanceID()))
                    {
                        result.Add(item);
                        processed.Add(item.GetInstanceID());
                    }
                }
            }
        }

        return result;
    }

    // Check if two items are adjacent
    private bool AreItemsAdjacent(DraggableItem item1, DraggableItem item2)
    {
        // Check each occupied grid cell from first item against each from second item
        foreach (Vector2Int pos1 in item1.OccupiedGrids)
        {
            foreach (Vector2Int pos2 in item2.OccupiedGrids)
            {
                // Check if positions are orthogonally adjacent (Manhattan distance = 1)
                int dx = Mathf.Abs(pos1.x - pos2.x);
                int dy = Mathf.Abs(pos1.y - pos2.y);

                // They're adjacent if they're one step away horizontally or vertically
                if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Helper to get adjacent items
    private List<DraggableItem> GetAdjacentItems(DraggableItem item)
    {
        List<DraggableItem> adjacentItems = new List<DraggableItem>();

        foreach (Vector2Int grid in item.OccupiedGrids)
        {
            List<Vector2Int> directions = new List<Vector2Int> {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjacentPos = grid + dir;

                if (IsValidPosition(adjacentPos) && IsOccupied(adjacentPos))
                {
                    DraggableItem adjacentItem = GetItemAt(adjacentPos);
                    if (adjacentItem != null && adjacentItem != item && adjacentItem.IsOpenToConnect() && !adjacentItems.Contains(adjacentItem))
                    {
                        adjacentItems.Add(adjacentItem);
                        Debug.LogWarning($"Found item {adjacentItem.GetItemData().itemName} at pos [{adjacentPos.x}, {adjacentPos.y}]");
                    }
                }
            }
        }

        return adjacentItems;
    }

    private bool IsBonus(DraggableItem item)
    {
        return item.GetConnectionGroupId() != -1 && item.GetBonusData() != null;
    }


    // Notify UI of bonus changes
    private void NotifyBonusChanges()
    {
        UIInventory.OnBonusChanged?.Invoke(GetCurrentBonuses());
    }
}