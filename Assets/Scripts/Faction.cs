using UnityEngine;
public enum FactionType
{
    Player,
    Enemy,
    Neutral
}
public class Faction : MonoBehaviour
{
    [SerializeField] private FactionType factionType;

    public FactionType Type => factionType;
}