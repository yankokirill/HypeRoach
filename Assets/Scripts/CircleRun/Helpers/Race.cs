using System;
using UnityEngine;
using UnityEngine.Splines;

public class Race : MonoBehaviour
{
    public static Race Instance { get; private set; }

    [Header("Track Settings")]
    public SplineContainer[] laneSplines;

    public static SplineContainer GetLane(int index)
    {
        return Instance?.laneSplines[index];
    }

    public static SplineContainer GetRandomLane()
    {
        int randomLane = UnityEngine.Random.Range(0, 3);
        return GetLane(randomLane);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}