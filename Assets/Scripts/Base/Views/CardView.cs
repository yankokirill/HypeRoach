using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace Game.Base
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class CardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hand Mode")]
        [SerializeField] private GameObject handCard;
        [SerializeField] private Image handArt;
        [SerializeField] private Image handBackground;
        [SerializeField] private TMP_Text handName;
        [SerializeField] private TMP_Text handDescription;

        [Header("Field Mode")]
        [SerializeField] private GameObject fieldCard;
        [SerializeField] private Image fieldArt;
        [SerializeField] private TMP_Text fieldLevel;
        [SerializeField] private TMP_Text fieldCircles;

        public HandCardData handData { get; private set; }
        public FieldCard fieldData { get; private set; }

        public bool IsOnField => fieldData != null;

        public Hand currentHand { get; set; }
        public GridSlot currentSlot { get; set; }

        public Action<CardView> OnCardClicked;
        public Vector3 targetLocalPosition;

        private CanvasGroup _canvasGroup;
        private RectTransform _rt;
        private Transform _originalParent;
        private Canvas _rootCanvas;
        private Canvas RootCanvas
        {
            get
            {
                if (_rootCanvas == null)
                    _rootCanvas = GetComponentInParent<Canvas>();
                return _rootCanvas;
            }
        }

        private GridSlot _slotBeforeDrag;
        private bool _isDragging;

        // Целевой масштаб для плавного увеличения
        private Vector3 _targetScale = Vector3.one;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rt = GetComponent<RectTransform>();
        }

        private void Update()
        {
            // Плавно меняем масштаб каждый кадр (и при наведении, и при сбросе)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * 15f);

            if (_isDragging) return;
            if (currentSlot != null || currentHand == null) return;

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, targetLocalPosition, Time.deltaTime * 12f);
        }

        // ─── Hover (Наведение курсора) ────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;

            // Устанавливаем целевой масштаб в 1.15
            _targetScale = new Vector3(1.15f, 1.15f, 1.15f);

            // Если карта в руке, поднимаем её поверх всех остальных карт
            if (currentHand != null && currentSlot == null)
            {
                transform.SetAsLastSibling();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;

            // Возвращаем масштаб к исходному
            _targetScale = Vector3.one;

            // Возвращаем правильный Z-порядок (перекрытия) в руке
            if (currentHand != null && currentSlot == null)
            {
                currentHand.UpdateSiblingIndices();
            }
        }

        // ─── Click ─────────────────────────────────────────────────────────────
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging) return;

            if (currentSlot != null)
            {
                GameManager.Instance?.OnSlotClicked(currentSlot);
                return;
            }

            OnCardClicked?.Invoke(this);
        }

        // ─── Init ─────────────────────────────────────────────────────────────

        public void InitAsHandCard(HandCardData data)
        {
            handData = data;
            fieldData = null;
            Refresh();
        }

        public void InitAsFieldCard(FieldCard fc)
        {
            fieldData = fc;
            handData = null;
            Refresh();
        }

        // ─── Refresh ──────────────────────────────────────────────────────────

        public void Refresh()
        {
            if (IsOnField)
                ShowFieldMode();
            else
                ShowHandMode();
        }

        private void ShowHandMode()
        {
            handCard.SetActive(true);
            fieldCard.SetActive(false);

            handArt.sprite = handData.art;
            handBackground.sprite = handData.background;
            handName.text = handData.cardName;
            handDescription.text = handData.description;
        }

        private void ShowFieldMode()
        {
            handCard.SetActive(false);
            fieldCard.SetActive(true);

            fieldLevel.text = $"{fieldData.Level}";

            if (fieldCircles != null)
                fieldCircles.text = BuildCirclesString(
                    fieldData.TotalGreen,
                    fieldData.TotalWhite,
                    fieldData.TotalYellow);
        }

        private string BuildCirclesString(int green, int white, int yellow)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (green > 0)
                parts.Add($"<color=#33CC33>●{green}</color>");
            if (white > 0)
                parts.Add($"<color=#CCCCCC>●{white}</color>");
            if (yellow > 0)
                parts.Add($"<color=#FFD700>●{yellow}</color>");

            return string.Join(" ", parts);
        }

        // ─── Slot ─────────────────────────────────────────────────────────────

        public void SetSlot(GridSlot slot)
        {
            currentSlot = slot;
            currentHand = null;
        }

        // ─── Drag ─────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            _isDragging = true;
            _targetScale = Vector3.one; // Сбрасываем масштаб до обычного во время перетаскивания

            currentHand?.BeginDrag(this);
            _slotBeforeDrag = currentSlot;
            _originalParent = transform.parent;

            transform.SetAsLastSibling();

            Canvas canvas = RootCanvas;
            if (canvas == null) return;

            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;
        }

        public void OnDrag(PointerEventData e)
        {
            Canvas canvas = RootCanvas;
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                e.position, e.pressEventCamera, out Vector2 pos);
            _rt.localPosition = pos;

            currentHand?.UpdateCardOrder(this, e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            _isDragging = false;
            _targetScale = Vector3.one; // Убеждаемся, что масштаб сброшен

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            if (currentHand != null && currentSlot == null)
            {
                transform.SetParent(_originalParent, false);
            }
            else if (currentSlot != null && currentSlot == _slotBeforeDrag)
            {
                transform.SetParent(_originalParent, false);

                RectTransform rt = GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                // Scale плавно вернется в Update()
            }

            currentHand?.EndDrag();
            _slotBeforeDrag = null;
        }
    }
}
