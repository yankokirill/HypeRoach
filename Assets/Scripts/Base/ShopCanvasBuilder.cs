// ShopCanvasBuilder.cs
// Canvas строится процедурно в Start().
// Вызови Open() / Close() / Toggle() из любой кнопки.
// Зависимости: TextMeshPro

using Game.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Base
{
    [Serializable]
    public class ShopEntry
    {
        public HandCardData card;
        public int price = 100;
    }

    public class ShopCanvasBuilder : MonoBehaviour
    {
        [Header("Кнопка открытия магазина")]
        public Button openShopButton;

        [Header("Товары — редкие улучшения (DedInside, Baryga)")]
        public List<ShopEntry> rareItems = new();

        [Header("Товары — легендарные улучшения (Berserk, Tusovshchik)")]
        public List<ShopEntry> legendaryItems = new();

        [Header("Рост популяции — покупка в магазине")]
        [Tooltip("Сколько стоит одна единица прироста популяции")]
        public int growthPricePerUnit = 40;
        [Tooltip("Сколько единиц прироста даётся за одну покупку")]
        public int growthAmountPerPurchase = 5;

        public Sprite growthIcon;

        // ── Цвета ──────────────────────────────────────────────────────────────
        [Header("Цвета")]
        public Color panelBg = new Color(0.10f, 0.10f, 0.12f, 0.97f);
        public Color headerBg = new Color(0.07f, 0.07f, 0.09f, 1.00f);
        public Color footerBg = new Color(0.07f, 0.07f, 0.09f, 1.00f);
        public Color cardBg = new Color(0.17f, 0.17f, 0.20f, 1.00f);
        public Color cardBorder = new Color(1.00f, 1.00f, 1.00f, 0.08f);
        public Color rareLabelColor = new Color(0.72f, 0.68f, 1.00f, 1.00f);
        public Color legendaryLabelColor = new Color(1.00f, 0.78f, 0.25f, 1.00f);
        public Color tooltipBg = new Color(0.20f, 0.18f, 0.05f, 0.60f);
        public Color tooltipTextColor = new Color(0.95f, 0.78f, 0.20f, 1.00f);
        public Color textPrimary = new Color(0.92f, 0.92f, 0.92f, 1.00f);
        public Color textSecondary = new Color(0.58f, 0.58f, 0.62f, 1.00f);
        public Color textDanger = new Color(0.95f, 0.35f, 0.35f, 1.00f);
        public Color buyBtnBg = new Color(0.22f, 0.40f, 0.22f, 1.00f);
        public Color buyBtnDisabled = new Color(0.22f, 0.22f, 0.24f, 1.00f);
        public Color closeBtnBg = new Color(0.55f, 0.15f, 0.15f, 1.00f);

        // ── Внутренние поля ────────────────────────────────────────────────────
        private Canvas _canvas;
        private GameObject _panel;
        private TMP_Text _balanceLabel;
        private TMP_Text _growthLabel;   // текущий прирост популяции
        private Button _growthButton;  // кнопка покупки роста

        private readonly List<CardRow> _cardRows = new();

        // Прибавка к каждому font-size во всём магазине — меняй одно число
        private const int FontSizeDelta = 18;

        // ══════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════════════

        private void Start()
        {
            BuildCanvas();
            _canvas.gameObject.SetActive(false);

            // Подписываемся на кнопку открытия из инспектора
            if (openShopButton != null)
                openShopButton.onClick.AddListener(Open);

            if (GameManager.Instance != null)
                GameManager.Instance.OnResourcesChanged += _ => RefreshAll();
        }

        private void OnDestroy()
        {
            // Отписываемся от кнопки
            if (openShopButton != null)
                openShopButton.onClick.RemoveListener(Open);

            if (GameManager.Instance != null)
                GameManager.Instance.OnResourcesChanged -= _ => RefreshAll();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════════════

        // Включаем / выключаем объект _canvas
        public void Open() { _canvas.gameObject.SetActive(true); RefreshAll(); }
        public void Close() { _canvas.gameObject.SetActive(false); }
        public void Toggle() { if (_canvas.gameObject.activeSelf) Close(); else Open(); }

        // ══════════════════════════════════════════════════════════════════════
        // Canvas root
        // ══════════════════════════════════════════════════════════════════════

        private void BuildCanvas()
        {
            // ── Canvas ─────────────────────────────────────────────────────────
            var canvasGo = new GameObject("ShopCanvas");

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Затемнение на весь экран ────────────────────────────────────────
            var overlay = Stretch("Overlay", canvasGo.transform);
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.60f);
            var ovBtn = overlay.AddComponent<Button>();
            ovBtn.transition = Selectable.Transition.None;
            ovBtn.onClick.AddListener(Close);

            // ── Панель магазина — stretch с отступами от краёв ──────────────────
            _panel = Stretch("ShopPanel", canvasGo.transform);
            var pRt = _panel.GetComponent<RectTransform>();
            pRt.offsetMin = new Vector2(120, 60);
            pRt.offsetMax = new Vector2(-120, -60);

            _panel.AddComponent<Image>().color = panelBg;
            _panel.AddComponent<GraphicRaycaster>(); // блокирует клики сквозь панель

            BuildHeader(_panel.transform);
            BuildBody(_panel.transform);
            BuildFooter(_panel.transform);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Header  — top 70px
        // ══════════════════════════════════════════════════════════════════════

        private void BuildHeader(Transform parent)
        {
            var go = Stretch("Header", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(0, -70);
            rt.offsetMax = Vector2.zero;

            go.AddComponent<Image>().color = headerBg;

            // Название магазина
            var title = Txt(go.transform, "Title", "Магазин улучшений",
                new Vector2(0, 0), new Vector2(0.65f, 1),
                new Vector2(20, 0), Vector2.zero);
            title.fontSize = 26 + FontSizeDelta;
            title.fontStyle = FontStyles.Bold;
            title.color = textPrimary;
            title.alignment = TextAlignmentOptions.MidlineLeft;

            // Плашка баланса
            var balBg = Stretch("BalanceBg", go.transform);
            var bRt = balBg.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.72f, 0.15f);
            bRt.anchorMax = new Vector2(0.97f, 0.85f);
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;
            balBg.AddComponent<Image>().color = new Color(0.20f, 0.20f, 0.26f);

            _balanceLabel = Txt(balBg.transform, "BalLabel", "◈  0  ресурсов",
                Vector2.zero, Vector2.one,
                new Vector2(8, 0), new Vector2(-8, 0));
            _balanceLabel.fontSize = 18 + FontSizeDelta;
            _balanceLabel.fontStyle = FontStyles.Bold;
            _balanceLabel.color = textPrimary;
            _balanceLabel.alignment = TextAlignmentOptions.Center;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Body  — между header (top 70) и footer (bottom 60)
        // ══════════════════════════════════════════════════════════════════════

        private void BuildBody(Transform parent)
        {
            var go = Stretch("Body", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(0, 60);  // над footer
            rt.offsetMax = new Vector2(0, -70);  // под header

            // Левая колонка — редкие (занимает левые 40%)
            BuildSection(go.transform,
                new Vector2(0f, 0f), new Vector2(0.4f, 1f),
                "Редкие улучшения", rareLabelColor,
                "После улучшения скрещивание недоступно",
                rareItems);

            // Разделитель 1
            MakeDivider(go.transform, 0.4f);

            // Средняя колонка — легендарные (40–80%)
            BuildSection(go.transform,
                new Vector2(0.4f, 0f), new Vector2(0.8f, 1f),
                "Легендарные улучшения", legendaryLabelColor,
                "Одно легендарное улучшение на таракана",
                legendaryItems);

            // Разделитель 2
            MakeDivider(go.transform, 0.8f);

            // Правая колонка — рост популяции (80–100%)
            BuildGrowthSection(go.transform,
                new Vector2(0.8f, 0f), new Vector2(1f, 1f));
        }

        private void MakeDivider(Transform parent, float xAnchor)
        {
            var div = Stretch("Divider", parent);
            var dRt = div.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(xAnchor, 0.04f);
            dRt.anchorMax = new Vector2(xAnchor, 0.96f);
            dRt.offsetMin = new Vector2(-1, 0);
            dRt.offsetMax = new Vector2(1, 0);
            div.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Growth section — правая колонка
        // ══════════════════════════════════════════════════════════════════════

        private void BuildGrowthSection(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = Stretch("Sec_Growth", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);

            go.AddComponent<Image>().color = cardBg;

            // Заголовок — верхние 12%
            var lblGo = Stretch("Label", go.transform);
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.88f);
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var growthColor = new Color(0.30f, 0.85f, 0.60f);
            lblGo.AddComponent<Image>().color =
                new Color(growthColor.r, growthColor.g, growthColor.b, 0.08f);
            var lTxt = Txt(lblGo.transform, "LTxt", "РОСТ\nПОПУЛЯЦИИ",
                Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, 0));
            lTxt.fontSize = 14 + FontSizeDelta;
            lTxt.fontStyle = FontStyles.Bold;
            lTxt.color = growthColor;
            lTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Иконка / иллюстрация — 9–78%
            var iconGo = Stretch("Icon", go.transform);
            var iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.20f, 0.55f);
            iRt.anchorMax = new Vector2(0.80f, 0.82f);
            iRt.offsetMin = iRt.offsetMax = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            if (growthIcon != null)
            {
                iconImg.sprite = growthIcon;
                iconImg.preserveAspect = true; // чтобы картинка не сплющивалась
            }

            // Текущий прирост — 40–52%
            var curGo = Stretch("CurrentGrowth", go.transform);
            var cgRt = curGo.GetComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(0, 0.38f);
            cgRt.anchorMax = new Vector2(1, 0.52f);
            cgRt.offsetMin = cgRt.offsetMax = Vector2.zero;
            _growthLabel = Txt(curGo.transform, "GrowthTxt", "",
                Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            _growthLabel.fontSize = 13 + FontSizeDelta;
            _growthLabel.color = textSecondary;
            _growthLabel.alignment = TextAlignmentOptions.Center;
            _growthLabel.enableWordWrapping = true;

            // Описание — 20–38%
            var descGo = Stretch("Desc", go.transform);
            var drRt = descGo.GetComponent<RectTransform>();
            drRt.anchorMin = new Vector2(0, 0.20f);
            drRt.anchorMax = new Vector2(1, 0.38f);
            drRt.offsetMin = drRt.offsetMax = Vector2.zero;
            var descTxt = Txt(descGo.transform, "DescTxt",
                $"+{growthAmountPerPurchase} к росту за {growthPricePerUnit * growthAmountPerPurchase} руб",
                Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            descTxt.fontSize = 12 + FontSizeDelta;
            descTxt.color = textSecondary;
            descTxt.alignment = TextAlignmentOptions.Center;
            descTxt.enableWordWrapping = true;

            // Кнопка — 2–18%
            var btnGo = Stretch("GrowthBtn", go.transform);
            var bRt = btnGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.05f, 0.02f);
            bRt.anchorMax = new Vector2(0.95f, 0.18f);
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;
            var growthBtnColor = new Color(0.15f, 0.35f, 0.25f);
            btnGo.AddComponent<Image>().color = growthBtnColor;

            _growthButton = btnGo.AddComponent<Button>();
            var gc = _growthButton.colors;
            gc.normalColor = growthBtnColor;
            gc.highlightedColor = new Color(0.20f, 0.45f, 0.32f);
            gc.pressedColor = new Color(0.10f, 0.25f, 0.18f);
            gc.disabledColor = buyBtnDisabled;
            _growthButton.colors = gc;
            _growthButton.onClick.AddListener(OnBuyGrowth);

            var btnLbl = Txt(btnGo.transform, "BtnLbl", "Купить",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btnLbl.fontSize = 14 + FontSizeDelta;
            btnLbl.fontStyle = FontStyles.Bold;
            btnLbl.color = Color.white;
            btnLbl.alignment = TextAlignmentOptions.Center;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Section
        // ══════════════════════════════════════════════════════════════════════

        private void BuildSection(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            string label, Color labelColor,
            string tooltip, List<ShopEntry> items)
        {
            var go = Stretch($"Sec_{label}", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);

            // Заголовок секции — верхние 12%
            var lblGo = Stretch("Label", go.transform);
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.88f);
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            lblGo.AddComponent<Image>().color =
                new Color(labelColor.r, labelColor.g, labelColor.b, 0.08f);

            var lTxt = Txt(lblGo.transform, "LTxt", label.ToUpper(),
                Vector2.zero, Vector2.one,
                new Vector2(14, 0), new Vector2(-14, 0));
            lTxt.fontSize = 14 + FontSizeDelta;
            lTxt.fontStyle = FontStyles.Bold;
            lTxt.color = labelColor;
            lTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Тултип — следующие 9%
            var tipGo = Stretch("Tip", go.transform);
            var tRt = tipGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.78f);
            tRt.anchorMax = new Vector2(1, 0.88f);
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;
            tipGo.AddComponent<Image>().color = tooltipBg;

            var tTxt = Txt(tipGo.transform, "TTxt", $"!!! {tooltip}",
                Vector2.zero, Vector2.one,
                new Vector2(10, 0), new Vector2(-10, 0));
            tTxt.fontSize = 11 + FontSizeDelta;
            tTxt.color = tooltipTextColor;
            tTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Зона карточек — нижние 78%
            var cardsZone = Stretch("CardsZone", go.transform);
            var czRt = cardsZone.GetComponent<RectTransform>();
            czRt.anchorMin = new Vector2(0, 0);
            czRt.anchorMax = new Vector2(1, 0.78f);
            czRt.offsetMin = new Vector2(0, 6);
            czRt.offsetMax = new Vector2(0, -4);

            BuildCardRow(cardsZone.transform, items);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Card row — карточки делят ширину поровну
        // ══════════════════════════════════════════════════════════════════════

        private void BuildCardRow(Transform parent, List<ShopEntry> items)
        {
            if (items == null || items.Count == 0) return;
            int n = items.Count;

            for (int i = 0; i < n; i++)
            {
                var entry = items[i];
                if (entry?.card == null) continue;

                float x0 = (float)i / n;
                float x1 = (float)(i + 1) / n;

                var slot = Stretch($"CardSlot_{i}", parent);
                var sRt = slot.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(x0, 0);
                sRt.anchorMax = new Vector2(x1, 1);
                sRt.offsetMin = new Vector2(5, 0);
                sRt.offsetMax = new Vector2(-5, 0);

                _cardRows.Add(BuildCard(slot.transform, entry));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Single card
        // ══════════════════════════════════════════════════════════════════════

        private CardRow BuildCard(Transform parent, ShopEntry entry)
        {
            // Фон
            var bg = Stretch("Bg", parent);
            bg.AddComponent<Image>().color = cardBg;
            var ol = bg.AddComponent<Outline>();
            ol.effectColor = cardBorder;
            ol.effectDistance = new Vector2(1, -1);

            // Арт — верхние 44%
            var artGo = Stretch("Art", parent);
            var aRt = artGo.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.05f, 0.52f);
            aRt.anchorMax = new Vector2(0.95f, 0.96f);
            aRt.offsetMin = aRt.offsetMax = Vector2.zero;
            var artImg = artGo.AddComponent<Image>();
            if (entry.card.art != null)
            {
                artImg.sprite = entry.card.art;
                artImg.color = Color.white;
                artImg.preserveAspect = true;
            }
            else
            {
                artImg.color = new Color(0.25f, 0.25f, 0.30f);
            }

            // Название — 11%
            var nameTxt = Txt(parent, "Name", entry.card.cardName,
                new Vector2(0.05f, 0.41f), new Vector2(0.95f, 0.52f),
                Vector2.zero, Vector2.zero);
            nameTxt.fontSize = 15 + FontSizeDelta;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = textPrimary;
            nameTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Описание — 22%
            string desc = !string.IsNullOrEmpty(entry.card.effectText)
                ? entry.card.effectText : entry.card.description;
            var descTxt = Txt(parent, "Desc", desc,
                new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.41f),
                Vector2.zero, Vector2.zero);
            descTxt.fontSize = 11 + FontSizeDelta;
            descTxt.color = textSecondary;
            descTxt.alignment = TextAlignmentOptions.TopLeft;
            descTxt.overflowMode = TextOverflowModes.Ellipsis;
            descTxt.enableWordWrapping = true;

            // Цена — нижний левый угол
            var priceTxt = Txt(parent, "Price", $"  {entry.price}\n     руб",
                new Vector2(0.05f, 0.02f), new Vector2(0.50f, 0.18f),
                Vector2.zero, Vector2.zero);
            priceTxt.fontSize = 15 + FontSizeDelta;
            priceTxt.fontStyle = FontStyles.Bold;
            priceTxt.color = textPrimary;
            priceTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Кнопка «Купить» — нижний правый угол
            var btnGo = Stretch("BuyBtn", parent);
            var bRt = btnGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.52f, 0.02f);
            bRt.anchorMax = new Vector2(0.95f, 0.18f);
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;
            btnGo.AddComponent<Image>().color = buyBtnBg;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = buyBtnBg;
            colors.highlightedColor = new Color(
                buyBtnBg.r + 0.10f, buyBtnBg.g + 0.10f, buyBtnBg.b + 0.05f);
            colors.pressedColor = new Color(
                buyBtnBg.r - 0.05f, buyBtnBg.g - 0.05f, buyBtnBg.b);
            colors.disabledColor = buyBtnDisabled;
            btn.colors = colors;

            var btnLbl = Txt(btnGo.transform, "BtnLbl", "Купить",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btnLbl.fontSize = 13 + FontSizeDelta;
            btnLbl.fontStyle = FontStyles.Bold;
            btnLbl.color = Color.white;
            btnLbl.alignment = TextAlignmentOptions.Center;

            ShopEntry cap = entry;
            btn.onClick.AddListener(() => OnBuy(cap));

            return new CardRow { entry = entry, priceText = priceTxt, buyButton = btn };
        }

        // ══════════════════════════════════════════════════════════════════════
        // Footer — bottom 60px
        // ══════════════════════════════════════════════════════════════════════

        private void BuildFooter(Transform parent)
        {
            var go = Stretch("Footer", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 60);

            go.AddComponent<Image>().color = footerBg;

            var closeBtnGo = Stretch("CloseBtn", go.transform);
            var cbRt = closeBtnGo.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.38f, 0.15f);
            cbRt.anchorMax = new Vector2(0.62f, 0.85f);
            cbRt.offsetMin = cbRt.offsetMax = Vector2.zero;
            closeBtnGo.AddComponent<Image>().color = closeBtnBg;

            var closeBtn = closeBtnGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            var lbl = Txt(closeBtnGo.transform, "Lbl", "Закрыть",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lbl.fontSize = 16 + FontSizeDelta;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Покупка роста популяции
        // ══════════════════════════════════════════════════════════════════════

        private void OnBuyGrowth()
        {
            if (GameManager.Instance == null || ProfileManager.Instance == null) return;

            int totalCost = growthPricePerUnit * growthAmountPerPurchase;

            if (!GameManager.Instance.CanAfford(totalCost))
            {
                Debug.LogWarning($"[Shop] Недостаточно ресурсов для роста: нужно {totalCost}");
                return;
            }

            GameManager.Instance.SpendResources(totalCost);
            ProfileManager.Instance.profile.populationGrowth += growthAmountPerPurchase;

            Debug.Log($"[Shop] Куплен рост популяции +{growthAmountPerPurchase}. " +
                      $"Итого: {ProfileManager.Instance.profile.populationGrowth}");

            RefreshAll();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Покупка карты

        private void OnBuy(ShopEntry entry)
        {
            if (GameManager.Instance == null) return;

            if (!GameManager.Instance.CanAfford(entry.price))
            {
                Debug.LogWarning($"[Shop] Недостаточно ресурсов: нужно {entry.price}");
                return;
            }
            if (GameManager.Instance.playerHand.Count >= GameManager.Instance.playerHand.maxHandSize)
            {
                Debug.LogWarning("[Shop] Рука полна.");
                return;
            }

            GameManager.Instance.SpendResources(entry.price);
            GameManager.Instance.SpawnHandCard(entry.card);
            Debug.Log($"[Shop] Куплено «{entry.card.cardName}» за {entry.price}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Обновление кнопок и баланса
        // ══════════════════════════════════════════════════════════════════════

        private void RefreshAll()
        {
            int bal = GameManager.Instance?.GetCurrentResources() ?? 0;
            if (_balanceLabel != null)
                _balanceLabel.text = $"{bal}  руб";

            bool handFull = GameManager.Instance != null &&
                GameManager.Instance.playerHand.Count >= GameManager.Instance.playerHand.maxHandSize;

            // Обновляем карточки улучшений
            foreach (var row in _cardRows)
            {
                bool ok = (GameManager.Instance?.CanAfford(row.entry.price) ?? false) && !handFull;
                row.buyButton.interactable = ok;
                row.priceText.color = ok ? textPrimary : textDanger;
            }

            // Обновляем секцию роста популяции
            int growth = ProfileManager.Instance?.profile.populationGrowth ?? 0;
            if (_growthLabel != null)
                _growthLabel.text = $"Текущий прирост:\n+{growth} за раунд";

            int growthCost = growthPricePerUnit * growthAmountPerPurchase;
            bool canAffordGrowth = GameManager.Instance?.CanAfford(growthCost) ?? false;
            if (_growthButton != null)
                _growthButton.interactable = canAffordGrowth;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Хелперы
        // ══════════════════════════════════════════════════════════════════════

        /// Создаёт GO с RectTransform, полностью растянутым на родитель (0,0 → 1,1, offset=0)
        private static GameObject Stretch(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        /// Создаёт TextMeshProUGUI с anchor-позиционированием
        private static TMP_Text Txt(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Данные строки карточки
        // ══════════════════════════════════════════════════════════════════════

        private class CardRow
        {
            public ShopEntry entry;
            public TMP_Text priceText;
            public Button buyButton;
        }
    }
}
