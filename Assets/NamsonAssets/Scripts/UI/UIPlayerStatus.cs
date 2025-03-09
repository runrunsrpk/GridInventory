using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIPlayerStatus : MonoBehaviour
{
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text bonusText;

    private int currentAttack = 0;
    private int currentDefense = 0;
    private int currentHealth = 0;

    private void OnEnable()
    {
        UIInventory.OnBonusChanged += OnBonusChanged;
    }

    private void OnDisable()
    {
        UIInventory.OnBonusChanged -= OnBonusChanged;
    }

    private void Start()
    {
        attackText.text = "0";
        defenseText.text = "0";
        healthText.text = "0";
        bonusText.text = "";
    }

    private void UpdatePlayerStats()
    {
        attackText.text = (currentAttack > 0) ? $"+{currentAttack}" : "0";
        defenseText.text = (currentDefense > 0) ? $"+{currentDefense}" : "0";
        healthText.text = (currentHealth > 0) ? $"+{currentHealth}" : "0";
    }

    private void UpdateBonusesText(string text)
    {
        bonusText.text = text;
    }

    private void OnBonusChanged(List<BonusData> bonuses)
    {
        string result = "";
        if (bonuses.Count > 0)
        {
            foreach (BonusData bonus in bonuses.Take(bonuses.Count - 1))
            {
                result += $"{bonus.bonusName}+";
            }
            result += $"{bonuses.Last().bonusName}";
        }
        UpdateBonusesText(result);

        ResetCurrentStats();
        foreach (BonusData bonus in bonuses)
        {
            currentAttack += bonus.bonusStatus.attackBonus;
            currentDefense += bonus.bonusStatus.defenseBonus;
            currentHealth += bonus.bonusStatus.healthBonus;
        }
        UpdatePlayerStats();
    }

    private void ResetCurrentStats()
    {
        currentAttack = 0;
        currentDefense = 0;
        currentHealth = 0;
    }
}
