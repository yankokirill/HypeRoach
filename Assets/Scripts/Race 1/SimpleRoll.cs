using UnityEngine;
using Game.EndlessRunner;

public class SimpleScroll : MonoBehaviour
{
    [Tooltip("Множитель: насколько быстро фон движется относительно WorldSpeed.\n" +
             "0.1 = медленный дальний план, 0.5 = средний, 1.0 = совпадает с миром.")]
    public float speedMultiplier = 0.1f;

    [Tooltip("Включить, если RunnerManager недоступен (например, в главном меню).\n" +
             "Тогда используется фиксированная скорость ниже.")]
    public float fallbackSpeed = 0.5f;

    private Renderer rend;
    private float _offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float speed = RunnerManager.Instance != null
            ? RunnerManager.Instance.WorldSpeed * speedMultiplier
            : fallbackSpeed;

        _offset += speed * Time.deltaTime;
        rend.material.mainTextureOffset = new Vector2(_offset, 0);
    }
}
