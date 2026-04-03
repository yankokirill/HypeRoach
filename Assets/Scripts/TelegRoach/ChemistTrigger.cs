using UnityEngine;
using UnityEngine.UI;

public class ChemistTrigger : MonoBehaviour
{
    [Header("Кнопка-невидимка (над полем ввода)")]
    public Button invisibleTrigger;

    [Header("Кнопка Химика (которая должна появиться)")]
    public GameObject chemistButton;

    void Start()
    {
        // При старте скрываем кнопку химика
        if (chemistButton != null)
            chemistButton.SetActive(false);

        // Подписываемся на клик
        if (invisibleTrigger != null)
            invisibleTrigger.onClick.AddListener(ToggleChemistButton);
    }

    public void ToggleChemistButton()
    {
        if (chemistButton != null)
        {
            // Инвертируем состояние: если была выключена — включится, и наоборот
            bool isActive = chemistButton.activeSelf;
            chemistButton.SetActive(!isActive);

            Debug.Log(isActive ? "Химик спрятан" : "Химик показан");
        }
    }
}
