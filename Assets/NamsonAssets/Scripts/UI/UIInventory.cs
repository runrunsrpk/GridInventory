using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour
{
    [Header("Inventory Setting")]
    [SerializeField, Range(2, 5)] private int row;
    [SerializeField, Range(5, 8)] private int column;
    [SerializeField, Range(8.0f, 99.99f)] private float maxWeight = 8.0f;
    [SerializeField] private UIInventorySlot slotPrefab;
    [SerializeField] private Transform slotParent;

    [Header("UI Setting")]
    [SerializeField] private TMP_Text currentWeightText;
    [SerializeField] private TMP_Text maxWeightText;

    private float currentWeight = 0.0f;

    private void Start()
    {
        SpawnInventorySlot();
        SetCurrentWeight(currentWeight);
        SetMaxWeight(maxWeight);
    }

    private void SpawnInventorySlot()
    {
        slotParent.GetComponent<GridLayoutGroup>().constraintCount = column;

        for (int x = 0; x < row; x++)
        {
            for (int y = 0; y < column; y++)
            {
                UIInventorySlot slot = Instantiate(slotPrefab, slotParent);
            }
        }
    }

    private void SetCurrentWeight(float weight)
    {
        currentWeight = weight;
        currentWeightText.text = weight.ToString("n2");

    }

    private void SetMaxWeight(float weight)
    {
        maxWeight = weight;
        maxWeightText.text = weight.ToString("n2");
    }
}
