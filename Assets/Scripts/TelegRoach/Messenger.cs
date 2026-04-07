using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Game.Base;
using Game.Core;

public class Messenger : MonoBehaviour
{
    public static Messenger Instance;
    public enum Sender { Boss, Chemist, Silent, Player }

    [Header("── Send a Message ──────────────────────────")]
    public Sender selectedSender = Sender.Player;
    [TextArea(2, 6)] public string messageContent = "Type your message here...";
    [Tooltip("Toggle ON to send — auto-resets to false.")]
    public bool sendMessage = false;

    [Header("── UI References (Auto-Assigned by Generator) ──")]
    public Canvas canvas;
    public ScrollRect scrollRect;
    public RectTransform contentRect;

    [Header("── Sprites ──────────────────────────")]
    public Sprite checkUnreadSprite;
    public Sprite checkReadSprite;

    [Header("── Bubble Visual Settings ──────────────────")]
    public float msgFontSize = 18f;
    public float nameFontSize = 11f;
    public float timeFontSize = 10f;

    [Space]
    public float bubblePaddingH = 12f;
    public float bubblePaddingV = 8f;
    public float elementSpacing = 6f;
    public float footerHeight = 21f;
    public float minBubbleWidth = 60f;
    public float widthBuffer = 7f; // Добавочный зазор для ширины

    [Space]
    public float avatarSize = 40f;
    public float avatarToBubbleGap = 10f;
    public float messageRowSpacing = 12f;

    [Header("── File Bubble Settings ────────────────────")]
    public float fileBubbleWidth = 220f;
    public float fileBlockHeight = 46f;
    public float fileIconSize = 38f;
    public float fileIconGap = 10f;
    public float fileNameFontSize = 13f;
    public float fileSizeFontSize = 10f;
    public float fileIconArrowSize = 19f;

    [Header("── Send a File (Test) ──────────────────────")]
    public string testFileName = "daily_reward.zip";
    public string testFileSize = "1.4 MB";
    public string testButtonLabel = "DOWNLOAD";
    public bool sendFileMessage = false; // Галочка-триггер

    public struct CharacterProfile
    {
        public string displayName;
        public Color avatarColor;
        public Color nameColor;
    }

    private CharacterProfile[] _profiles;
    private bool wasOpened = false;

    private Coroutine _dialogueCoroutine;
    private DialogueData _currentDialogueData;
    private int _currentMessageIndex = 0;
    private bool _isDialogueActive = false;

    private void Awake()
    {
        _profiles = new CharacterProfile[]
        {
            new CharacterProfile { displayName = "Босс",    avatarColor = Theme.HexColor("E57373"), nameColor = Theme.HexColor("C0392B") },
            new CharacterProfile { displayName = "Химик", avatarColor = Theme.HexColor("66BB6A"), nameColor = Theme.HexColor("1E7E34") },
            new CharacterProfile { displayName = "Молчун",        avatarColor = Theme.HexColor("90A4AE"), nameColor = Theme.HexColor("546E7A") },
            new CharacterProfile { displayName = "Вы",        avatarColor = Theme.HexColor("4A9EF5"), nameColor = Color.clear },
        };

        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (sendMessage)
        {
            sendMessage = false;
            SendMessageNow(selectedSender, messageContent);
        }

        if (sendFileMessage)
        {
            sendFileMessage = false;
            if (Application.isPlaying)
                SpawnFileBubble(selectedSender, testFileName, testFileSize, () => Debug.Log("File Clicked!"));
        }
    }

    [ContextMenu("Send Message Now")]
    public void TriggerSendFromMenu()
    {
        if (Application.isPlaying && !string.IsNullOrWhiteSpace(messageContent))
            SendMessageNow(selectedSender, messageContent);
    }

    private void SendMessageNow(Sender sender, string text)
    {
        GameObject bubble = SpawnMessageBubble(sender, text);

        ScrollToBottom();

        if (sender == Sender.Player)
        {
            StartCoroutine(DelayedReadCheck(bubble));
        }
    }

    private IEnumerator DelayedReadCheck(GameObject bubble)
    {
        yield return new WaitForSeconds(0.5f);
        SetReadChecks(bubble, true);
    }

    private GameObject SpawnMessageBubble(Sender sender, string text)
    {
        if (contentRect == null) return null;

        bool isPlayer = (sender == Sender.Player);
        CharacterProfile profile = _profiles[(int)sender];

        // --- 1. Подготовка текста ---
        GameObject msgGO = new GameObject("MessageText");
        TextMeshProUGUI msgT = msgGO.AddComponent<TextMeshProUGUI>();
        msgT.fontSize = msgFontSize;
        msgT.text = text;

        Vector2 textSize = msgT.GetPreferredValues(text);
        float textW = textSize.x;
        float textH = textSize.y;

        float nameW = 0;
        float nameH = 0;
        if (!isPlayer)
        {
            Vector2 nSize = msgT.GetPreferredValues(profile.displayName);
            nameW = nSize.x;
            nameH = 16f; // Можно тоже вынести в поле, если нужно
        }

        // --- 2. Расчет размеров контента ---
        float finalContentWidth = Mathf.Max(textW, nameW, minBubbleWidth) + widthBuffer;

        // Итоговые размеры бабла
        float bubbleW = finalContentWidth + (bubblePaddingH * 2);
        float bubbleH = (isPlayer ? 0 : nameH + elementSpacing)
                      + textH
                      + footerHeight
                      + (bubblePaddingV * 2)
                      + elementSpacing;

        // --- 3. MsgRow (Контейнер строки) ---
        GameObject row = new GameObject("MsgRow");
        RectTransform rowRT = row.AddComponent<RectTransform>();
        row.transform.SetParent(contentRect, false);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = bubbleH + messageRowSpacing;

        // --- 4. Avatar ---
        float avOffset = 0;
        GameObject av = new GameObject("Avatar");
        av.transform.SetParent(row.transform, false);
        Image avImg = av.AddComponent<Image>();
        avImg.color = profile.avatarColor;
        avImg.sprite = Theme.GetRoundedBubbleSprite();
        avImg.type = Image.Type.Sliced;

        RectTransform avRT = av.GetComponent<RectTransform>();
        avRT.anchorMin = avRT.anchorMax = new Vector2(0, 1);
        avRT.pivot = new Vector2(0, 1);
        avRT.anchoredPosition = new Vector2(8, -4);
        avRT.sizeDelta = new Vector2(avatarSize, avatarSize);

        avOffset = avatarSize + avatarToBubbleGap;

        // --- 5. Bubble ---
        GameObject bubble = new GameObject("Bubble");
        bubble.transform.SetParent(row.transform, false);
        Image bubImg = bubble.AddComponent<Image>();
        bubImg.sprite = Theme.GetRoundedBubbleSprite();
        bubImg.type = Image.Type.Sliced;
        bubImg.color = isPlayer ? Theme.BG_PLY_BUBBLE : Theme.BG_BOT_BUBBLE;

        RectTransform bubRT = bubble.GetComponent<RectTransform>();
        bubRT.anchorMin = bubRT.anchorMax = new Vector2(0, 1);
        bubRT.pivot = new Vector2(0, 1);
        bubRT.anchoredPosition = new Vector2(8 + avOffset, -4);
        bubRT.sizeDelta = new Vector2(bubbleW, bubbleH);

        // --- 6. Внутренние элементы ---
        float currentY = -bubblePaddingV;

        // Имя
        if (!isPlayer)
        {
            GameObject nameGO = new GameObject("NameLabel");
            nameGO.transform.SetParent(bubble.transform, false);
            TextMeshProUGUI nT = nameGO.AddComponent<TextMeshProUGUI>();
            nT.text = profile.displayName;
            nT.fontSize = nameFontSize;
            nT.fontWeight = FontWeight.Bold;
            nT.color = profile.nameColor;

            RectTransform nRT = nT.rectTransform;
            nRT.anchorMin = nRT.anchorMax = new Vector2(0, 1);
            nRT.pivot = new Vector2(0, 1);
            nRT.anchoredPosition = new Vector2(bubblePaddingH, currentY);
            nRT.sizeDelta = new Vector2(finalContentWidth, nameH);
            currentY -= (nameH + elementSpacing);
        }

        // Текст сообщения
        msgGO.transform.SetParent(bubble.transform, false);
        msgT.color = Theme.TEXT_MAIN;
        msgT.alignment = TextAlignmentOptions.TopLeft;

        RectTransform mRT = msgT.rectTransform;
        mRT.anchorMin = mRT.anchorMax = new Vector2(0, 1);
        mRT.pivot = new Vector2(0, 1);
        mRT.anchoredPosition = new Vector2(bubblePaddingH, currentY);
        mRT.sizeDelta = new Vector2(finalContentWidth, textH);

        // Футер
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(bubble.transform, false);
        RectTransform fRT = footer.AddComponent<RectTransform>();
        fRT.anchorMin = fRT.anchorMax = new Vector2(1, 0);
        fRT.pivot = new Vector2(1, 0);
        fRT.anchoredPosition = new Vector2(-bubblePaddingH, bubblePaddingV);
        fRT.sizeDelta = new Vector2(finalContentWidth, footerHeight);

        // Время
        GameObject tsGO = new GameObject("Time");
        tsGO.transform.SetParent(footer.transform, false);
        TextMeshProUGUI tsT = tsGO.AddComponent<TextMeshProUGUI>();
        tsT.text = DateTime.Now.ToString("HH:mm");
        tsT.fontSize = timeFontSize;
        tsT.color = Theme.TEXT_MUTED;
        tsT.alignment = TextAlignmentOptions.BottomRight;

        RectTransform tsRT = tsGO.GetComponent<RectTransform>();
        UIUtils.FullStretch(tsRT);

        if (isPlayer)
        {
            tsRT.offsetMax = new Vector2(-34, 0); // Зазор под галочки

            GameObject checksGO = new GameObject("Checks");
            checksGO.transform.SetParent(footer.transform, false);
            Image chImg = checksGO.AddComponent<Image>();
            chImg.sprite = checkUnreadSprite;
            chImg.color = Theme.CHECK_UNREAD;
            chImg.preserveAspect = true;

            RectTransform chRT = checksGO.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(1, 0.5f);
            chRT.pivot = new Vector2(1, 0.5f);
            chRT.anchoredPosition = new Vector2(0, 6);
            chRT.sizeDelta = new Vector2(25, 25);
        }

        return bubble;
    }

    /// <summary>
    /// Telegram-style file bubble.
    /// Click target = the icon circle OR the filename (both wired to one Button).
    /// Layout:
    ///   [Avatar]  ┌──────────────────────────┐
    ///             │ The Don                   │  ← sender name (non-player)
    ///             │ [●↓]  filename.zip        │  ← [Button] icon + name + size
    ///             │       1.4 KB · ZIP        │
    ///             │                    12:34 ✓│  ← footer
    ///             └──────────────────────────┘
    /// </summary>
    public GameObject SpawnFileBubble(
        Sender sender,
        string fileName,
        string fileSize,
        Action onDownloadClick = null)
    {
        if (contentRect == null) return null;

        bool isPlayer = (sender == Sender.Player);
        CharacterProfile profile = _profiles[(int)sender];

        // ── Размеры ───────────────────────────────────────────────────────────────
        // nameH: фиксированная высота строки имени — как в SpawnMessageBubble
        const float nameH = 25f;

        float contentW = fileBubbleWidth - (bubblePaddingH * 2f);

        //  bubbleH: верхний пад + [имя + gap] + fileBlock + gap + footer + нижний пад
        float bubbleH = bubblePaddingV
                      + (isPlayer ? 0f : nameH + elementSpacing)
                      + fileBlockHeight
                      + elementSpacing
                      + footerHeight
                      + bubblePaddingV;

        // ── 1. Row ────────────────────────────────────────────────────────────────
        GameObject row = new GameObject("FileMsgRow");
        row.transform.SetParent(contentRect, false);
        row.AddComponent<RectTransform>();
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = bubbleH + messageRowSpacing;

        // ── 2. Avatar ─────────────────────────────────────────────────────────────
        GameObject av = new GameObject("Avatar");
        av.transform.SetParent(row.transform, false);
        Image avImg = av.AddComponent<Image>();
        avImg.sprite = Theme.GetRoundedBubbleSprite();
        avImg.type = Image.Type.Sliced;
        avImg.color = profile.avatarColor;

        RectTransform avRT = av.GetComponent<RectTransform>();
        avRT.anchorMin = avRT.anchorMax = new Vector2(0f, 1f);
        avRT.pivot = new Vector2(0f, 1f);
        avRT.anchoredPosition = new Vector2(8f, -4f);
        avRT.sizeDelta = new Vector2(avatarSize, avatarSize);

        float bubbleX = 8f + avatarSize + avatarToBubbleGap;

        // ── 3. Bubble ─────────────────────────────────────────────────────────────
        GameObject bubble = new GameObject("Bubble");
        bubble.transform.SetParent(row.transform, false);
        Image bubImg = bubble.AddComponent<Image>();
        bubImg.sprite = Theme.GetRoundedBubbleSprite();
        bubImg.type = Image.Type.Sliced;
        bubImg.color = isPlayer ? Theme.BG_PLY_BUBBLE : Theme.BG_BOT_BUBBLE;

        RectTransform bubRT = bubble.GetComponent<RectTransform>();
        bubRT.anchorMin = bubRT.anchorMax = new Vector2(0f, 1f);
        bubRT.pivot = new Vector2(0f, 1f);
        bubRT.anchoredPosition = new Vector2(bubbleX, -4f);
        bubRT.sizeDelta = new Vector2(fileBubbleWidth, bubbleH);

        // ── Курсор Y (от верха бабла вниз, как в SpawnMessageBubble) ─────────────
        float y = -bubblePaddingV;

        // ── 4. Имя отправителя ────────────────────────────────────────────────────

        GameObject nameGO = new GameObject("SenderName");
        nameGO.transform.SetParent(bubble.transform, false);
        TextMeshProUGUI nT = nameGO.AddComponent<TextMeshProUGUI>();
        nT.text = profile.displayName;
        nT.fontSize = nameFontSize;
        nT.fontWeight = FontWeight.Bold;
        nT.color = profile.nameColor;

        RectTransform nRT = nT.rectTransform;
        nRT.anchorMin = nRT.anchorMax = new Vector2(0f, 1f);
        nRT.pivot = new Vector2(0f, 1f);
        nRT.anchoredPosition = new Vector2(bubblePaddingH, y);
        nRT.sizeDelta = new Vector2(contentW, nameH);

        y -= nameH + elementSpacing;

        // ── 5. FileButton (прозрачная кнопка-контейнер) ───────────────────────────
        GameObject fileBtn = new GameObject("FileButton");
        fileBtn.transform.SetParent(bubble.transform, false);

        Image fileBtnImg = fileBtn.AddComponent<Image>();
        fileBtnImg.color = Color.clear;

        Button fileButton = fileBtn.AddComponent<Button>();
        ColorBlock cb = fileButton.colors;
        cb.normalColor = Color.clear;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        fileButton.colors = cb;
        fileButton.targetGraphic = fileBtnImg;
        fileButton.onClick.AddListener(() =>
        {
            onDownloadClick?.Invoke();
            fileButton.interactable = false;
        });

        RectTransform fbRT = fileBtn.GetComponent<RectTransform>();
        fbRT.anchorMin = fbRT.anchorMax = new Vector2(0f, 1f);
        fbRT.pivot = new Vector2(0f, 1f);
        fbRT.anchoredPosition = new Vector2(bubblePaddingH, y);
        fbRT.sizeDelta = new Vector2(contentW, fileBlockHeight);

        // ── 5a. Иконка (круг) — вертикально по центру fileBlock ──────────────────
        //  Внутри fileBtn якоря (0,1), поэтому центр по Y = -(fileBlockHeight / 2)
        float iconCenterY = -(fileBlockHeight / 2f);

        GameObject iconCircle = new GameObject("IconCircle");
        iconCircle.transform.SetParent(fileBtn.transform, false);
        Image icImg = iconCircle.AddComponent<Image>();
        icImg.sprite = Theme.GetRoundedBubbleSprite();
        icImg.type = Image.Type.Sliced;
        icImg.color = Theme.HexColor("4A9EF5");

        RectTransform icRT = iconCircle.GetComponent<RectTransform>();
        icRT.anchorMin = icRT.anchorMax = new Vector2(0f, 1f);
        icRT.pivot = new Vector2(0f, 0.5f);
        icRT.anchoredPosition = new Vector2(0f, iconCenterY);
        icRT.sizeDelta = new Vector2(fileIconSize, fileIconSize);

        // Стрелка внутри иконки
        GameObject arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(iconCircle.transform, false);
        TextMeshProUGUI arrowT = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowT.text = "↓";
        arrowT.fontSize = fileIconArrowSize;
        arrowT.color = Color.white;
        arrowT.alignment = TextAlignmentOptions.Center;
        UIUtils.FullStretch(arrowT.rectTransform);

        // ── 5b. Имя файла — верхняя половина текстового блока ────────────────────
        float textX = fileIconSize + fileIconGap;
        float textAreaW = contentW - textX;

        // Делим fileBlockHeight пополам: верх — имя, низ — размер
        float halfBlock = fileBlockHeight / 2f;
        float fnHeight = fileNameFontSize + 4f;
        float fsHeight = fileSizeFontSize + 4f;

        GameObject fileNameGO = new GameObject("FileName");
        fileNameGO.transform.SetParent(fileBtn.transform, false);
        TextMeshProUGUI fnT = fileNameGO.AddComponent<TextMeshProUGUI>();
        fnT.text = fileName;
        fnT.fontSize = fileNameFontSize;
        fnT.fontWeight = FontWeight.SemiBold;
        fnT.color = Theme.TEXT_MAIN;
        fnT.overflowMode = TextOverflowModes.Ellipsis;
        fnT.maxVisibleLines = 1;
        fnT.alignment = TextAlignmentOptions.BottomLeft;

        RectTransform fnRT = fnT.rectTransform;
        fnRT.anchorMin = fnRT.anchorMax = new Vector2(0f, 1f);
        fnRT.pivot = new Vector2(0f, 1f);
        // Верхняя половина блока, текст прижат ко дну своего прямоугольника
        fnRT.anchoredPosition = new Vector2(textX, 0f);
        fnRT.sizeDelta = new Vector2(textAreaW, halfBlock);

        // ── 5c. Размер файла — нижняя половина текстового блока ──────────────────
        string ext = System.IO.Path.GetExtension(fileName).TrimStart('.').ToUpper();
        string subLabel = string.IsNullOrEmpty(ext) ? fileSize : $"{fileSize} · {ext}";

        GameObject fileSizeGO = new GameObject("FileSize");
        fileSizeGO.transform.SetParent(fileBtn.transform, false);
        TextMeshProUGUI fsT = fileSizeGO.AddComponent<TextMeshProUGUI>();
        fsT.text = subLabel;
        fsT.fontSize = fileSizeFontSize;
        fsT.color = Theme.TEXT_MUTED;
        fsT.alignment = TextAlignmentOptions.TopLeft;

        RectTransform fsRT = fsT.rectTransform;
        fsRT.anchorMin = fsRT.anchorMax = new Vector2(0f, 0f);
        fsRT.pivot = new Vector2(0f, 0f);
        // Нижняя половина блока, текст прижат к верху своего прямоугольника
        fsRT.anchoredPosition = new Vector2(textX, 0f);
        fsRT.sizeDelta = new Vector2(textAreaW, halfBlock);

        y -= fileBlockHeight + elementSpacing;

        // ── 6. Footer ─────────────────────────────────────────────────────────────
        // Зеркало SpawnMessageBubble: anchor (1,0), pivot (1,0),
        // anchoredPosition.x = -bubblePaddingH (отступ от правого края бабла)
        // anchoredPosition.y =  bubblePaddingV  (отступ от нижнего края бабла)
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(bubble.transform, false);
        RectTransform fRT = footer.AddComponent<RectTransform>();
        fRT.anchorMin = fRT.anchorMax = new Vector2(1f, 0f);
        fRT.pivot = new Vector2(1f, 0f);
        fRT.anchoredPosition = new Vector2(-bubblePaddingH, bubblePaddingV);
        fRT.sizeDelta = new Vector2(contentW, footerHeight);

        // Время
        GameObject tsGO = new GameObject("Time");
        tsGO.transform.SetParent(footer.transform, false);
        TextMeshProUGUI tsT = tsGO.AddComponent<TextMeshProUGUI>();
        tsT.text = DateTime.Now.ToString("HH:mm");
        tsT.fontSize = timeFontSize;
        tsT.color = Theme.TEXT_MUTED;
        tsT.alignment = TextAlignmentOptions.BottomRight;

        RectTransform tsRT = tsGO.GetComponent<RectTransform>();
        UIUtils.FullStretch(tsRT);

        // Галочки для Player (зеркало SpawnMessageBubble)
        if (isPlayer)
        {
            tsRT.offsetMax = new Vector2(-34f, 0f);

            GameObject checksGO = new GameObject("Checks");
            checksGO.transform.SetParent(footer.transform, false);
            Image chImg = checksGO.AddComponent<Image>();
            chImg.sprite = checkUnreadSprite;
            chImg.color = Theme.CHECK_UNREAD;
            chImg.preserveAspect = true;

            RectTransform chRT = checksGO.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(1f, 0.5f);
            chRT.pivot = new Vector2(1f, 0.5f);
            chRT.anchoredPosition = new Vector2(0f, 6f);
            chRT.sizeDelta = new Vector2(25f, 25f);
        }

        ScrollToBottom();
        return bubble;
    }


    private void SetReadChecks(GameObject bubble, bool read)
    {
        Transform checks = bubble.transform.Find("Footer/Checks");
        if (checks == null) return;

        Image img = checks.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = read ? checkReadSprite : checkUnreadSprite;
            img.color = read ? Theme.CHECK_READ : Theme.CHECK_UNREAD;
        }
    }

    private Coroutine _scrollCoroutine;

    private void ScrollToBottom()
    {
        // Останавливаем предыдущую прокрутку, если она еще идет
        if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
        _scrollCoroutine = StartCoroutine(SmoothScrollToBottom(0.3f)); // 0.3 секунды на доезд
    }

    private IEnumerator SmoothScrollToBottom(float duration)
    {
        // Ждем конца кадра и еще один кадр, чтобы Unity успела пересчитать высоту Content
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        if (scrollRect == null) yield break;

        float elapsed = 0;
        float startPos = scrollRect.verticalNormalizedPosition;
        float targetPos = 0f; // 0 - это самый низ

        // Если мы уже почти внизу, не нужно долго крутить
        if (Mathf.Abs(startPos - targetPos) < 0.01f)
        {
            scrollRect.verticalNormalizedPosition = targetPos;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Используем SmoothStep для мягкого начала и конца движения
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPos;
        _scrollCoroutine = null;
    }

    public void PlayDialogue(DialogueData data)
    {
        if (data == null) return;

        // Если уже идет какой-то диалог, останавливаем его перед новым
        if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);

        _currentDialogueData = data;
        _currentMessageIndex = 0;
        _isDialogueActive = true;

        _dialogueCoroutine = StartCoroutine(ProcessDialogueQueue(data));
    }

    private IEnumerator ProcessDialogueQueue(DialogueData data)
    {
        for (_currentMessageIndex = 0; _currentMessageIndex < data.messages.Count; _currentMessageIndex++)
        {
            var entry = data.messages[_currentMessageIndex];
            SendMessageNow(entry.sender, entry.text);

            float waitTime = entry.delayAfter > 0 ? entry.delayAfter : 3.0f;
            yield return new WaitForSeconds(waitTime);
        }

        FinishDialogue();
    }

    // Вынесли завершение в отдельный метод, чтобы вызывать его и при скипе
    private void FinishDialogue()
    {
        _isDialogueActive = false;
        _dialogueCoroutine = null;

        SpawnFileBubble(Sender.Boss, "daily_reward.zip", "1.4kb", RoundManager.Instance.ClaimBonusDraft);
    }

    public void SkipDialogue()
    {
        // Если диалог не идет, ничего не делаем
        if (!_isDialogueActive || _currentDialogueData == null) return;

        // 1. Останавливаем корутину, чтобы она не спавнила сообщения по таймеру
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        // 2. Доспавниваем все сообщения, которые игрок еще не увидел
        for (int i = _currentMessageIndex + 1; i < _currentDialogueData.messages.Count; i++)
        {
            var entry = _currentDialogueData.messages[i];
            SendMessageNow(entry.sender, entry.text);
        }

        // 3. Вызываем финал (файл награды)
        FinishDialogue();

        Debug.Log("Диалог пропущен");
    }

    public void CloseMessenger()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    public void OpenMessenger()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);

        if (!wasOpened)
        {
            int result = ProfileManager.Instance.profile.result;
            DialogueManager.Instance.PlayNext(result);
            wasOpened = true;
        }
    }
}
