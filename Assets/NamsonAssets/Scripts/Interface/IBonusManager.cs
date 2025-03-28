using System.Collections.Generic;
using UnityEngine;

public interface IBonusManager
{
    public void CheckConnections(Vector2Int position);
    public List<BonusData> GetCurrentBonuses();
}
