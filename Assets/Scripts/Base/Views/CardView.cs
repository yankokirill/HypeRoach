using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CardData cardData;

        [Header("UI References")]
        [SerializeField] private Text cardNameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text levelText;
        [SerializeField] private Image descriptionPanel;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image headerImage;
        [SerializeField] private Image levelImage;

        [Header("Animation Settings")]
        public float hoverScale = 1.15f;
        public float scaleTransitionSpeed = 15f;
        public float moveTransitionSpeed = 15f;

        private CanvasGroup canvasGroup;
        private Transform originalParent;
        public GridSlot currentSlot { get; set; }
        public Hand currentHand { get; set; }

        public bool isDragging { get; private set; }
        private float targetScale = 1f;
        private float defaultScale = 1f;
        public Vector3 targetLocalPosition { get; set; }
        public Action<CardView> OnCardClicked;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            if (isDragging)
            {
                isDragging = false;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDefaultHint();
                }
            }
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * scaleTransitionSpeed);

            if (!isDragging && currentSlot == null && currentHand != null)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * moveTransitionSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * moveTransitionSpeed);
            }
        }

        public void Initialize(CardData data)
        {
            cardData = data;
            Refresh(data.level);
            SetVisualMode(false);
        }

        public void Refresh(int level)
        {
            if (cardData == null) return;
            if (cardNameText != null) cardNameText.text = cardData.cardName;
            if (frameImage != null) frameImage.sprite = cardData.art;
            if (descriptionText != null) descriptionText.text = cardData.CurrentEffectText;
            if (levelText != null) levelText.text = level.ToString();
        }

        public void SetSlot(GridSlot slot) => currentSlot = slot;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentSlot != null || isDragging) return;
            targetScale = hoverScale * defaultScale;
            if (currentHand != null) transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (currentSlot != null || isDragging) return;
            targetScale = defaultScale;
            if (currentHand != null) transform.SetSiblingIndex(currentHand.GetCardIndex(this));
        }

        // --- ИСПРАВЛЕННЫЙ КЛИК ---
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;

            // 1. Если карта на сетке — имитируем клик по слоту
            if (currentSlot != null)
            {
                GameManager.Instance.OnSlotClicked(currentSlot);
                return;
            }

            // 2. Если карта в Драфте (нет ни руки, ни слота)
            if (currentHand == null && currentSlot == null)
            {
                OnCardClicked?.Invoke(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            targetScale = 1f;
            originalParent = transform.parent;

            if (currentSlot != null)
                UIManager.Instance.ShowHint("Перетаскивание...", "Объедините со зданием того же уровня!");

            Transform targetLayer = GameManager.Instance.dragLayer != null
                ? GameManager.Instance.dragLayer
                : GetComponentInParent<Canvas>().transform;

            transform.SetParent(targetLayer);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            transform.position = eventData.position;

            // Пересчитываем порядок в руке, только если карта из руки
            if (currentHand != null)
            {
                currentHand.UpdateCardOrder(this, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            canvasGroup.blocksRaycasts = true;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowDefaultHint();

            if (currentSlot != null)
            {
                currentSlot.PlaceCard(this);
            }
            else if (currentHand != null)
            {
                transform.SetParent(currentHand.handParent);
                transform.SetSiblingIndex(currentHand.GetCardIndex(this));
                SetVisualMode(false);
            }
        }

        public void SetVisualMode(bool isOnGrid)
        {
            if (cardNameText != null) cardNameText.gameObject.SetActive(!isOnGrid);
            if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(!isOnGrid);
            if (headerImage != null) headerImage.gameObject.SetActive(!isOnGrid);

            RectTransform rt = GetComponent<RectTransform>();
            if (isOnGrid)
            {
                levelImage.gameObject.SetActive(true);
                rt.sizeDelta = new Vector2(160, 160);
                if (frameImage != null) frameImage.transform.localPosition = Vector3.zero;
            }
        }

        public void SetScale(float scale)
        {
            defaultScale = scale;
            targetScale = scale;
            transform.localScale = Vector3.one * scale;
        }
    }
}
