using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Run
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Cockroach : MonoBehaviour
    {
        public string racerName = "Таракан";

        [Header("Movement & Lanes")]
        public float baseSpeed = 2f;
        public int currentLane = 1;
        [SerializeField] private float laneChangeSpeed = 10f;

        [Header("Hype Settings")]
        public int maxHype = 100; // Максимальное количество очков хайпа
        protected int hypeAmount = 0; // Текущий хайп

        protected Rigidbody2D rb;
        public bool isRacing = false;
        protected float bounceVelocity = 0f;

        // СОБЫТИЯ
        public event Action<int, int> UpdateHypeUI;
        public event Action<int> OnHypeChanged;

        // Список активных эффектов
        private List<StatusEffect> activeEffects = new List<StatusEffect>();

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // В раннерах по линиям лучше использовать Kinematic, чтобы избежать багов с отскоками
            rb.isKinematic = true;
        }

        // --- МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА И ЛОГИКИ ---

        public virtual void Initialize()
        {
            hypeAmount = 0;
            ClearEffects();
            UpdateHypeUI?.Invoke(hypeAmount, maxHype);
        }

        public virtual void Stop()
        {
            isRacing = false;
            ClearEffects(); // Снимаем все эффекты после финиша
        }

        public virtual void AddHype(int amount)
        {
            hypeAmount += amount;
            OnHypeChanged?.Invoke(hypeAmount);
        }

        public void SetMaxHype(int newMaxHype)
        {
            maxHype = newMaxHype;
            UpdateHypeUI?.Invoke(hypeAmount, maxHype);
        }

        // ---------------------------------------

        // УПРАВЛЕНИЕ ЭФФЕКТАМИ
        public void AddEffect(StatusEffect effect) => activeEffects.Add(effect);
        public void RemoveEffect(StatusEffect effect) => activeEffects.Remove(effect);
        public void ClearEffects() => activeEffects.Clear();

        public bool IsInvulnerable()
        {
            foreach (var effect in activeEffects)
                if (effect.ProvidesInvulnerability()) return true;
            return false;
        }

        // ПОЛУЧЕНИЕ ИТОГОВОЙ СКОРОСТИ
        public virtual float GetCurrentSpeed()
        {
            float speed = baseSpeed;
            foreach (var effect in activeEffects)
            {
                speed = effect.ModifySpeed(speed);
            }
            return speed + bounceVelocity;
        }

        public void ChangeLane(int targetLane) => currentLane = Mathf.Clamp(targetLane, 0, 2);

        public void BumpBack(float force) => bounceVelocity = -force;

        public abstract void UpdateState();

        // ЕДИНАЯ ЛОГИКА ДВИЖЕНИЯ (Вызывать из FixedUpdate дочерних классов!)
        protected void Move()
        {
            if (!isRacing) return;

            // Обработка эффектов
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Update(Time.fixedDeltaTime))
                {
                    activeEffects[i].OnRemove();
                    activeEffects.RemoveAt(i);
                }
            }

            // Затухание отбрасывания (BumpBack)
            bounceVelocity = Mathf.MoveTowards(bounceVelocity, 0f, 15f * Time.fixedDeltaTime);

            float speed = GetCurrentSpeed();
            float targetY = RaceManager.Instance.GetLaneY(currentLane);

            // Плавное движение без багов с коллизиями
            Vector2 newPos = new Vector2(
                transform.position.x + speed * Time.fixedDeltaTime,
                Mathf.MoveTowards(transform.position.y, targetY, laneChangeSpeed * Time.fixedDeltaTime)
            );

            rb.MovePosition(newPos);
        }

        public void TeleportTo(Vector2 newPosition)
        {
            rb.MovePosition(newPosition);
            transform.position = newPosition;
        }

        public void ResetBounce()
        {
            bounceVelocity = 0f;
        }
    }
}
