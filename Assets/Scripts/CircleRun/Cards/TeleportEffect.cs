using UnityEngine;

namespace Game.CircleRun.Effects
{
    [System.Serializable]
    public class TeleportEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
            => enemies != null && enemies.Length > 0;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level)
        {
            EnemyCockroach target = FindClosestOnCircle(player, enemies);
            if (target == null) return;

            float pProg = player.GetProgress();
            int pLane = player.currentLane;

            float tProg = target.GetProgress();
            int tLane = target.currentLane;

            player.SetProgress(tProg);
            target.SetProgress(pProg);

            player.TeleportToLane(tLane);
            target.TeleportToLane(pLane);

            target.LockLaneChanging(1.5f);

            Debug.Log($"Мгновенная рокировка: {player.racerName} и {target.racerName}");
        }

        private EnemyCockroach FindClosestOnCircle(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            EnemyCockroach closest = null;
            float minDist = float.MaxValue;
            float pProg = player.GetProgress();

            foreach (var e in enemies)
            {
                if (e == null) continue;
                float diff = Mathf.Abs(pProg - e.GetProgress());
                float dist = Mathf.Min(diff, 1f - diff);
                if (dist < minDist) { minDist = dist; closest = e; }
            }
            return closest;
        }
    }
}
