using UnityEngine;

namespace Game.Run
{
    public class EnemyCockroach : Cockroach
    {
        [Header("AI Settings")]
        [SerializeField] private float minLaneChangeTime = 1.5f; // Мин. время между мыслями о смене линии
        [SerializeField] private float maxLaneChangeTime = 3.5f; // Макс. время
        [SerializeField, Range(0f, 1f)] private float chanceToChangeLane = 0.4f; // Шанс 40% сменить линию

        private float aiTimer;
        private float laneLockTimer;

        public override void Initialize()
        {
            base.Initialize();
            // Даем случайную погрешность базовой скорости
            baseSpeed += Random.Range(0f, 0.1f);
            ResetAiTimer();
        }

        public override void UpdateState()
        {
            if (!isRacing) return;

            if (laneLockTimer > 0)
            {
                laneLockTimer -= Time.deltaTime;
            } else
            {
                aiTimer -= Time.deltaTime;
            }

            if (aiTimer <= 0)
            {
                DecideNextMove();
                ResetAiTimer();
            }
        }

        // Вызываем движение из базового класса!
        private void FixedUpdate()
        {
            Move();
        }

        private void DecideNextMove()
        {
            // Срабатывает с указанным шансом
            if (Random.value <= chanceToChangeLane)
            {
                int newLane;
                // Ищем любую линию, кроме текущей
                do
                {
                    newLane = Random.Range(0, 3);
                }
                while (newLane == currentLane);

                ChangeLane(newLane);
            }
        }

        private void ResetAiTimer()
        {
            aiTimer = Random.Range(minLaneChangeTime, maxLaneChangeTime);
        }

        public void LockLaneChanging(float duration)
        {
            laneLockTimer = duration;
        }
    }
}
