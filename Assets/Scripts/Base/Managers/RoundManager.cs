using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Core;

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

        [Header("Start Race Button")]
        [SerializeField] private Button startButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsChanged += RefreshStartButton;

            RefreshStartButton();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsChanged -= RefreshStartButton;
        }

        private void RefreshStartButton()
        {
            if (startButton == null) return;
            bool hasCockroach = IsCenterSlotFilled();
            startButton.interactable = hasCockroach;
        }

        private bool IsCenterSlotFilled()
        {
            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
                if (slot.gridX == 1 && slot.gridY == 1)
                    return !slot.IsEmpty;
            return false;
        }

        public void ClaimBonusDraft()
        {
            if (ProfileManager.Instance == null || draftManager == null) return;
            DisableBonusButton();
            draftManager.StartDraft();
        }

        private void DisableBonusButton()
        {
            if (getBonusButton == null) return;
            getBonusButton.interactable = false;
            var txt = getBonusButton.GetComponentInChildren<Text>();
            if (txt != null) txt.text = "ПОЛУЧЕНО";
        }

        public void ProcessEndOfRound()
        {
            SceneTransitionManager.Instance.BlockScreen();
            StartCoroutine(EndOfRoundSequence());
        }

        private IEnumerator EndOfRoundSequence()
        {
            if (ProfileManager.Instance == null || uiManager == null) yield break;
            ProfileManager.Instance.profile.dayCount += 1;
            PlayerProfile profile = ProfileManager.Instance.profile;

            if (profile.dayCount % 5 != 0)
                SceneTransitionManager.Instance?.PreloadScene("Race 1");
            else
                SceneTransitionManager.Instance?.PreloadScene("Race");

            profile.totalPopulation += profile.populationGrowth;
            profile.ValidateStats();

            yield return uiManager.AnimateStatsRefresh(profile.totalPopulation);

            GameManager.Instance?.SaveState();

            // Проверка победы
            if (profile.totalPopulation >= 1054)
            {
                SceneTransitionManager.Instance?.BlockScreen();
                EndGameScreen.Instance?.ShowVictory();
                yield break;
            }

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.CommitTransition();
            else
                SceneManager.LoadScene("Race");
        }
    }
}
