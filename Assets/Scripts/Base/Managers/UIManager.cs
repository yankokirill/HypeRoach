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

        [Header("Resources")]
        [SerializeField] private Text resourcesText;

        [Header("Animation Settings")]
        [SerializeField] private float animationTickDuration = 1.0f; // Общее время на "тиканье" одного стата
        [SerializeField] private float pulseScale = 1.2f; // Насколько увеличится число
        [SerializeField] private float panelPulseScale = 1.1f; // На сколько увеличится statsPanel
        [SerializeField] private float delayBetweenStats = 0.3f; // Пауза между анимациями
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private Color defaultColor = Color.white;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged += UpdateResourcesText;
                GameManager.Instance.OnSlotSelected += OnSlotSelected;
                GameManager.Instance.OnStatsChanged += RefreshStats;

                UpdateResourcesText(GameManager.Instance.GetCurrentResources());
            }

            if (infoPanelView != null)
                infoPanelView.SetDefault();

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

        private void UpdateResourcesText(int amount)
        {
            if (resourcesText != null)
                resourcesText.text = $"Ресурсы: {amount}";
        }

        private void OnSlotSelected(GridSlot slot)
        {
            if (infoPanelView == null) return;

            if (slot == null || slot.IsEmpty || slot.GetCard() == null)
            {
                ShowDefaultHint();
            }
            else
            {
                CardView card = slot.GetCard();
                CardData data = card.cardData;

                // Формируем красивый текст: Лор (курсивом) + пустая строка + Эффект
                string fullDescription = $"<i>{data.CurrentDescription}</i>\n\n{data.CurrentEffectText}";

                ShowHint(data.cardName, fullDescription);
            }
        }

        public void ShowDefaultHint()
        {
            infoPanelView.SetDefault();
        }

        public void ShowHint(string title, string description)
        {
            infoPanelView.SetTitle(title);
            infoPanelView.SetDescription(description);
        }

        public IEnumerator AnimateStatsRefresh(int newPopulation, int newIQ, int newCharisma)
        {
            if (statsPanelView == null || ProfileManager.Instance == null) yield break;

            // 1. Увеличиваем всю панель статов
            yield return StartCoroutine(AnimatePanelScale(statsPanelView.transform, panelPulseScale, 0.3f));

            // 2. Получаем старые значения
            int.TryParse(statsPanelView.populationText.text.Split('/')[0].Trim().Replace(",", ""), out int oldPopulation);
            int.TryParse(statsPanelView.championIQText.text, out int oldIQ);
            int.TryParse(statsPanelView.championCharismaText.text, out int oldCharisma);

            // 3. Запускаем анимации характеристик последовательно
            yield return StartCoroutine(AnimateSingleStat(statsPanelView.populationText, oldPopulation, newPopulation, true));
            yield return new WaitForSeconds(delayBetweenStats);

            yield return StartCoroutine(AnimateSingleStat(statsPanelView.championCharismaText, oldCharisma, newCharisma));
            yield return new WaitForSeconds(delayBetweenStats);

            yield return StartCoroutine(AnimateSingleStat(statsPanelView.championIQText, oldIQ, newIQ));

            // 4. Возвращаем размер панели в норму
            yield return StartCoroutine(AnimatePanelScale(statsPanelView.transform, 1.0f, 0.3f));
        }

        // Анимация масштаба всей панели
        private IEnumerator AnimatePanelScale(Transform target, float targetScale, float duration)
        {
            Vector3 startScale = target.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }
            target.localScale = endScale;
        }

        private IEnumerator AnimateSingleStat(Text textElement, int startValue, int endValue, bool isPopulation = false)
        {
            // Определяем, изменилось ли значение
            bool hasChanged = startValue != endValue;

            // Если изменилось - выбираем цвет, если нет - оставляем белый
            Color targetColor = hasChanged ? (endValue > startValue ? increaseColor : decreaseColor) : defaultColor;

            Vector3 originalScale = Vector3.one; // Базовый размер
            Vector3 pulseTargetScale = originalScale * pulseScale;

            float elapsed = 0f;

            while (elapsed < animationTickDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationTickDuration;
                float smoothedT = Mathf.SmoothStep(0, 1, t);

                // 1. Анимация пульсации (Scale) - Работает ВСЕГДА
                float pulseCurve = 1 - Mathf.Abs(0.5f - t) * 2;
                textElement.transform.localScale = Vector3.Lerp(originalScale, pulseTargetScale, pulseCurve);

                // 2. Анимация числа и цвета - только если ЗНАЧЕНИЕ ИЗМЕНИЛОСЬ
                if (hasChanged)
                {
                    int currentValue = (int)Mathf.Lerp(startValue, endValue, smoothedT);
                    textElement.color = Color.Lerp(defaultColor, targetColor, pulseCurve);

                    if (isPopulation)
                        textElement.text = $"{currentValue:N0} / 1000";
                    else
                        textElement.text = currentValue.ToString();
                }
                else
                {
                    // Если значение не менялось, просто держим белый цвет
                    textElement.color = defaultColor;
                }

                yield return null;
            }

            // Финальная установка значений
            if (isPopulation)
                textElement.text = $"{endValue:N0} / 1000";
            else
                textElement.text = endValue.ToString();

            textElement.transform.localScale = originalScale;
            textElement.color = defaultColor;
        }

        public void RefreshStats()
        {
            if (statsPanelView == null || ProfileManager.Instance == null) return;
            PlayerProfile profile = ProfileManager.Instance.profile;
            statsPanelView.SetStats(profile.totalPopulation, profile.baseIQ, profile.baseCharisma);
        }
    }
}
