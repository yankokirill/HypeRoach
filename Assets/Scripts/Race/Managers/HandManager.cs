using UnityEngine;

namespace Game.Race
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

        void Update()
        {
            HandleInput();
            UpdateCardsPlayability();
        }

        private void HandleInput()
        {
            for (int i = 0; i < handCardViews.Length; i++)
            {
                if (Input.GetKeyDown(hotKeys[i]))
                {
                    bool success = RaceManager.Instance.TryPlayCard(i);

                    if (success)
                    {
                        handCardViews[i].PlaySuccessAnimation();
                    } else
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

        private void PlaySuccessAnimation(int index)
        {
            if (index < handCardViews.Length && handCardViews[index] != null)
            {
                handCardViews[index].PlaySuccessAnimation();
            }
        }
    }
}
