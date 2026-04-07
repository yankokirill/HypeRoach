using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Race
{
    [Serializable]
    public struct StickerMovementData
    {
        [Tooltip("Включить ли движение?")]
        public bool isMoving;

        [Header("Движение по линиям (Влево/Вправо)")]
        public float laneAmplitude; // На сколько линий отклоняется (например 1)
        public float laneSpeed;
        public float lanePhase;     // Фаза синусоиды (сдвиг)

        [Header("Движение по трассе (Вперед/Назад)")]
        [Tooltip("Амплитуда по прогрессу сплайна (например 0.05)")]
        public float progAmplitude;
        public float progSpeed;
        public float progPhase;
    }

    [Serializable]
    public class StickerSpawnData
    {
        public StickerType type;
        [Range(0, 2)] public int startLane;
        [Tooltip("Сдвиг вперед относительно начала паттерна (0.0, 0.02 и т.д.)")]
        public float progressOffset;

        [Header("Динамика")]
        public StickerMovementData movement;
    }

    [Serializable]
    public class StickerPattern
    {
        public string patternName = "New Pattern";
        public List<StickerSpawnData> stickers = new List<StickerSpawnData>();
    }
}
