using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game.EndlessRunner
{
    public class RunnerUI : MonoBehaviour
    {
        public static RunnerUI Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI hypeText;
        [SerializeField] private TextMeshProUGUI hypeGoalText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Transform livesContainer;
        [SerializeField] private Sprite heartSprite;

        [Header("Result Screen (победа и поражение)")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultHypeText;
        [SerializeField] private TextMeshProUGUI resultMoneyText;
        [SerializeField] private Button resultReturnButton;

        [Header("Result Screen — цвета заголовка")]
        [SerializeField] private Color victoryTitleColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color defeatTitleColor = new Color(1f, 0.25f, 0.25f);

        [Header("Floating Text")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Canvas worldCanvas;

        [Header("Controls Hint")]
        [SerializeField] private GameObject controlsHint;

        private List<GameObject> _lifeIcons = new List<GameObject>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            resultPanel?.SetActive(false);

            resultReturnButton?.onClick.AddListener(() => RunnerManager.Instance?.ReturnToBase());

            if (controlsHint != null)
                StartCoroutine(HideHintAfterDelay(3f));
        }

        // ── публичный API ─────────────────────────────────────────────────────

        public void ShowHUD(bool visible)
        {
            if (hypeText) hypeText.gameObject.SetActive(visible);
            if (hypeGoalText) hypeGoalText.gameObject.SetActive(visible);
            if (speedText) speedText.gameObject.SetActive(visible);
            if (moneyText) moneyText.gameObject.SetActive(visible);
            if (livesContainer) livesContainer.gameObject.SetActive(visible);
        }

        public void UpdateHype(int hype)
        {
            if (hypeText) hypeText.text = $"HYPE: {hype}";
        }

        public void UpdateHypeGoal(int goal)
        {
            if (hypeGoalText) hypeGoalText.text = $"GOAL: {goal}";
        }

        public void UpdateSpeed(float speed)
        {
            if (speedText) speedText.text = $"SPEED: {speed:F1}";
        }

        public void UpdateMoney(int money)
        {
            if (moneyText) moneyText.text = $"RUB: {money}";
        }

        public void UpdateLives(int lives)
        {
            foreach (var icon in _lifeIcons)
                if (icon) Destroy(icon);
            _lifeIcons.Clear();

            if (heartSprite == null || livesContainer == null) return;

            for (int i = 0; i < lives; i++)
            {
                var go = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(livesContainer, false);

                var img = go.GetComponent<Image>();
                img.sprite = heartSprite;
                img.preserveAspect = true;

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(50f, 50f);

                _lifeIcons.Add(go);
            }
        }

        public void ShowGameOver(int finalHype) => ShowResult(finalHype, isVictory: false);

        public void ShowVictory(int finalHype) => ShowResult(finalHype, isVictory: true);

        private void ShowResult(int finalHype, bool isVictory)
        {
            if (resultPanel) resultPanel.SetActive(true);

            if (resultTitleText)
            {
                resultTitleText.text = isVictory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
                resultTitleText.color = isVictory ? victoryTitleColor : defeatTitleColor;
            }

            if (resultHypeText)
                resultHypeText.text = $"Хайп: {finalHype}";

            if (resultMoneyText)
                resultMoneyText.text = $"Заработано: {RunnerManager.Instance?.Money ?? 0} руб";
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color)
        {
            if (floatingTextPrefab == null) return;

            var parent = worldCanvas != null ? worldCanvas.transform : transform;
            var go = Instantiate(floatingTextPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity, parent);

            if (go.TryGetComponent<TextMeshPro>(out var tmp))
            {
                tmp.text = text;
                tmp.color = color;
            }

            StartCoroutine(AnimateFloatingText(go));
        }

        // ── приватные ─────────────────────────────────────────────────────────

        private IEnumerator AnimateFloatingText(GameObject go)
        {
            float duration = 1.2f;
            float elapsed = 0f;
            Vector3 startPos = go.transform.position;
            var tmp = go.GetComponent<TextMeshPro>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                go.transform.position = startPos + Vector3.up * (t * 1.5f);
                if (tmp) tmp.alpha = 1f - t;
                yield return null;
            }

            Destroy(go);
        }

        private IEnumerator HideHintAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (controlsHint) controlsHint.SetActive(false);
        }
    }
}
