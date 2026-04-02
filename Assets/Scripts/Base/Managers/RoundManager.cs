using UnityEngine;
using Game.Core;

namespace Game.Base
{
    public class RoundManager : MonoBehaviour
    {
        [Header("References")]
        public GameManager gameManager;
        public UIManager uiManager;
        public DraftManager draftManager;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            {
                // Блокируем завершение следующего раунда, если игрок еще выбирает карту
                if (draftManager != null && draftManager.IsDrafting) return;

                ProcessEndOfRound();
            }
        }

        public void ProcessEndOfRound()
        {
            if (ProfileManager.Instance == null) return;
            PlayerProfile profile = ProfileManager.Instance.profile;

            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);

            int roundResources = 0;
            int roundPopulation = 0;

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

            // Начисление ресурсов
            if (gameManager != null)
            {
                gameManager.AddResources(roundResources);
            }
            else
            {
                profile.currentResources += roundResources;
            }

            profile.ValidateStats();
            Debug.Log($"Раунд завершен! +{roundPopulation} Тараканов, +{roundResources} Ресурсов.");

            // Обновление UI
            if (uiManager != null)
                uiManager.RefreshStats();

            // Проверка победы
            if (profile.totalPopulation >= 1000)
            {
                Debug.Log("🏆 ВЫ ПОБЕДИЛИ! Достигнуто 1000 Тараканов!");
            }

            // Вызываем панель выбора карт в конце раунда
            if (draftManager != null)
            {
                draftManager.StartDraft();
            }
        }
    }
}