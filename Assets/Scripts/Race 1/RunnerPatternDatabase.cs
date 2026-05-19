using System.Collections.Generic;
using UnityEngine;

namespace Game.EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Pattern Database", fileName = "RunnerPatternDatabase")]
    public class RunnerPatternDatabase : ScriptableObject
    {
        public List<RunnerPattern> patterns = new List<RunnerPattern>();

        public RunnerPattern GetRandom()
        {
            if (patterns == null || patterns.Count == 0) return null;
            return patterns[UnityEngine.Random.Range(0, patterns.Count)];
        }
    }
}
