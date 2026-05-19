using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.EndlessRunner
{
    // ─── Типы препятствий ───────────────────────────────────────────────────────

    public enum RunnerStickerType
    {
        Hype,   // монетка / бонус — собирай
        Death   // мгновенная смерть — уклоняйся
    }

    // ─── Движение стикера ───────────────────────────────────────────────────────

    [Serializable]
    public struct RunnerStickerMovement
    {
        [Tooltip("Двигается ли стикер вертикально во время полёта?")]
        public bool isMoving;

        [Header("Вертикальное колебание (дорожки)")]
        [Tooltip("Амплитуда в дорожках (например 1 = прыгает на ±1 дорожку)")]
        public float laneAmplitude;
        public float laneSpeed;
        public float lanePhase;

        [Header("Горизонтальное колебание")]
        [Tooltip("Дополнительное смещение по X (синусоида)")]
        public float xAmplitude;
        public float xSpeed;
        public float xPhase;
    }

    // ─── Один стикер в паттерне ─────────────────────────────────────────────────

    [Serializable]
    public class RunnerStickerSpawnData
    {
        public RunnerStickerType type;

        [Range(0, 3)]
        public int startLane;

        [Tooltip("Смещение по X относительно начала паттерна (0 = голова, >0 = дальше вправо)")]
        public float xOffset;

        public RunnerStickerMovement movement;
    }

    // ─── Паттерн ────────────────────────────────────────────────────────────────

    [Serializable]
    public class RunnerPattern
    {
        public string patternName = "New Pattern";

        [Tooltip("Суммарная ширина паттерна в юнитах. " +
                 "Следующий паттерн спавнится с отступом от xOffset последнего стикера + patternWidth")]
        public float patternWidth = 6f;

        public List<RunnerStickerSpawnData> stickers = new List<RunnerStickerSpawnData>();
    }
}
