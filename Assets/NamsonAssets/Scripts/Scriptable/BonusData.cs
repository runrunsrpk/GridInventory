using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBonus", menuName = "Inventory/Bonus")]
public class BonusData : ScriptableObject
{
    public string bonusID;
    public string bonusName;
    public BonusStatus bonusStatus;
    public List<string> bonusRequirement;
    public int priority;
}

[System.Serializable]
public class BonusStatus
{
    public int attackBonus;
    public int defenseBonus;
    public int healthBonus;
}
