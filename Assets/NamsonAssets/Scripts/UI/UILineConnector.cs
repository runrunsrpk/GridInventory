using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineConnector : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 5f;
    [SerializeField] private Sprite lineSprite; // Use a simple white sprite
    [SerializeField] private Transform lineParent;

    // Line ordering to prevent flickering
    [SerializeField] private int baseSortingOrder = 10;

    // Colors for different states
    [Header("Line Colors")]
    [SerializeField] private Color waitingColor = Color.white;
    [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f); // Gold color for opened/closed

    // Track connections for each group to preserve them
    private Dictionary<int, List<LineConnection>> groupConnections = new Dictionary<int, List<LineConnection>>();
    private Dictionary<int, ItemConnectedState> groupStates = new Dictionary<int, ItemConnectedState>();

    // Cache previous groups to detect changes
    private Dictionary<int, HashSet<int>> previousGroups = new Dictionary<int, HashSet<int>>();

    private Queue<Image> linePool = new Queue<Image>();
    private BonusManager bonusManager;
    private Canvas parentCanvas;

    // Class to track line connections between items
    private class LineConnection
    {
        public DraggableItem Item1;
        public DraggableItem Item2;
        public Image LineImage;

        // Cache the connection's key for fast lookup
        public string ConnectionKey => GetConnectionKey(Item1, Item2);

        // Get unique connection key between two items
        public static string GetConnectionKey(DraggableItem a, DraggableItem b)
        {
            // Ensure consistent ordering regardless of parameter order
            int id1 = a.GetInstanceID();
            int id2 = b.GetInstanceID();

            if (id1 < id2)
                return $"{id1}_{id2}";
            else
                return $"{id2}_{id1}";
        }
    }

    // Initialize with references
    public void Initialize(BonusManager bonusManager)
    {
        this.bonusManager = bonusManager;
        parentCanvas = GetComponentInParent<Canvas>();

        // Create line parent if needed
        if (lineParent == null)
        {
            GameObject lineParentObj = new GameObject("LineParent");
            lineParentObj.transform.SetParent(transform);
            lineParentObj.AddComponent<RectTransform>();
            lineParent = lineParentObj.transform;
        }

        // Subscribe to bonus changed event
        UIInventory.OnBonusChanged += OnBonusChanged;
    }

    private void OnDestroy()
    {
        // Prevent memory leaks
        UIInventory.OnBonusChanged -= OnBonusChanged;
    }

    // Update lines when bonuses change
    private void OnBonusChanged(List<BonusData> bonuses)
    {
        UpdateConnectionLines();
    }

    // Public method to force update all lines (called externally)
    public void DrawAllConnectionLines()
    {
        ClearAllLines();
        groupConnections.Clear();
        groupStates.Clear();
        previousGroups.Clear();

        UpdateConnectionLines();
    }

    // Update connection lines based on current groups
    private void UpdateConnectionLines()
    {
        var groups = bonusManager.GetConnectedGroups();
        HashSet<int> processedGroups = new HashSet<int>();

        // Process all groups
        foreach (var group in groups)
        {
            int groupId = group.Key;
            List<DraggableItem> items = group.Value;

            // Skip if no items or only one item
            if (items == null || items.Count < 2)
                continue;

            processedGroups.Add(groupId);

            ItemConnectedState currentState = items[0].ConnectedState;

            // Check if this is an existing group
            if (groupConnections.ContainsKey(groupId))
            {
                // Check if state has changed
                if (groupStates.ContainsKey(groupId) && groupStates[groupId] != currentState)
                {
                    // Update color for all connections
                    UpdateGroupColor(groupId, currentState);
                }

                // For closed groups, preserve connections
                if (currentState == ItemConnectedState.Closed)
                {
                    // Skip further processing for closed groups - keep existing connections
                    continue;
                }

                // For open groups, check if items were added or removed
                if (previousGroups.ContainsKey(groupId))
                {
                    HashSet<int> previousItems = previousGroups[groupId];
                    HashSet<int> currentItems = new HashSet<int>();

                    foreach (var item in items)
                    {
                        currentItems.Add(item.GetInstanceID());
                    }

                    // If items were added
                    if (currentItems.Count > previousItems.Count)
                    {
                        // Find added items
                        List<DraggableItem> addedItems = new List<DraggableItem>();
                        List<DraggableItem> existingItems = new List<DraggableItem>();

                        foreach (var item in items)
                        {
                            if (!previousItems.Contains(item.GetInstanceID()))
                                addedItems.Add(item);
                            else
                                existingItems.Add(item);
                        }

                        // Connect new items to closest existing items
                        foreach (var newItem in addedItems)
                        {
                            ConnectToClosestItem(newItem, existingItems, groupId, currentState);
                        }

                        // Update cached group
                        previousGroups[groupId] = currentItems;
                        continue;
                    }
                }
            }

            // For new groups or complete recalculation
            RebuildGroupConnections(groupId, items, currentState);
        }

        // Remove connections for groups that no longer exist
        List<int> groupsToRemove = new List<int>();

        foreach (int groupId in groupConnections.Keys)
        {
            if (!processedGroups.Contains(groupId))
            {
                groupsToRemove.Add(groupId);
            }
        }

        foreach (int groupId in groupsToRemove)
        {
            RemoveGroupConnections(groupId);
        }
    }

    // Connect a new item to the closest existing item in a group
    private void ConnectToClosestItem(DraggableItem newItem, List<DraggableItem> existingItems, int groupId, ItemConnectedState state)
    {
        if (existingItems.Count == 0)
            return;

        DraggableItem closest = null;
        float minDistance = float.MaxValue;

        // Find closest existing item
        foreach (var existingItem in existingItems)
        {
            float distance = Vector2.Distance(
                GetItemCenter(newItem),
                GetItemCenter(existingItem));

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = existingItem;
            }
        }

        if (closest != null)
        {
            // Create connection
            Image line = GetLineFromPool();
            LineConnection connection = new LineConnection
            {
                Item1 = newItem,
                Item2 = closest,
                LineImage = line
            };

            // Update line position and appearance
            Color color = state == ItemConnectedState.Waiting ? waitingColor : goldColor;
            UpdateLinePosition(connection, color);

            // Add to group connections
            if (!groupConnections.ContainsKey(groupId))
            {
                groupConnections[groupId] = new List<LineConnection>();
            }

            groupConnections[groupId].Add(connection);
            groupStates[groupId] = state;
        }
    }

    // Update group's line colors
    private void UpdateGroupColor(int groupId, ItemConnectedState newState)
    {
        if (!groupConnections.ContainsKey(groupId))
            return;

        Color color = newState == ItemConnectedState.Waiting ? waitingColor : goldColor;

        foreach (var connection in groupConnections[groupId])
        {
            if (connection.LineImage != null)
            {
                connection.LineImage.color = color;
            }
        }

        groupStates[groupId] = newState;
    }

    // Rebuild all connections for a group using MST algorithm
    private void RebuildGroupConnections(int groupId, List<DraggableItem> items, ItemConnectedState state)
    {
        // Remove existing connections for this group
        RemoveGroupConnections(groupId);

        // Create minimum spanning tree
        List<LineConnection> connections = CreateMinimumSpanningTree(items, state);

        // Store connections
        groupConnections[groupId] = connections;
        groupStates[groupId] = state;

        // Cache item IDs for this group
        HashSet<int> itemIds = new HashSet<int>();
        foreach (var item in items)
        {
            itemIds.Add(item.GetInstanceID());
        }
        previousGroups[groupId] = itemIds;
    }

    // Create minimum spanning tree connections
    private List<LineConnection> CreateMinimumSpanningTree(List<DraggableItem> items, ItemConnectedState state)
    {
        // Set color based on state
        Color lineColor = state == ItemConnectedState.Waiting ? waitingColor : goldColor;

        List<LineConnection> result = new List<LineConnection>();
        if (items.Count < 2) return result;

        HashSet<int> connected = new HashSet<int>();
        List<DraggableItem> remaining = new List<DraggableItem>(items);

        // Start with first item
        connected.Add(remaining[0].GetInstanceID());
        DraggableItem firstItem = remaining[0];
        remaining.RemoveAt(0);

        // Build MST
        while (remaining.Count > 0)
        {
            float minDistance = float.MaxValue;
            DraggableItem closestConnected = null;
            DraggableItem closestRemaining = null;
            int closestRemainingIndex = -1;

            // Find closest pair between connected and remaining
            foreach (var item in items)
            {
                if (!connected.Contains(item.GetInstanceID()))
                    continue;

                for (int i = 0; i < remaining.Count; i++)
                {
                    float distance = Vector2.Distance(
                        GetItemCenter(item),
                        GetItemCenter(remaining[i]));

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestConnected = item;
                        closestRemaining = remaining[i];
                        closestRemainingIndex = i;
                    }
                }
            }

            if (closestConnected != null && closestRemaining != null)
            {
                // Create connection
                Image line = GetLineFromPool();
                LineConnection connection = new LineConnection
                {
                    Item1 = closestConnected,
                    Item2 = closestRemaining,
                    LineImage = line
                };

                // Update line position
                UpdateLinePosition(connection, lineColor);

                // Add to results
                result.Add(connection);

                // Update collections
                connected.Add(closestRemaining.GetInstanceID());
                remaining.RemoveAt(closestRemainingIndex);
            }
            else
            {
                // Should not happen, but break to avoid infinite loop
                break;
            }
        }

        return result;
    }

    // Update line position between two items
    private void UpdateLinePosition(LineConnection connection, Color color)
    {
        if (connection == null || connection.LineImage == null)
            return;

        Vector2 start = GetItemCenter(connection.Item1);
        Vector2 end = GetItemCenter(connection.Item2);

        // Get RectTransform
        RectTransform rectTransform = connection.LineImage.rectTransform;

        // Calculate position - center is the midpoint
        rectTransform.position = (start + end) / 2;

        // Calculate size - width is the distance, height is the lineWidth
        float distance = Vector2.Distance(start, end);
        rectTransform.sizeDelta = new Vector2(distance, lineWidth);

        // Calculate angle between points
        Vector2 direction = (end - start).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        // Set color
        connection.LineImage.color = color;
    }

    // Get center position of an item in screen/world coordinates
    private Vector2 GetItemCenter(DraggableItem item)
    {
        if (item == null)
            return Vector2.zero;

        // Get RectTransform
        RectTransform rectTransform = item.GetComponent<RectTransform>();
        if (rectTransform == null)
            return Vector2.zero;

        // Calculate center in world space
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        return new Vector2(
            (corners[0].x + corners[2].x) / 2,
            (corners[0].y + corners[2].y) / 2);
    }

    // Remove all connections for a group
    private void RemoveGroupConnections(int groupId)
    {
        if (!groupConnections.ContainsKey(groupId))
            return;

        foreach (var connection in groupConnections[groupId])
        {
            if (connection.LineImage != null)
            {
                ReturnLineToPool(connection.LineImage);
            }
        }

        groupConnections.Remove(groupId);
        groupStates.Remove(groupId);
        previousGroups.Remove(groupId);
    }

    // Clear all existing lines
    private void ClearAllLines()
    {
        foreach (var connections in groupConnections.Values)
        {
            foreach (var connection in connections)
            {
                if (connection.LineImage != null)
                {
                    ReturnLineToPool(connection.LineImage);
                }
            }
        }

        groupConnections.Clear();
        groupStates.Clear();
    }

    // Get a line from the object pool
    private Image GetLineFromPool()
    {
        if (linePool.Count > 0)
        {
            Image line = linePool.Dequeue();
            line.gameObject.SetActive(true);
            return line;
        }

        return CreateNewLine();
    }

    // Create a new line image
    private Image CreateNewLine()
    {
        GameObject lineObj = new GameObject("ConnectionLine");
        lineObj.transform.SetParent(lineParent);

        // Add RectTransform
        RectTransform rectTransform = lineObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;

        // Add Image component
        Image image = lineObj.AddComponent<Image>();

        // Set sprite
        if (lineSprite != null)
        {
            image.sprite = lineSprite;
        }
        else
        {
            // Create simple white texture
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        // Set raycast behavior
        image.raycastTarget = false;

        // Set sorting order
        Canvas canvas = image.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = baseSortingOrder;

        return image;
    }

    // Return a line to the object pool
    private void ReturnLineToPool(Image line)
    {
        if (line == null) return;

        line.gameObject.SetActive(false);
        linePool.Enqueue(line);
    }
}
