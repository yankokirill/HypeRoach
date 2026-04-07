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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ClaimBonusDraft()
        {
            if (ProfileManager.Instance == null || draftManager == null) return;

            DisableBonusButton();
            Debug.Log("Запуск бонусного выбора карт!");
            draftManager.StartDraft();
        }

        private void DisableBonusButton()
        {
            if (getBonusButton != null)
            {
                getBonusButton.interactable = false;
                Text btnText = getBonusButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "ПОЛУЧЕНО";
            }
        }

        public void ProcessEndOfRound()
        {
            SceneTransitionManager.Instance.BlockScreen();
            // Запускаем полную последовательность действий
            StartCoroutine(EndOfRoundSequence());
        }

        private IEnumerator EndOfRoundSequence()
        {
            // 1. ПРОВЕРКИ
            if (ProfileManager.Instance == null || uiManager == null) yield break;
            PlayerProfile profile = ProfileManager.Instance.profile;

            // 2. НАЧИНАЕМ ПРЕДЗАГРУЗКУ СЦЕНЫ В ФОНЕ
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.PreloadScene("Race");

            // 3. РАССЧИТЫВАЕМ НОВЫЕ СТАТЫ
            int roundPopulation = 0;
            int newIQ = 0;
            int newCharisma = 0;

            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
            foreach (GridSlot slot in allSlots)
            {
                if (!slot.IsEmpty && slot.GetCard() != null)
                {
                    CardData building = slot.GetCard().cardData;
                    if (building == null) continue;
                    roundPopulation += building.CurrentPopulationIncome;
                    newIQ += building.CurrentIQBuff;
                    newCharisma += building.CurrentCharismaBuff;
                }
            }
            int finalPopulation = profile.totalPopulation + roundPopulation;

            profile.totalPopulation = finalPopulation;
            profile.baseIQ = newIQ;
            profile.baseCharisma = newCharisma;
            profile.ValidateStats();

            // 4. ЗАПУСКАЕМ АНИМАЦИЮ В UI И ЖДЕМ ЕЕ ОКОНЧАНИЯ
            yield return uiManager.AnimateStatsRefresh(profile.totalPopulation, profile.baseIQ, profile.baseCharisma);

            // gameManager.AddResources(roundResources);

            // 5. СОХРАНЯЕМ ИГРУ
            if (GameManager.Instance != null)
                GameManager.Instance.SaveState();

            // 6. ЗАПУСКАЕМ ПЕРЕХОД
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.CommitTransition();
            else
                SceneManager.LoadScene("Race");
        }
    }
}
