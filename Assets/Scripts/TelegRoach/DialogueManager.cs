using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Content")]
    public DialogueData startingDialogue; // Играет самым первым
    public List<DialogueData> startingDialogues; // Пул для перемешивания

    private List<DialogueData> _shuffledList;
    private int _currentIndex = 0;

    private void Awake()
    {
        // 1. Singleton + DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 2. Делаем шаффл один раз при инициализации
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializePool()
    {
        if (startingDialogues == null || startingDialogues.Count == 0) return;

        // Копируем оригинальный список
        _shuffledList = new List<DialogueData>(startingDialogues);

        // Fisher-Yates Shuffle
        for (int i = 0; i < _shuffledList.Count; i++)
        {
            DialogueData temp = _shuffledList[i];
            int randomIndex = Random.Range(i, _shuffledList.Count);
            _shuffledList[i] = _shuffledList[randomIndex];
            _shuffledList[randomIndex] = temp;
        }

        _currentIndex = 0;
        Debug.Log($"[DialogueManager] Пул перемешан. Количество диалогов: {_shuffledList.Count}");
    }

    private void Start()
    {
        Theme.Initialize();
    }

    public void PlayNext()
    {
        // Если есть конкретный стартовый диалог — играем его и забываем
        if (startingDialogue != null)
        {
            Messenger.Instance.PlayDialogue(startingDialogue);
            startingDialogue = null;
            return;
        }

        if (_shuffledList == null || _shuffledList.Count == 0) return;

        // Если мы дошли до конца перемешанного списка — перемешиваем заново
        if (_currentIndex >= _shuffledList.Count)
        {
            Debug.Log("[DialogueManager] Цикл завершен. Перемешиваем заново.");
            InitializePool();
        }

        // Берем диалог по текущему индексу и двигаем указатель вперед
        DialogueData selected = _shuffledList[_currentIndex];
        _currentIndex++;

        Messenger.Instance.PlayDialogue(selected);
    }
}
