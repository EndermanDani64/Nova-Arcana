using UnityEngine;

[CreateAssetMenu(menuName = "Backrooms/BiomeData")]
public class BiomeData : ScriptableObject
{
    public Biome biomeType;

    [Header("Generálás")]
    public float generationChance;
    public int labirynthCount;
    public float stopCollisionProbability;
    public float randomFactorForDoors;
    public int patchIterationCount;

    [Header("Szobák")]
    public int roomCount;
    public int roomMinSize;
    public int roomMaxSize;

    [Header("Lighting")]
    public float lightChance;

    [Header("Vizuális")]
    //public Material wallMaterial;
    public GameObject wallPrefab;

    [Header("Debug")]
    public Color debugColor;
}