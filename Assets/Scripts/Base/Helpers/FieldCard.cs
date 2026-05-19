// FieldCard.cs
using System.Collections.Generic;
using UnityEngine;

namespace Game.Base
{
    public class FieldCard
    {
        public HandCardData Source { get; private set; }
        public int Level { get; private set; } = 1;
        public const int MaxLevel = 4;

        // Статы масштабируются линейно с уровнем
        public int TotalHype { get; private set; }
        public int TotalEvasion { get; private set; }

        // Кружочки — аккумуляторы, заполняются при создании и суммируются при мерже
        public int TotalGreen { get; private set; }
        public int TotalWhite { get; private set; }
        public int TotalYellow { get; private set; }

        private readonly List<UpgradeType> _upgrades = new();
        public IReadOnlyList<UpgradeType> Upgrades => _upgrades;

        public bool HasLegendary => _upgrades.Exists(u => u.IsLegendary());
        public bool IsUpgraded => _upgrades.Count > 0;
        public bool CanMerge => !IsUpgraded && Level < MaxLevel;

        // ─── Фабрика ───────────────────────────────────────────────────────────
        public static FieldCard FromHand(HandCardData source)
        {
            Debug.Assert(source.type == CardType.Building,
                $"[FieldCard] Попытка создать FieldCard из улучшения: {source.cardName}");
            return new FieldCard
            {
                Source = source,
                TotalGreen = source.greenCount,
                TotalWhite = source.whiteCount,
                TotalYellow = source.yellowCount,
                TotalHype = source.baseHype,
                TotalEvasion = source.baseEvasion,
            };
        }

        // ─── Слияние ───────────────────────────────────────────────────────────
        public bool TryMerge(FieldCard donor)
        {
            if (!CanMerge || !donor.CanMerge) return false;
            if (Level >= MaxLevel || Level != donor.Level) return false;

            Level++;

            // Суммируем кружочки донора в базовую карту
            TotalGreen += donor.TotalGreen;
            TotalWhite += donor.TotalWhite;
            TotalYellow += donor.TotalYellow;

            TotalEvasion += donor.TotalEvasion;
            TotalHype += donor.TotalHype;

            return true;
        }

        // ─── Применение улучшения ──────────────────────────────────────────────
        public bool TryApplyUpgrade(HandCardData upgradeCard)
        {
            if (upgradeCard.type != CardType.Upgrade)
            {
                Debug.LogWarning("[FieldCard] Это не карта улучшения.");
                return false;
            }

            UpgradeType upgrade = upgradeCard.upgradeType;

            if (upgrade == UpgradeType.None)
            {
                Debug.LogWarning("[FieldCard] UpgradeType.None — настройте карту в инспекторе.");
                return false;
            }

            if (upgrade.IsLegendary() && HasLegendary)
            {
                Debug.LogWarning($"[FieldCard] Уже есть легендарное улучшение на {Source.cardName}");
                return false;
            }

            _upgrades.Add(upgrade);
            return true;
        }

        // ─── Загрузка из сохранения ────────────────────────────────────────────
        public static FieldCard FromSave(HandCardData source, int level, List<UpgradeType> upgrades,
            int green, int white, int yellow)
        {
            var fc = new FieldCard
            {
                Source = source,
                TotalGreen = green,
                TotalWhite = white,
                TotalYellow = yellow,
            };
            fc.Level = Mathf.Clamp(level, 1, MaxLevel);
            foreach (var u in upgrades) fc._upgrades.Add(u);
            return fc;
        }
    }
}
