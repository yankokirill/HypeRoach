using System.Collections.Generic;
using UnityEngine;


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Special")]
    public DialogueData startingDialogue; // Играет самым первым один раз

    [Header("Outcome Pools")]
    public List<DialogueData> winDialogues;
    public List<DialogueData> lossDialogues;
    public List<DialogueData> superWinDialogues;

    // Списки для перемешанных данных
    private List<DialogueData> _winShuffled;
    private List<DialogueData> _lossShuffled;
    private List<DialogueData> _superWinShuffled;

    // Указатели для каждого списка
    private int _winIndex = 0;
    private int _lossIndex = 0;
    private int _superWinIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAllPools();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Theme.Initialize();
    }

    private void InitializeAllPools()
    {
        _winShuffled = CreateShuffledList(winDialogues);
        _lossShuffled = CreateShuffledList(lossDialogues);
        _superWinShuffled = CreateShuffledList(superWinDialogues);

        _winIndex = 0;
        _lossIndex = 0;
        _superWinIndex = 0;
    }

    // Универсальный метод для создания перемешанной копии списка
    private List<DialogueData> CreateShuffledList(List<DialogueData> original)
    {
        if (original == null || original.Count == 0) return new List<DialogueData>();

        List<DialogueData> shuffled = new List<DialogueData>(original);
        for (int i = 0; i < shuffled.Count; i++)
        {
            DialogueData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        return shuffled;
    }

    // Теперь метод требует указать, какой результат был в гонке
    public void PlayNext(int result)
    {
        // 1. Если есть самый первый диалог — играем его приоритетно
        if (startingDialogue != null)
        {
            Messenger.Instance.PlayDialogue(startingDialogue);
            startingDialogue = null;
            return;
        }

        // 2. Выбираем нужный список и индекс в зависимости от результата
        List<DialogueData> targetList = null;
        int targetIndex = 0;

        if (result > 250)
        {
            if (_superWinIndex >= _superWinShuffled.Count) { _superWinShuffled = CreateShuffledList(superWinDialogues); _superWinIndex = 0; }
            targetList = _superWinShuffled;
            targetIndex = _superWinIndex;
            _superWinIndex++;
        }
        else if (result > 0)
        {
            if (_winIndex >= _winShuffled.Count) { _winShuffled = CreateShuffledList(winDialogues); _winIndex = 0; }
            targetList = _winShuffled;
            targetIndex = _winIndex;
            _winIndex++;
        }
        else
        {
            if (_lossIndex >= _lossShuffled.Count) { _lossShuffled = CreateShuffledList(lossDialogues); _lossIndex = 0; }
            targetList = _lossShuffled;
            targetIndex = _lossIndex;
            _lossIndex++;
        }

        // 3. Запускаем диалог
        if (targetList != null && targetList.Count > 0)
        {
            Messenger.Instance.PlayDialogue(targetList[targetIndex]);
        }
    }
}
