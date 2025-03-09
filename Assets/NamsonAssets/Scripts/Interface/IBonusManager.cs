using System.Collections.Generic;
using UnityEngine;

public interface IBonusManager
{
    public void CheckConnections(Vector2Int position);
    public void RecheckConnections(HashSet<Vector2Int> positions);
    public List<BonusData> GetCurrentBonuses();
}
