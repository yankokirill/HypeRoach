using UnityEngine;
using Game.Race;
using System.Collections.Generic;

namespace Game.Core
{
    [System.Serializable]
    public class TeleportVoiceData
    {
        public AudioClip clip;
        [Tooltip("Если true, фраза звучит только когда таракан сменил дорожку. Если false — подходит для любого телепорта.")]
        public bool requiresDifferentLanes;
    }

    [RequireComponent(typeof(AudioSource))]
    public class VoiceManager : MonoBehaviour
    {
        public static VoiceManager Instance { get; private set; }

        [Header("Audio Components")]
        [SerializeField] private AudioSource voiceAudioSource;

        [Header("Sticker Voices Source")]
        [SerializeField] private AudioClip[] hypeVoices;
        [SerializeField] private AudioClip[] sabotageVoices;

        [Header("Ability Voices Source")]
        [SerializeField] private AudioClip[] dashVoices;
        [SerializeField] private TeleportVoiceData[] teleportVoices; // Теперь это массив структур

        [Header("Settings")]
        [Tooltip("Через сколько стикеров пытаться сказать фразу.")]
        [SerializeField] private Vector2Int voiceTriggerRange = new Vector2Int(1, 1);

        // Плейлисты
        private List<AudioClip> hypePlaylist = new List<AudioClip>();
        private List<AudioClip> sabotagePlaylist = new List<AudioClip>();
        private List<AudioClip> dashPlaylist = new List<AudioClip>();
        private List<TeleportVoiceData> teleportPlaylist = new List<TeleportVoiceData>();

        // Индексы
        private int currentHypeIndex = 0;
        private int currentSabotageIndex = 0;
        private int currentDashIndex = 0;
        private int currentTeleportIndex = 0;

        private int playerStickerCounter = 0;
        private int nextVoiceTarget = 1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlaylists();
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (voiceAudioSource == null)
                voiceAudioSource = GetComponent<AudioSource>();

            SetNextVoiceTarget();
        }

        private void InitializePlaylists()
        {
            PreparePlaylist(hypeVoices, hypePlaylist);
            PreparePlaylist(sabotageVoices, sabotagePlaylist);
            PreparePlaylist(dashVoices, dashPlaylist);

            // Отдельная обработка для телепорта
            if (teleportVoices != null && teleportVoices.Length > 0)
            {
                teleportPlaylist = new List<TeleportVoiceData>(teleportVoices);
                ShuffleList(teleportPlaylist);
            }

            currentHypeIndex = 0;
            currentSabotageIndex = 0;
            currentDashIndex = 0;
            currentTeleportIndex = 0;
        }

        private void PreparePlaylist(AudioClip[] source, List<AudioClip> target)
        {
            if (source != null && source.Length > 0)
            {
                target.Clear();
                target.AddRange(source);
                ShuffleList(target);
            }
        }

        public void ResetRaceCounters()
        {
            playerStickerCounter = 0;
            if (voiceAudioSource != null) voiceAudioSource.Stop();
            SetNextVoiceTarget();
        }

        #region Trigger Methods

        public void OnCollectedSticker(StickerType type, bool collected)
        {
            if (!collected) return;
            playerStickerCounter++;

            if (playerStickerCounter >= nextVoiceTarget)
            {
                if (type == StickerType.Hype)
                    PlayFromPlaylist(hypePlaylist, ref currentHypeIndex);
                else
                    PlayFromPlaylist(sabotagePlaylist, ref currentSabotageIndex);

                playerStickerCounter = 0;
                SetNextVoiceTarget();
            }
        }

        public void PlayDashVoice()
        {
            PlayFromPlaylist(dashPlaylist, ref currentDashIndex);
        }

        public void PlayTeleportVoice(bool changedLane)
        {
            if (voiceAudioSource == null || voiceAudioSource.isPlaying || teleportPlaylist.Count == 0) return;

            // Ищем подходящую фразу, начиная с текущего индекса
            int startIndex = currentTeleportIndex;

            for (int i = 0; i < teleportPlaylist.Count; i++)
            {
                int checkIndex = (startIndex + i) % teleportPlaylist.Count;
                var data = teleportPlaylist[checkIndex];

                if (!changedLane && data.requiresDifferentLanes)
                    continue;

                voiceAudioSource.clip = data.clip;
                voiceAudioSource.Play();

                currentTeleportIndex = (checkIndex + 1) % teleportPlaylist.Count;
                return;
            }
        }

        #endregion

        private void PlayFromPlaylist(List<AudioClip> playlist, ref int index)
        {
            if (voiceAudioSource == null || voiceAudioSource.isPlaying || playlist.Count == 0) return;

            AudioClip clipToPlay = playlist[index];
            index = (index + 1) % playlist.Count;

            voiceAudioSource.clip = clipToPlay;
            voiceAudioSource.Play();
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int k = Random.Range(0, i + 1);
                T value = list[k];
                list[k] = list[i];
                list[i] = value;
            }
        }

        private void SetNextVoiceTarget()
        {
            nextVoiceTarget = Random.Range(voiceTriggerRange.x, voiceTriggerRange.y + 1);
        }
    }
}
