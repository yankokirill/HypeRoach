using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game.Race
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CardView : MonoBehaviour
    {
        public int instanceId;

        [Header("Main UI")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private Image hotKey;
        [SerializeField] private Image artImage;

        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve successCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float shakeMagnitude = 0.1f; // Сила тряски при ошибке

        private Coroutine currentAnimation;
        private Vector3 originalScale;
        private Vector3 originalLocalPos;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            originalScale = transform.localScale;
            originalLocalPos = transform.localPosition; // Запоминаем позицию для тряски

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(CardInfo info, int id)
        {
            instanceId = id;

            if (nameText) nameText.text = info.title;
            if (descText) descText.text = info.description;
            if (manaText) manaText.text = info.mana.ToString();

            if (hotKey)
            {
                hotKey.sprite = info.hotKey;
                hotKey.gameObject.SetActive(info.hotKey != null);
            }

            if (artImage)
            {
                artImage.sprite = info.artImage;
                artImage.gameObject.SetActive(info.artImage != null);
            }
        }

        // Делаем карту тусклой, если ее нельзя разыграть
        public void SetPlayableState(bool isPlayable)
        {
            if (canvasGroup != null)
            {
                // Если нельзя разыграть, делаем полупрозрачной
                canvasGroup.alpha = isPlayable ? 1f : 0.4f;
            }
        }

        // --- АНИМАЦИИ ---

        public void PlaySuccessAnimation()
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            transform.localPosition = originalLocalPos; // Сбрасываем позицию на случай прерванной тряски
            currentAnimation = StartCoroutine(SuccessRoutine());
        }

        public void PlayErrorAnimation()
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            transform.localScale = originalScale; // Сбрасываем скейл на случай прерванной анимации успеха
            currentAnimation = StartCoroutine(ErrorRoutine());
        }

        // Анимация УСПЕХА (Провал внутрь и восстановление)
        private IEnumerator SuccessRoutine()
        {
            float elapsed = 0f;
            float halfDuration = 0.1f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(1f, 0.8f, successCurve.Evaluate(t));
                transform.localScale = originalScale * scale;
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(0.8f, 1f, successCurve.Evaluate(t));
                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
            currentAnimation = null;
        }

        // Анимация ОШИБКИ (Быстрая тряска влево-вправо)
        private IEnumerator ErrorRoutine()
        {
            float elapsed = 0f;
            float duration = 0.3f; // Длительность тряски

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Математика тряски: синусоида с затуханием
                float damping = 1f - (elapsed / duration); // Затухание к концу анимации
                float xOffset = Mathf.Sin(elapsed * 60f) * shakeMagnitude * damping;

                transform.localPosition = originalLocalPos + new Vector3(xOffset, 0, 0);

                yield return null;
            }

            transform.localPosition = originalLocalPos;
            currentAnimation = null;
        }
    }
}
