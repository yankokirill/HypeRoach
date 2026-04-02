using UnityEngine;

namespace Game.Run.Effects
{
    [System.Serializable]
    public class SwapPlayerEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            return FindClosest(player, enemies) != null;
        }

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            EnemyCockroach closest = FindClosest(player, enemies);
            if (closest == null) return;

            // 1. —охран€ем ѕќЋЌџ≈ позиции (X и Y)
            Vector2 playerPos = player.transform.position;
            Vector2 enemyPos = closest.transform.position;

            // 2. —охран€ем логические индексы линий
            int playerLane = player.currentLane;
            int enemyLane = closest.currentLane;

            // 3. ћгновенно перемещаем физические тела (X и Y мен€ютс€ здесь)
            player.TeleportTo(enemyPos);
            closest.TeleportTo(playerPos);

            // 4. ќбновл€ем логику линий, чтобы Move() не т€нул их обратно на старые Y
            player.ChangeLane(enemyLane);
            closest.ChangeLane(playerLane);

            // 5. ќпционально: сбрасываем скорость "отскока", 
            // чтобы после свапа тараканов не продолжало "колбасить" от старых столкновений
            player.ResetBounce();
            closest.ResetBounce();

            // 6. 1.5 сек враг не мен€ет линию
            closest.LockLaneChanging(1.5f);

            Debug.Log($"—вап! {player.racerName} теперь на позиции {enemyPos.x}, а враг на {playerPos.x}");
        }

        private EnemyCockroach FindClosest(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            EnemyCockroach closest = null;
            float minDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                // —читаем дистанцию по вектору (и горизонталь, и вертикаль)
                float dist = Vector2.Distance(player.transform.position, enemy.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemy;
                }
            }
            return closest;
        }
    }
}
