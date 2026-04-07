#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.Race
{
    [ExecuteAlways] // Заставляет скрипт работать в редакторе без Play Mode
    [RequireComponent(typeof(RaceManager))]
    public class PatternPreviewer : MonoBehaviour
    {
        [Header("Preview Controls")]
        public bool enableLivePreview = false;

        [Tooltip("Какой паттерн из базы смотрим")]
        public int patternIndexToView = 0;

        [Range(0f, 1f)]
        public float baseProgressOnTrack = 0.5f;

        [Header("Simulation")]
        [Tooltip("Увеличь, чтобы симуляция шла быстрее")]
        public float timeScale = 1f;
        public Race race;

        private RaceManager raceManager;
        private float simulatedTime = 0f;
        private double lastEditorTime;

        private void OnEnable()
        {
            raceManager = GetComponent<RaceManager>();
            lastEditorTime = EditorApplication.timeSinceStartup;
            // Подписываемся на обновление редактора
            EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
        }

        private void EditorUpdate()
        {
            if (!enableLivePreview || Application.isPlaying) return;

            // Вычисляем дельту времени в редакторе
            float dt = (float)(EditorApplication.timeSinceStartup - lastEditorTime);
            lastEditorTime = EditorApplication.timeSinceStartup;

            // Двигаем "фейковое" время для симуляции синусоид
            simulatedTime += dt * timeScale;

            // Заставляем окно Scene перерисовываться каждый кадр
            SceneView.RepaintAll();
        }

        private void OnDrawGizmos()
        {
            // Работает только если включено превью и мы НЕ в Play Mode
            if (!enableLivePreview || Application.isPlaying || raceManager == null) return;

            // Получаем доступ к приватной базе паттернов через Reflection (или сделай её public/internal)
            var dbField = typeof(RaceManager).GetField("patternDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dbField == null) return;

            var patternDatabase = dbField.GetValue(raceManager) as List<StickerPattern>;
            if (patternDatabase == null || patternDatabase.Count == 0) return;

            patternIndexToView = Mathf.Clamp(patternIndexToView, 0, patternDatabase.Count - 1);
            StickerPattern pattern = patternDatabase[patternIndexToView];

            foreach (var sticker in pattern.stickers)
            {
                // Вычисляем ДИНАМИЧЕСКИЕ параметры на основе simulatedTime
                float currentProg = (baseProgressOnTrack + sticker.progressOffset) % 1f;
                float targetLaneFloat = sticker.startLane;

                if (sticker.movement.isMoving)
                {
                    float laneOffset = Mathf.Sin(simulatedTime * sticker.movement.laneSpeed + sticker.movement.lanePhase) * sticker.movement.laneAmplitude;
                    targetLaneFloat = Mathf.Clamp(sticker.startLane + laneOffset, 0f, 2f);

                    float progOffset = Mathf.Sin(simulatedTime * sticker.movement.progSpeed + sticker.movement.progPhase) * sticker.movement.progAmplitude;
                    currentProg = (currentProg + progOffset) % 1f;
                    if (currentProg < 0f) currentProg += 1f;
                }

                // 1. ПОЗИЦИЯ
                Vector3 pos0 = race.laneSplines[0].EvaluatePosition(currentProg);
                Vector3 pos1 = race.laneSplines[1].EvaluatePosition(currentProg);
                Vector3 pos2 = race.laneSplines[2].EvaluatePosition(currentProg);

                Vector3 currentPos;
                if (targetLaneFloat <= 1f) currentPos = Vector3.Lerp(pos0, pos1, targetLaneFloat);
                else currentPos = Vector3.Lerp(pos1, pos2, targetLaneFloat - 1f);

                // 2. ЦВЕТ
                Color gizmoColor = Color.white;
                switch (sticker.type)
                {
                    case StickerType.Hype: gizmoColor = Color.yellow; break;
                    case StickerType.Sabotage: gizmoColor = Color.red; break;
                    case StickerType.Arrow: gizmoColor = Color.cyan; break;
                }

                // 3. ОТРИСОВКА ЖИВОГО СТИКЕРА
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(currentPos, 0.4f); // Рисуем плотный шар

            }
        }
    }
}
#endif
