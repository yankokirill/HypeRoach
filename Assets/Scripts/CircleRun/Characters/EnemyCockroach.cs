using UnityEngine;

namespace Game.CircleRun
{
    public class EnemyCockroach : Cockroach
    {
        [Header("AI Intelligence")]
        public float decisionInterval = 2f;

        private float aiTimer = 2f;
        private float laneLockTimer = 0f;

        public override void UpdateState()
        {
            if (!isRacing) return;

            if (laneLockTimer > 0)
            {
                laneLockTimer -= Time.deltaTime;
            }
            else
            {
                aiTimer -= Time.deltaTime;
            }

            if (aiTimer <= 0)
            {
                DecideLaneChange();
                aiTimer = decisionInterval + Random.Range(-0.5f, 0.5f);
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void DecideLaneChange()
        {
            // Простая логика: 30% шанс сменить линию на случайную соседнюю
            if (Random.value < 0.3f)
            {
                int direction = Random.value < 0.5f ? 1 : 2;
                ChangeLane((currentLane + direction) % 3);
            }
        }

        public void LockLaneChanging(float duration)
        {
            laneLockTimer = duration;
        }
    }
}
