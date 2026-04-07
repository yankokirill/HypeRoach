using Game.Core;
using UnityEngine;

namespace Game.Race.Effects
{
    [System.Serializable]
    public class TeleportEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
            => enemies != null && enemies.Length > 0;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level)
        {
            EnemyCockroach target = FindClosestByDistance(player, enemies);
            if (target == null) return;

            float pProg = player.GetProgress();
            int pLane = player.currentLane;
            int pLap = player.currentLap;

            float tProg = target.GetProgress();
            int tLane = target.currentLane;
            int tLap = target.currentLap;

            player.SetProgressAndLap(tProg, tLap);
            target.SetProgressAndLap(pProg, pLap);

            player.TeleportToLane(tLane);
            target.TeleportToLane(pLane);

            target.LockLaneChanging(1.5f);

            if (VoiceManager.Instance != null)
            {
                VoiceManager.Instance.PlayTeleportVoice(pLane != tLane);
            }
            Debug.Log($"Мгновенная рокировка: {player.racerName} и {target.racerName}");
        }

        private EnemyCockroach FindClosestByDistance(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            EnemyCockroach closest = null;
            float minDist = float.MaxValue;
            Vector3 playerPos = player.transform.position;

            foreach (var e in enemies)
            {
                if (e == null) continue;

                // Евклидово расстояние
                float dist = Vector3.Distance(playerPos, e.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    closest = e;
                }
            }
            return closest;
        }
    }
}
