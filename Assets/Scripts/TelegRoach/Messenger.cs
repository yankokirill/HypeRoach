using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Messenger : MonoBehaviour
{
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

    public struct CharacterProfile
    {
        public string displayName;
        public Color avatarColor;
        public Color nameColor;
    }

    private CharacterProfile[] _profiles;

    private void Awake()
    {
        _profiles = new CharacterProfile[]
        {
            new CharacterProfile { displayName = "The Don",    avatarColor = Theme.HexColor("E57373"), nameColor = Theme.HexColor("C0392B") },
            new CharacterProfile { displayName = "The Dealer", avatarColor = Theme.HexColor("66BB6A"), nameColor = Theme.HexColor("1E7E34") },
            new CharacterProfile { displayName = "...",        avatarColor = Theme.HexColor("90A4AE"), nameColor = Theme.HexColor("546E7A") },
            new CharacterProfile { displayName = "You",        avatarColor = Theme.HexColor("4A9EF5"), nameColor = Color.clear },
        };
    }

    private void Update()
    {
        if (sendMessage)
        {
            sendMessage = false;
            SendMessageNow(selectedSender, messageContent);
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
        msgT.fontSize = 14;
        msgT.text = text;

        Vector2 textSize = msgT.GetPreferredValues(text);
        float textW = textSize.x;
        float textH = textSize.y;

        float nameW = 0; float nameH = 0;
        if (!isPlayer)
        {
            Vector2 nSize = msgT.GetPreferredValues(profile.displayName);
            nameW = nSize.x;
            nameH = 16f;
        }

        // --- 2. Расчет чистых размеров контента ---
        float minContentWidth = 50f;
        float finalContentWidth = Mathf.Max(textW, nameW, minContentWidth) + 7f;

        // Константы отступов
        float padH = Theme.PAD_H;
        float padV = Theme.PAD_V;
        float footerH = 14f;
        float spacing = 4f;

        // Итоговые размеры бабла
        float bubbleW = finalContentWidth + (padH * 2);
        float bubbleH = nameH + textH + footerH + (padV * 2) + (spacing * (isPlayer ? 1 : 2));

        // --- 3. MsgRow (Контейнер строки для ScrollView) ---
        GameObject row = new GameObject("MsgRow");
        RectTransform rowRT = row.AddComponent<RectTransform>();
        row.transform.SetParent(contentRect, false);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = bubbleH + 6f; // Зазор между сообщениями

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
        avRT.sizeDelta = new Vector2(Theme.AVATAR_SIZE, Theme.AVATAR_SIZE);
        avOffset = Theme.AVATAR_SIZE + 10; // Сдвиг бабла относительно аватара

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

        // --- 6. Внутренние элементы (Имя -> Текст -> Футер) ---
        float currentY = -padV;

        // Имя
        if (!isPlayer)
        {
            GameObject nameGO = new GameObject("NameLabel");
            nameGO.transform.SetParent(bubble.transform, false);
            TextMeshProUGUI nT = nameGO.AddComponent<TextMeshProUGUI>();
            nT.text = profile.displayName;
            nT.fontSize = 11;
            nT.fontWeight = FontWeight.Bold;
            nT.color = profile.nameColor;

            RectTransform nRT = nT.rectTransform;
            nRT.anchorMin = nRT.anchorMax = new Vector2(0, 1);
            nRT.pivot = new Vector2(0, 1);
            nRT.anchoredPosition = new Vector2(padH, currentY);
            nRT.sizeDelta = new Vector2(finalContentWidth, nameH);
            currentY -= (nameH + spacing);
        }

        // Текст сообщения
        msgGO.transform.SetParent(bubble.transform, false);
        msgT.color = Theme.TEXT_MAIN;
        msgT.alignment = TextAlignmentOptions.TopLeft;

        RectTransform mRT = msgT.rectTransform;
        mRT.anchorMin = mRT.anchorMax = new Vector2(0, 1);
        mRT.pivot = new Vector2(0, 1);
        mRT.anchoredPosition = new Vector2(padH, currentY);
        mRT.sizeDelta = new Vector2(finalContentWidth, textH);

        // Футер (Время + Галочки)
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(bubble.transform, false);
        RectTransform fRT = footer.AddComponent<RectTransform>();
        fRT.anchorMin = fRT.anchorMax = new Vector2(1, 0); // Прижат к правому нижнему углу бабла
        fRT.pivot = new Vector2(1, 0);
        fRT.anchoredPosition = new Vector2(-padH, padV);
        fRT.sizeDelta = new Vector2(finalContentWidth, footerH);

        // Время
        GameObject tsGO = new GameObject("Time");
        tsGO.transform.SetParent(footer.transform, false);
        TextMeshProUGUI tsT = tsGO.AddComponent<TextMeshProUGUI>();
        tsT.text = DateTime.Now.ToString("HH:mm");
        tsT.fontSize = 10;
        tsT.color = Theme.TEXT_MUTED;
        tsT.alignment = TextAlignmentOptions.BottomRight;

        RectTransform tsRT = tsGO.GetComponent<RectTransform>();
        UIUtils.FullStretch(tsRT);
        if (isPlayer) tsRT.offsetMax = new Vector2(-18, 0); // Сдвиг для галочек

        if (isPlayer)
        {
            GameObject checksGO = new GameObject("Checks");
            checksGO.transform.SetParent(footer.transform, false);
            Image chImg = checksGO.AddComponent<Image>();
            chImg.sprite = checkUnreadSprite;
            chImg.color = Theme.CHECK_UNREAD;
            chImg.preserveAspect = true;

            RectTransform chRT = checksGO.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(1, 0.5f);
            chRT.pivot = new Vector2(1, 0.5f);
            chRT.anchoredPosition = new Vector2(0, 0);
            chRT.sizeDelta = new Vector2(14, 14);
        }

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
        StartCoroutine(ProcessDialogueQueue(data));
    }

    private IEnumerator ProcessDialogueQueue(DialogueData data)
    {
        foreach (var entry in data.messages)
        {
            // Отправляем сообщение мгновенно (наш старый метод)
            SendMessageNow(entry.sender, entry.text);

            // Ждем указанное в файле время перед следующим сообщением
            float waitTime = entry.delayAfter > 0 ? entry.delayAfter : 3.0f;
            yield return new WaitForSeconds(waitTime);
        }
    }
}
