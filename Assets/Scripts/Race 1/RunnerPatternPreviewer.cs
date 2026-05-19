#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.EndlessRunner
{
    /// <summary>
    /// Превьюер паттернов для Endless Runner сцены.
    /// Аналог PatternPreviewer из Race-сцены.
    ///
    /// Как использовать:
    ///   1. Повесить на тот же GameObject, что и RunnerObstacleSpawner.
    ///   2. Включить enableLivePreview в Inspector.
    ///   3. Выбрать нужный паттерн через patternIndexToView.
    ///   4. В Scene View будут отрисованы Gizmo-сферы стикеров с анимацией движения.
    ///
    /// Требует RunnerLaneLayout в сцене для корректных Y-позиций дорожек.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RunnerObstacleSpawner))]
    public class RunnerPatternPreviewer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Preview Controls")]
        public bool enableLivePreview = false;

        [Tooltip("Индекс паттерна для просмотра")]
        public int patternIndexToView = 0;

        [Header("Simulation")]
        [Tooltip("Скорость симуляции движения стикеров")]
        public float timeScale = 1f;

        [Header("References")]
        public RunnerLaneLayout laneLayout;

        [Header("Display")]
        [Tooltip("X-позиция спавна в Scene View (правый край)")]
        public float previewSpawnX = 12f;

        [Tooltip("Размер Gizmo-сферы для каждого стикера")]
        public float gizmoRadius = 0.35f;

        [Tooltip("Показывать имена стикеров в Scene View")]
        public bool showLabels = true;

        // ── приватные ─────────────────────────────────────────────────────────

        private RunnerObstacleSpawner _spawner;
        private float _simulatedTime = 0f;
        private double _lastEditorTime;

        // Reflection-кэш поля patternDatabase в RunnerObstacleSpawner
        private static System.Reflection.FieldInfo _dbField;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            _spawner = GetComponent<RunnerObstacleSpawner>();
            _lastEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += EditorUpdate;

            // Кэшируем поле один раз
            _dbField ??= typeof(RunnerObstacleSpawner).GetField(
                "patternDatabase",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
        }

        // ── Editor update (анимация без Play Mode) ───────────────────────────

        private void EditorUpdate()
        {
            if (!enableLivePreview || Application.isPlaying) return;

            float dt = (float)(EditorApplication.timeSinceStartup - _lastEditorTime);
            _lastEditorTime = EditorApplication.timeSinceStartup;

            _simulatedTime += dt * timeScale;
            SceneView.RepaintAll();
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!enableLivePreview || Application.isPlaying || _spawner == null) return;

            if (laneLayout == null)
            {
                Handles.Label(transform.position + Vector3.up * 2f,
                    "⚠ Assign LaneLayout in RunnerPatternPreviewer Inspector",
                    EditorStyles.boldLabel);
                return;
            }

            var layout = laneLayout;

            // Получаем базу паттернов через Reflection
            RunnerPatternDatabase db = null;
            if (_dbField != null)
                db = _dbField.GetValue(_spawner) as RunnerPatternDatabase;

            if (db == null || db.patterns == null || db.patterns.Count == 0)
            {
                Handles.Label(transform.position + Vector3.up * 2f,
                    "⚠ No Pattern Database assigned to RunnerObstacleSpawner",
                    EditorStyles.boldLabel);
                return;
            }

            patternIndexToView = Mathf.Clamp(patternIndexToView, 0, db.patterns.Count - 1);
            RunnerPattern pattern = db.patterns[patternIndexToView];

            // ── Рисуем подложку паттерна ──────────────────────────────────────

            DrawPatternBackground(pattern, layout);

            // ── Рисуем каждый стикер ─────────────────────────────────────────

            for (int i = 0; i < pattern.stickers.Count; i++)
            {
                var sticker = pattern.stickers[i];
                DrawSticker(sticker, layout, i, pattern.stickers.Count);
            }

            // ── Лейбл паттерна ────────────────────────────────────────────────

            if (showLabels)
            {
                Vector3 labelPos = new Vector3(
                    previewSpawnX + pattern.patternWidth * 0.5f,
                    layout.GetLaneY(layout.LaneCount - 1) + 1.2f,
                    0f);

                GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    fontSize = 12
                };
                Handles.Label(labelPos, $"[{patternIndexToView}] {pattern.patternName}", style);
            }
        }

        // ── Отрисовка одного стикера ─────────────────────────────────────────

        private void DrawSticker(RunnerStickerSpawnData sticker, RunnerLaneLayout layout, int index, int total)
        {
            // Базовая X-позиция
            float x = previewSpawnX + sticker.xOffset;

            // Базовая Y по дорожке
            float laneFloat = sticker.startLane;

            // Анимация движения
            if (sticker.movement.isMoving)
            {
                float laneOffset = Mathf.Sin(
                    _simulatedTime * sticker.movement.laneSpeed + sticker.movement.lanePhase)
                    * sticker.movement.laneAmplitude;

                laneFloat = Mathf.Clamp(sticker.startLane + laneOffset, 0f, layout.LaneCount - 1f);

                float xOscil = Mathf.Sin(
                    _simulatedTime * sticker.movement.xSpeed + sticker.movement.xPhase)
                    * sticker.movement.xAmplitude;

                x += xOscil * 0.1f; // масштабируем, чтобы не улетало за экран
            }

            // Интерполяция Y между дорожками
            float y = LaneFloatToY(laneFloat, layout);

            Vector3 pos = new Vector3(x, y, 0f);

            // Цвет по типу
            Color col = StickerColor(sticker.type);

            // Внешняя сфера (обводка)
            Gizmos.color = new Color(col.r, col.g, col.b, 0.25f);
            Gizmos.DrawSphere(pos, gizmoRadius * 1.4f);

            // Основная сфера
            Gizmos.color = col;
            Gizmos.DrawSphere(pos, gizmoRadius);

            // Иконка в виде Handles-label
            if (showLabels)
            {
                string icon = sticker.type switch
                {
                    RunnerStickerType.Hype => "★",
                    RunnerStickerType.Death => "☠",
                    _ => "?"
                };

                string movingMark = sticker.movement.isMoving ? " ~" : "";
                string label = $"{icon} L{sticker.startLane}{movingMark}";

                GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = col },
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };

                Handles.Label(pos + Vector3.up * (gizmoRadius + 0.25f), label, style);
            }

            // Линия к базовой дорожке (если стикер движется)
            if (sticker.movement.isMoving)
            {
                float baseY = layout.GetLaneY(sticker.startLane);
                Gizmos.color = new Color(col.r, col.g, col.b, 0.4f);
                Gizmos.DrawLine(
                    new Vector3(x, baseY, 0f),
                    pos);
            }
        }

        // ── Подложка паттерна (дорожки + рамка) ─────────────────────────────

        private void DrawPatternBackground(RunnerPattern pattern, RunnerLaneLayout layout)
        {
            float x0 = previewSpawnX;
            float x1 = previewSpawnX + pattern.patternWidth;
            float laneStep = 0f;

            // Линии дорожек
            for (int lane = 0; lane < layout.LaneCount; lane++)
            {
                float y = layout.GetLaneY(lane);
                Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
                Gizmos.DrawLine(new Vector3(x0, y, 0f), new Vector3(x1, y, 0f));

                // Подпись дорожки
                if (showLabels)
                {
                    GUIStyle laneStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(1f, 1f, 1f, 0.4f) }
                    };
                    Handles.Label(new Vector3(x0 - 0.8f, y - 0.1f, 0f), $"Lane {lane}", laneStyle);
                }
            }

            // Рамка паттерна
            float yMin = layout.GetLaneY(0) - 0.7f;
            float yMax = layout.GetLaneY(layout.LaneCount - 1) + 0.7f;

            Gizmos.color = new Color(0.6f, 0.6f, 1f, 0.3f);
            Gizmos.DrawLine(new Vector3(x0, yMin, 0f), new Vector3(x0, yMax, 0f)); // левый край
            Gizmos.DrawLine(new Vector3(x1, yMin, 0f), new Vector3(x1, yMax, 0f)); // правый край
            Gizmos.DrawLine(new Vector3(x0, yMin, 0f), new Vector3(x1, yMin, 0f)); // низ
            Gizmos.DrawLine(new Vector3(x0, yMax, 0f), new Vector3(x1, yMax, 0f)); // верх
        }

        // ── Утилиты ────────────────────────────────────────────────────────────

        private static float LaneFloatToY(float laneFloat, RunnerLaneLayout layout)
        {
            int a = Mathf.Clamp(Mathf.FloorToInt(laneFloat), 0, layout.LaneCount - 1);
            int b = Mathf.Clamp(Mathf.CeilToInt(laneFloat), 0, layout.LaneCount - 1);
            float t = laneFloat - a;
            return Mathf.Lerp(layout.GetLaneY(a), layout.GetLaneY(b), t);
        }

        private static Color StickerColor(RunnerStickerType type) => type switch
        {
            RunnerStickerType.Hype => new Color(1f, 0.9f, 0.1f), // золотой
            RunnerStickerType.Death => new Color(1f, 0.2f, 0.2f), // красный
            _ => Color.white
        };
    }
}
#endif
