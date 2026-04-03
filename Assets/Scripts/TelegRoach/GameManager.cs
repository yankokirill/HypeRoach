using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public Messenger messenger;

    [Header("Content")]
    [Tooltip("Список возможных начальных диалогов")]
    public List<DialogueData> startingDialogues;

    private void Start()
    {
        Theme.Initialize();
        if (startingDialogues != null && startingDialogues.Count > 0)
        {
            // Выбираем случайный диалог из списка
            int randomIndex = Random.Range(0, startingDialogues.Count);
            DialogueData selected = startingDialogues[randomIndex];

            // Запускаем его через мессенджер
            messenger.PlayDialogue(selected);
        }
    }
}
