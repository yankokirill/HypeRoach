using Game.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Base
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Views")]
        [SerializeField] private StatsPanelView statsPanelView;
        [SerializeField] private InfoPanelView infoPanelView;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI resourcesText;
        [SerializeField] private TextMeshProUGUI dayText;

        [Header("Animation Settings")]
        [SerializeField] private float animationTickDuration = 1.0f;
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float panelPulseScale = 1.1f;
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private Color defaultColor = Color.white;

        private GridSlot _selectedSlot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (dayText != null && ProfileManager.Instance != null)
            {
                int currentDay = ProfileManager.Instance.profile.dayCount;
                dayText.text = $"День: {currentDay}";
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged += UpdateResourcesText;
                GameManager.Instance.OnSlotSelected += OnSlotSelected;
                GameManager.Instance.OnStatsChanged += RefreshStats;
                UpdateResourcesText(GameManager.Instance.GetCurrentResources());
            }

            infoPanelView?.SetDefault();
            RefreshStats();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged -= UpdateResourcesText;
                GameManager.Instance.OnSlotSelected -= OnSlotSelected;
                GameManager.Instance.OnStatsChanged -= RefreshStats;
            }
        }

        // ─── Ресурсы ──────────────────────────────────────────────────────────

        private void UpdateResourcesText(int amount)
        {
            if (resourcesText != null)
                resourcesText.text = $"Рубли: {amount}";
        }

        // ─── Слот / карта ──────────────────────────────────────────────────────

        private void OnSlotSelected(GridSlot slot)
        {
            _selectedSlot = slot;
            RefreshSlotInfo(slot);
        }

        private void RefreshSlotInfo(GridSlot slot)
        {
            if (infoPanelView == null) return;

            if (slot == null || slot.IsEmpty || slot.GetCard() == null)
            {
                ShowDefaultHint();
                return;
            }

            CardView card = slot.GetCard();
            FieldCard fc = card.fieldData;
            HandCardData src = fc.Source;

            int lives = fc.Level >= 4 ? 2 : 1;
            string body = $"Уровень: {fc.Level}\nЖизни: {lives}";
            body += $"\nХайп: +{fc.TotalHype}% (+{ProfileManager.Instance.profile.dedInsideHypeBonus})\nУклонение: +{fc.TotalEvasion}%";

            if (fc.Upgrades.Count > 0)
            {
                var grouped = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var u in fc.Upgrades)
                {
                    string name = UpgradeDisplayName(u);
                    grouped.TryGetValue(name, out int count);
                    grouped[name] = count + 1;
                }
                var parts = new System.Collections.Generic.List<string>();
                foreach (var kv in grouped)
                    parts.Add(kv.Value > 1 ? $"{kv.Key} x{kv.Value}" : kv.Key);
                body += $"\nУлучшения: {string.Join(", ", parts)}";
            }

            ShowHint(src.cardName, body);
        }

        // ─── Хинты ────────────────────────────────────────────────────────────

        public void ShowDefaultHint() => infoPanelView?.SetDefault();

        public void ShowHint(string title, string description)
        {
            infoPanelView?.SetTitle(title);
            infoPanelView?.SetDescription(description);
        }

        // ─── Статы ────────────────────────────────────────────────────────────

        public void RefreshStats()
        {
            if (statsPanelView == null || ProfileManager.Instance == null) return;
            statsPanelView.SetStats(ProfileManager.Instance.profile.totalPopulation);

            // Обновляем панель информации если слот выбран
            if (_selectedSlot != null)
                RefreshSlotInfo(_selectedSlot);
        }

        // ─── Анимация конца раунда ────────────────────────────────────────────

        public IEnumerator AnimateStatsRefresh(int newPopulation)
        {
            if (statsPanelView == null) yield break;

            int.TryParse(
                statsPanelView.populationText.text.Replace(",", "").Split('/')[0].Trim(),
                out int oldPopulation);

            yield return StartCoroutine(AnimateSingleStat(
                statsPanelView.populationText, oldPopulation, newPopulation, isPopulation: true));
        }

        private IEnumerator AnimateSingleStat(TextMeshProUGUI textElement, int startValue, int endValue,
                                               bool isPopulation = false)
        {
            bool hasChanged = startValue != endValue;
            Color targetColor = hasChanged
                ? (endValue > startValue ? increaseColor : decreaseColor)
                : defaultColor;

            Vector3 originalScale = Vector3.one;
            Vector3 pulseTargetScale = originalScale * pulseScale;
            float elapsed = 0f;

            while (elapsed < animationTickDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationTickDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                float pulse = 1 - Mathf.Abs(0.5f - t) * 2;

                textElement.transform.localScale =
                    Vector3.Lerp(originalScale, pulseTargetScale, pulse);

                if (hasChanged)
                {
                    int current = (int)Mathf.Lerp(startValue, endValue, smoothT);
                    textElement.color = Color.Lerp(defaultColor, targetColor, pulse);
                    textElement.text = (isPopulation ? $"{current:N0}" : current.ToString()) + " / 1054";
                }
                else
                {
                    textElement.color = defaultColor;
                }

                yield return null;
            }

            textElement.text = (isPopulation ? $"{endValue:N0}" : endValue.ToString()) + " / 1054";
            textElement.transform.localScale = originalScale;
            textElement.color = defaultColor;
        }
        private static string UpgradeDisplayName(UpgradeType u) => u switch
        {
            UpgradeType.DedInside => "Дед Инсайд",
            UpgradeType.Baryga => "Барыга",
            UpgradeType.Berserk => "Берсерк",
            UpgradeType.Tusovshchik => "Тусовщик",
            _ => u.ToString(),
        };
    }
}
