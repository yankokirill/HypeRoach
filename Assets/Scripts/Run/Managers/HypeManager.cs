using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

namespace Game.Run
{
    public class HypeManager : MonoBehaviour
    {
        public static HypeManager Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI multiplierText;

        private List<Cockroach> monitoredRacers = new List<Cockroach>();
        private int globalMaxHype = 0;
        private int multiplierValue = 1;

        public int GlobalMax => globalMaxHype;

        private int maxHype = 100;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Initialize(List<Cockroach> racers)
        {
            monitoredRacers = racers;

            foreach (var racer in monitoredRacers)
            {
                racer.OnHypeChanged += UpdateHype;
            }
        }

        private void OnDestroy()
        {
            foreach (var racer in monitoredRacers)
            {
                racer.OnHypeChanged -= UpdateHype;
            }
        }

        public void UpdateHype(int current)
        {
            if (current >= maxHype)
            {
                Debug.Log($"🚀 {current} достиг лимита! Удваиваем предел.");
                maxHype *= 2;
                multiplierValue++;
                multiplierText.text = "x" + multiplierValue.ToString();
            }

            foreach (var racer in monitoredRacers)
            {
                racer.SetMaxHype(maxHype);
            }

        }
    }
}
