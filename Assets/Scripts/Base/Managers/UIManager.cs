using Game.Core;
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

        private void Awake()
        {
            Instance = this;
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

                ShowHint($"{data.cardName} [Ур. {data.level}]", fullDescription);
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

        public void RefreshStats()
        {
            if (statsPanelView == null || ProfileManager.Instance == null) return;

            PlayerProfile profile = ProfileManager.Instance.profile;

            // Передаем только нужные новые статы
            statsPanelView.SetStats(profile.totalPopulation, profile.baseIQ, profile.baseCharisma);
        }
    }
}
