using Game.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Base
{
    public class RoundManager : MonoBehaviour
    {
        public static RoundManager Instance;

        [Header("References")]
        public GameManager gameManager;
        public UIManager uiManager;
        public DraftManager draftManager;

        [Header("One-time Bonus Button")]
        [SerializeField] private Button getBonusButton;

        // --- ЛОГИКА БОНУСНОГО ДРАФТА ---

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ClaimBonusDraft()
        {
            if (ProfileManager.Instance == null || draftManager == null) return;

            // 1. Делаем кнопку неактивной (вместо удаления)
            DisableBonusButton();

            // 2. Запускаем выбор карт
            Debug.Log("Запуск бонусного выбора карт!");
            draftManager.StartDraft();
        }

        private void DisableBonusButton()
        {
            if (getBonusButton != null)
            {
                getBonusButton.interactable = false; // Кнопка станет серой и некликтабельной

                // Опционально: можно поменять текст на кнопке, если он есть
                Text btnText = getBonusButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "ПОЛУЧЕНО";
            }
        }

        // --- ЛОГИКА ЗАВЕРШЕНИЯ РАУНДА ---

        public void ProcessEndOfRound()
        {
            if (ProfileManager.Instance == null) return;
            PlayerProfile profile = ProfileManager.Instance.profile;

            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);

            int roundResources = 0;
            int roundPopulation = 0;

            // Пересчет характеристик Чемпиона на основе текущих построек
            profile.baseIQ = 0;
            profile.baseCharisma = 0;

            foreach (GridSlot slot in allSlots)
            {
                if (!slot.IsEmpty && slot.GetCard() != null)
                {
                    CardData building = slot.GetCard().cardData;
                    if (building == null) continue;

                    roundPopulation += building.CurrentPopulationIncome;
                    profile.baseIQ += building.CurrentIQBuff;
                    profile.baseCharisma += building.CurrentCharismaBuff;
                }
            }

            profile.totalPopulation += roundPopulation;
            if (gameManager != null) gameManager.AddResources(roundResources);

            profile.ValidateStats();
            if (uiManager != null) uiManager.RefreshStats();

            // ЗАПУСКАЕМ ЗАДЕРЖКУ И ПЕРЕХОД
            StartCoroutine(WaitAndStartRace());
        }

        // Вспомогательный метод (Корутина)
        private IEnumerator WaitAndStartRace()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveState();
            }

            yield return new WaitForSeconds(1.0f);
            SceneManager.LoadScene("Race");
        }

    }
}
