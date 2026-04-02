using UnityEngine;

namespace Game.Run
{
    public class HandManager : MonoBehaviour
    {
        public static HandManager Instance { get; private set; }

        [Header("UI Cards")]
        public CardView[] handCardViews;

        // Оставили 4 хоткея
        readonly KeyCode[] hotKeys = new KeyCode[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Initialize()
        {
            RaceManager.Instance.OnCardPlayed += PlayCardAnimation;
        }

        private void OnDestroy()
        {
            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.OnCardPlayed -= PlayCardAnimation;
            }
        }

        void Update()
        {
            if (!RaceManager.Instance.IsRaceActive) return;

            HandleCardSelectionInput();
            UpdateCardsPlayability();
        }

        private void HandleCardSelectionInput()
        {
            for (int i = 0; i < handCardViews.Length; i++)
            {
                if (Input.GetKeyDown(hotKeys[i]))
                {
                    bool success = RaceManager.Instance.TryPlayCard(i);

                    // Если разыграть не вышло (нет маны или Check() = false)
                    if (!success)
                    {
                        handCardViews[i].PlayErrorAnimation();
                    }
                }
            }
        }

        // Обновляем визуал: делаем карту тусклой, если ее нельзя использовать
        private void UpdateCardsPlayability()
        {
            for (int i = 0; i < handCardViews.Length; i++)
            {
                bool canPlay = RaceManager.Instance.CanPlayCard(i);
                handCardViews[i].SetPlayableState(canPlay);
            }
        }

        private void PlayCardAnimation(int index)
        {
            if (index < handCardViews.Length && handCardViews[index] != null)
            {
                handCardViews[index].PlaySuccessAnimation();
            }
        }
    }
}
