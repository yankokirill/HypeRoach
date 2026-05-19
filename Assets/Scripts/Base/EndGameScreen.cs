using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Core; // Обязательно для доступа к ProfileManager и SceneTransitionManager

namespace Game.Base
{
    public class EndGameScreen : MonoBehaviour
    {
        public static EndGameScreen Instance { get; private set; }

        [Header("Настройки сцены")]
        [Tooltip("Название сцены, которая загрузится при нажатии 'Начать заново'")]
        [SerializeField] private string startSceneName = "Base";

        [Header("Настройки печати")]
        [SerializeField] private float charDelay = 0.03f;
        [SerializeField] private float paragraphDelay = 0.9f;

        // ── Тексты ──────────────────────────────────────────────────────────
        private readonly string[] VictoryStory = {
            "17 проклятых квадратных метров. Двухместная комната. По нормам общежития: 31 таракан на человека на квадратный метр — и тогда помещение признают непригодным для проживания.",
            "Цель достигнута! Я предъявил комендатше усатый легион. Ей было нечего возразить — здесь жить просто не-воз-мож-но!",
            "Я пообщался с ней, она сказала, что решит этот вопрос, а теперь можно лечь спать спокойно...",
            "На утро появился щуплый парниша с чемоданом. Выяснилось, что это мой второй сосед!",
            "31 таракан на квадратный метр на человека, но людей теперь трое! Никуда меня эта стерва не выселит..."
        };

        private readonly string[] DefeatStory = {
            "Тараканов не осталось. Совсем.",
            "Комендантша зашла в комнату и долго молчала.",
            "«Ну вот», — сказала она наконец. — «Порядок».",
            "Это было самое страшное слово, которое я когда-либо слышал."
        };

        // ── Цвета ───────────────────────────────────────────────────────────
        private readonly Color BgColor = new Color(0.05f, 0.05f, 0.05f, 0.98f);
        private readonly Color PanelColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private readonly Color TextPrimary = new Color(0.95f, 0.93f, 0.88f, 1f);
        private readonly Color TextMuted = new Color(0.55f, 0.53f, 0.50f, 1f);
        private readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.2f, 1f);
        private readonly Color ButtonHover = new Color(0.3f, 0.3f, 0.3f, 1f);

        // ── Внутренние ссылки ───────────────────────────────────────────────
        private GameObject _uiRoot;
        private GameObject _phase1;
        private GameObject _phase2;
        private Text[] _storyTexts;
        private Text _endNoteText;
        private CanvasGroup _restartBtnGroup;
        private Coroutine _activeRoutine;
        private bool _isVictory;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ═══════════════════════════════════════════════════════════════════
        //   ПУБЛИЧНЫЙ API
        // ═══════════════════════════════════════════════════════════════════

        public void ShowVictory() => OpenScreen(true);
        public void ShowDefeat() => OpenScreen(false);

        private void OpenScreen(bool isVictory)
        {
            _isVictory = isVictory;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (_uiRoot == null) BuildUI();

            _phase1.transform.Find("Title").GetComponent<Text>().text = _isVictory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            _phase1.transform.Find("Title").GetComponent<Text>().color = _isVictory ? new Color(0.85f, 0.75f, 0.35f) : new Color(0.75f, 0.30f, 0.28f);
            _phase1.transform.Find("Subtitle").GetComponent<Text>().text = _isVictory ? "Неужели наступит долгожданное выселение?.." : "Последний таракан исчез.";

            _uiRoot.SetActive(true);
            SwitchToPhase1();
        }

        // ═══════════════════════════════════════════════════════════════════
        //   ЛОГИКА КНОПКИ РЕСТАРТА
        // ═══════════════════════════════════════════════════════════════════

        private void RestartGame()
        {
            // На всякий случай снимаем игру с паузы, если она была
            Time.timeScale = 1f;

            // 1. Обнуляем профиль
            if (ProfileManager.Instance != null)
            {
                ProfileManager.Instance.ResetProfile();
            }

            // 2. Скрываем экран конца игры
            _uiRoot.SetActive(false);

            // 3. Загружаем сцену
               UnityEngine.SceneManagement.SceneManager.LoadScene(startSceneName);
        }

        // ═══════════════════════════════════════════════════════════════════
        //   ПОСТРОЕНИЕ ИНТЕРФЕЙСА
        // ═══════════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            _uiRoot = new GameObject("EndGame_Canvas");

            var canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _uiRoot.AddComponent<GraphicRaycaster>();

            var bg = CreateUIObject("Background", _uiRoot.transform);
            StretchRect(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = BgColor;
            bgImg.raycastTarget = true;

            // --- Фаза 1 (Окно с кнопкой) ---
            _phase1 = CreatePanel("Phase1_Panel", bg.transform, 600, 350);
            CreateText("Emoji", _phase1.transform, "🪲", 60, TextAnchor.MiddleCenter, TextPrimary);
            CreateText("Title", _phase1.transform, "TITLE", 45, TextAnchor.MiddleCenter, Color.white);
            CreateText("Subtitle", _phase1.transform, "Subtitle", 22, TextAnchor.MiddleCenter, TextMuted);

            var btnGo = CreateButton("NextButton", _phase1.transform, "Далее →", SwitchToPhase2);
            var btnLayout = btnGo.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 60;

            // --- Фаза 2 (Хроника) ---
            _phase2 = CreatePanel("Phase2_Panel", bg.transform, 800, 650);
            CreateText("Header", _phase2.transform, "ХРОНИКА ВЫСЕЛЕНИЯ", 14, TextAnchor.MiddleLeft, TextMuted);

            var divider = CreateUIObject("Divider", _phase2.transform);
            divider.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            divider.AddComponent<LayoutElement>().preferredHeight = 2;

            int maxParagraphs = Mathf.Max(VictoryStory.Length, DefeatStory.Length);
            _storyTexts = new Text[maxParagraphs];
            for (int i = 0; i < maxParagraphs; i++)
            {
                var pText = CreateText($"Para_{i}", _phase2.transform, "", 22, TextAnchor.UpperLeft, TextPrimary);
                pText.GetComponent<LayoutElement>().flexibleHeight = 1;
                _storyTexts[i] = pText;
            }

            _endNoteText = CreateText("EndNote", _phase2.transform, "— Конец истории —", 16, TextAnchor.MiddleCenter, new Color(TextMuted.r, TextMuted.g, TextMuted.b, 0f));
            _endNoteText.GetComponent<LayoutElement>().preferredHeight = 40;

            // КНОПКА РЕСТАРТА (Скрыта изначально с помощью CanvasGroup)
            var restartBtnGo = CreateButton("RestartButton", _phase2.transform, "Начать заново", RestartGame);
            var restartLayout = restartBtnGo.AddComponent<LayoutElement>();
            restartLayout.preferredHeight = 60;

            _restartBtnGroup = restartBtnGo.AddComponent<CanvasGroup>();
            _restartBtnGroup.alpha = 0f;
            _restartBtnGroup.interactable = false;

            _uiRoot.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        //   АНИМАЦИИ
        // ═══════════════════════════════════════════════════════════════════

        private void SwitchToPhase1()
        {
            _phase1.SetActive(true);
            _phase2.SetActive(false);
        }

        private void SwitchToPhase2()
        {
            _phase1.SetActive(false);
            _phase2.SetActive(true);

            foreach (var t in _storyTexts) t.text = "";
            _endNoteText.color = new Color(TextMuted.r, TextMuted.g, TextMuted.b, 0f);

            if (_restartBtnGroup != null)
            {
                _restartBtnGroup.alpha = 0f;
                _restartBtnGroup.interactable = false;
            }

            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(TypewriterRoutine());
        }

        private IEnumerator TypewriterRoutine()
        {
            string[] currentStory = _isVictory ? VictoryStory : DefeatStory;

            for (int i = 0; i < currentStory.Length; i++)
            {
                _storyTexts[i].text = "";
                foreach (char c in currentStory[i])
                {
                    _storyTexts[i].text += c;
                    yield return new WaitForSecondsRealtime(charDelay);
                }
                yield return new WaitForSecondsRealtime(paragraphDelay);
            }

            // Плавное появление надписи "Конец" и Кнопки Рестарта
            float elapsed = 0f;
            float duration = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);

                _endNoteText.color = new Color(TextMuted.r, TextMuted.g, TextMuted.b, alpha);
                if (_restartBtnGroup != null) _restartBtnGroup.alpha = alpha;

                yield return null;
            }

            // Включаем кликабельность кнопки
            if (_restartBtnGroup != null) _restartBtnGroup.interactable = true;
        }

        // ═══════════════════════════════════════════════════════════════════
        //   ХЕЛПЕРЫ ДЛЯ ГЕНЕРАЦИИ UI
        // ═══════════════════════════════════════════════════════════════════

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void StretchRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        private GameObject CreatePanel(string name, Transform parent, float width, float height)
        {
            var panel = CreateUIObject(name, parent);
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = panel.AddComponent<Image>();
            img.color = PanelColor;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 15;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private Text CreateText(string name, Transform parent, string content, int size, TextAnchor align, Color color)
        {
            var go = CreateUIObject(name, parent);
            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.alignment = align;
            txt.color = color;
            txt.raycastTarget = false;
            go.AddComponent<LayoutElement>();
            return txt;
        }

        private GameObject CreateButton(string name, Transform parent, string labelText, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = CreateUIObject(name, parent);

            var img = btnGo.AddComponent<Image>();
            img.color = ButtonNormal;
            img.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            var txtGo = CreateUIObject("Text", btnGo.transform);
            StretchRect(txtGo);
            var txt = txtGo.AddComponent<Text>();
            txt.text = labelText;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = TextPrimary;
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;

            return btnGo;
        }
    }
}
