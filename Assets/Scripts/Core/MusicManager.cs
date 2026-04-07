using UnityEngine;
using System.Collections;

namespace Game.Core
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private AudioSource musicAudioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip baseMusicClip; // Музыка для меню/хаба
        [SerializeField] private AudioClip raceMusicClip; // Музыка для гонки

        [Header("Volume Settings")]
        [Range(0, 1)][SerializeField] private float maxVolume = 0.4f;
        [Range(0, 1)][SerializeField] private float minVolume = 0.1f;
        [SerializeField] private float fadeDuration = 2.0f;
        [SerializeField] private float sceneTransitionFadeOut = 1.0f;

        private Coroutine _musicFadeRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // --- МЕТОДЫ ПЕРЕКЛЮЧЕНИЯ ---

        public void StartBaseMusic()
        {
            TransitionToClip(baseMusicClip);
        }

        public void StartRaceMusic()
        {
            TransitionToClip(raceMusicClip);
        }

        // Старый метод StartMusic для совместимости с RaceManager
        public void StartMusic() => StartRaceMusic();

        // --- ЛОГИКА ПЕРЕХОДА ---

        private void TransitionToClip(AudioClip clip)
        {
            if (clip == null) return;

            // Если этот клип уже играет — просто убеждаемся, что громкость на максимуме
            if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
            {
                if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = StartCoroutine(FadeVolume(maxVolume, fadeDuration));
                return;
            }

            // Если играет другой клип или тишина — запускаем полный переход
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            // 1. Уводим текущую громкость в ноль
            if (musicAudioSource.isPlaying)
            {
                yield return StartCoroutine(FadeVolume(0f, fadeDuration / 2f));
            }

            // 2. Меняем трек
            musicAudioSource.clip = newClip;
            musicAudioSource.Play();

            // 3. Поднимаем громкость до максимума
            yield return StartCoroutine(FadeVolume(maxVolume, fadeDuration / 2f));
        }

        public void LowerMusicAtEnd()
        {
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(FadeVolume(minVolume, fadeDuration));
        }

        public void StopMusicCompletely()
        {
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(FadeVolume(0f, sceneTransitionFadeOut));
        }

        private IEnumerator FadeVolume(float targetVolume, float duration)
        {
            float startVolume = musicAudioSource.volume;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            musicAudioSource.volume = targetVolume;
            if (targetVolume <= 0) musicAudioSource.Stop();

            _musicFadeRoutine = null;
        }
    }
}
