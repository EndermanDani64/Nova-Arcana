using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Backrooms/BiomeData")]
[Serializable]
public class BiomeData : ScriptableObject
{
    public Biome biomeType;

    [Header("Generálás")]
    [SerializeField] public float generationChance;
    [SerializeField] public int labirynthCount;
    [SerializeField] public float stopCollisionProbability;
    [SerializeField] public float randomFactorForDoors;
    [SerializeField] public int patchIterationCount;

    [Header("Szobák")]
    [SerializeField] public int roomCount;
    [SerializeField] public int roomMinSize;
    [SerializeField] public int roomMaxSize;

    [Header("Lighting")]
    [SerializeField] public float lightChance;

    [Header("Vizuális")]
    //public Material wallMaterial;
    [SerializeField] public GameObject wallPrefab;

    [Header("Debug")]
    [SerializeField] public Color debugColor;
}