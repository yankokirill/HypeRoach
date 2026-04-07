using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Game.Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance;

        [Header("Timing Settings")]
        [SerializeField] private float fadeOutDuration = 0.4f;
        [SerializeField] private float fadeInDuration = 0.8f;

        [Header("Components")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        private AsyncOperation _asyncOperation;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (fadeCanvasGroup == null) fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();

                fadeCanvasGroup.alpha = 0;
                fadeCanvasGroup.blocksRaycasts = false;
                fadeCanvasGroup.gameObject.SetActive(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void BlockScreen()
        {
            fadeCanvasGroup.blocksRaycasts = true;
        }

        public void PreloadScene(string sceneName)
        {
            _asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            _asyncOperation.allowSceneActivation = false;
        }

        public void CommitTransition()
        {
            if (MusicManager.Instance != null) MusicManager.Instance.StopMusicCompletely();
            if (_asyncOperation == null) return;
            StopAllCoroutines();
            StartCoroutine(CommitRoutine());
        }

        // Обычный метод для простых переходов без предзагрузки
        public void LoadScene(string sceneName)
        {
            PreloadScene(sceneName);
            CommitTransition();
        }

        private IEnumerator CommitRoutine()
        {
            fadeCanvasGroup.blocksRaycasts = true;
            // 1. Быстро затемняем экран
            yield return StartCoroutine(Fade(1f, fadeOutDuration));

            // 2. Активируем сцену, пока экран черный
            _asyncOperation.allowSceneActivation = true;
            while (!_asyncOperation.isDone)
                yield return null;

            // Ждем один кадр, чтобы новая сцена "проснулась"
            yield return new WaitForEndOfFrame();

            // 3. ПЛАВНО открываем новую сцену
            yield return StartCoroutine(Fade(0f, fadeInDuration));

            fadeCanvasGroup.blocksRaycasts = false;
            _asyncOperation = null;
        }

        // Обновленный метод Fade теперь принимает длительность
        private IEnumerator Fade(float targetAlpha, float duration)
        {
            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;

            if (duration <= 0)
            {
                fadeCanvasGroup.alpha = targetAlpha;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float smoothedT = Mathf.SmoothStep(0, 1, t);

                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothedT);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
        }
    }
}
