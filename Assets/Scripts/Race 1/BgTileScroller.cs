using Game.EndlessRunner;
using UnityEngine;

public class BgTileScroller : MonoBehaviour
{
    public float tileWidth; // 16.64

    void Update()
    {
        float speed = RunnerManager.Instance != null
            ? RunnerManager.Instance.WorldSpeed * 0.4f // множитель параллакса
            : 2f;

        transform.position += Vector3.left * speed * Time.deltaTime;

        // Когда тайл ушёл за левый край — телепортируем вправо
        if (transform.position.x <= -tileWidth)
            transform.position += new Vector3(tileWidth * 2f, 0f, 0f);
    }
}
