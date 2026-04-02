using System.Collections.Generic;
using UnityEngine;

namespace Game.CircleRun
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Cockroach : MonoBehaviour
    {
        public string racerName = "Таракан";

        [Header("Movement Settings")]
        public float baseSpeed = 5f;
        public int currentLane = 1;
        public float laneChangeSpeed = 5f; // Скорость перехода между сплайнами

        [Header("State")]
        public bool isRacing = false;

        [Header("Hype Settings")]
        protected int hypeAmount = 0;

        protected float progress = 0f; // Прогресс круга от 0 до 1
        protected float visualLaneAlpha = 1f; // Для плавного перехода (0..1)
        protected int lastLane = 1;
        protected float bounceVelocity = 0f;

        protected Rigidbody2D rb;
        private Stack<int> laneHistory = new Stack<int>();
        private List<StatusEffect> activeEffects = new List<StatusEffect>();

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            visualLaneAlpha = 1f;
            lastLane = currentLane;
            laneHistory.Clear();
        }

        public void SnapToStart()
        {
            // Берем позицию начала (progress = 0) на выбранном сплайне
            Vector3 startPos = (Vector3)Race.GetLane(currentLane).EvaluatePosition(0);
            Vector3 forward = (Vector3)Race.GetLane(currentLane).EvaluateTangent(0);

            // Мгновенно перемещаем
            transform.position = startPos;
            rb.position = startPos;

            // Поворачиваем "по ходу движения"
            Quaternion startRot = Quaternion.LookRotation(forward, Vector3.forward);
            transform.rotation = Quaternion.Euler(0, 0, startRot.eulerAngles.z - 90);

            progress = 0f;
            lastLane = currentLane;
            visualLaneAlpha = 1f;
        }


        // --- ЛОГИКА ДВИЖЕНИЯ ---

        protected void Move()
        {
            if (!isRacing) return;

            UpdateEffects();

            // 1. Считаем скорость
            float speed = GetCurrentSpeed();

            // Получаем длину текущего сплайна, чтобы скорость была честной (метры в секунду)
            float currentSplineLength = Race.GetLane(currentLane).CalculateLength();

            // Увеличиваем прогресс (0..1)
            progress += (speed / currentSplineLength) * Time.fixedDeltaTime;
            progress %= 1f; // Зацикливаем

            // 2. Рассчитываем позицию на текущем сплайне
            Vector3 targetSplinePos = (Vector3)Race.GetLane(currentLane).EvaluatePosition(progress);
            Quaternion targetSplineRot = Quaternion.LookRotation(Race.GetLane(currentLane).EvaluateTangent(progress), Vector3.forward);

            // 3. Плавный переход между старой и новой линией
            if (lastLane != currentLane)
            {
                visualLaneAlpha = Mathf.MoveTowards(visualLaneAlpha, 1f, laneChangeSpeed * Time.fixedDeltaTime);
            }

            Vector3 startSplinePos = (Vector3)Race.GetLane(lastLane).EvaluatePosition(progress);

            // Итоговая позиция — это Lerp между двумя сплайнами на одном и том же прогрессе
            Vector3 finalPos = Vector3.Lerp(startSplinePos, targetSplinePos, visualLaneAlpha);

            // 4. Поворот (смотрим по касательной сплайна)
            // Мы тоже интерполируем поворот для плавности
            Quaternion startSplineRot = Quaternion.LookRotation(Race.GetLane(lastLane).EvaluateTangent(progress), Vector3.forward);
            Quaternion finalRot = Quaternion.Slerp(startSplineRot, targetSplineRot, visualLaneAlpha);

            rb.MovePosition(finalPos);
            rb.MoveRotation(finalRot.eulerAngles.z - 90); // -90 если спрайт смотрит вправо

            // Если переход закончен, фиксируем линию
            if (visualLaneAlpha >= 1f) lastLane = currentLane;
        }

        public void ChangeLane(int nextLane)
        {
            if (nextLane == currentLane) return;

            // Сохраняем текущую линию в историю ПЕРЕД сменой
            laneHistory.Push(currentLane);

            ApplyLaneChangeInternal(nextLane);
        }

        // ОТМОТКА (Вызывается картой Back)
        public void RewindLane()
        {
            if (laneHistory.Count > 0)
            {
                // Достаем последнюю посещенную линию
                int previous = laneHistory.Pop();

                // Применяем её БЕЗ сохранения в историю (чтобы не зациклиться)
                ApplyLaneChangeInternal(previous);

                Debug.Log($"{racerName} отмотал линию назад на {previous}. В истории осталось: {laneHistory.Count}");
            }
            else
            {
                Debug.Log($"{racerName} не может отмотать назад - история пуста!");
            }
        }

        // Внутренняя логика перестроения (общая для всех способов)
        private void ApplyLaneChangeInternal(int nextLane)
        {
            lastLane = currentLane;
            currentLane = Mathf.Clamp(nextLane, 0, 2);
            visualLaneAlpha = 0f;
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        public void BumpBack(float force) => bounceVelocity = -force;
        public void ResetBounce() => bounceVelocity = 0f;
        public float GetProgress() => progress;
        public void SetProgress(float p) => progress = p % 1f;

        public abstract void UpdateState();

        private void UpdateEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Update(Time.fixedDeltaTime))
                {
                    activeEffects[i].OnRemove();
                    activeEffects.RemoveAt(i);
                }
            }
            bounceVelocity = Mathf.MoveTowards(bounceVelocity, 0f, 15f * Time.fixedDeltaTime);
        }

        public void AddEffect(StatusEffect effect) => activeEffects.Add(effect);
        public void ClearEffects() => activeEffects.Clear();
        public bool IsInvulnerable()
        {
            foreach (var e in activeEffects) if (e.ProvidesInvulnerability()) return true;
            return false;
        }


        // Мгновенная смена линии (для телепорта)
        public void TeleportToLane(int nextLane)
        {
            if (nextLane == currentLane) return;

            // Сохраняем в стек истории, чтобы карта Back могла это отмотать
            laneHistory.Push(currentLane);

            currentLane = Mathf.Clamp(nextLane, 0, 2);

            lastLane = currentLane;
            visualLaneAlpha = 1f;

            // Обновляем позицию сразу в этом кадре (опционально, для точности)
            Vector3 snapPos = (Vector3)Race.GetLane(currentLane).EvaluatePosition(progress);
            rb.position = snapPos;
            transform.position = snapPos;
        }

        public virtual float GetCurrentSpeed()
        {
            float speed = baseSpeed;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                speed = activeEffects[i].ModifySpeed(speed);
            }
            return speed + bounceVelocity;
        }

        public virtual void AddHype(int amount)
        {
            hypeAmount += amount;
            UIManager.Instance.SetHype(this, hypeAmount);
        }
    }
}

