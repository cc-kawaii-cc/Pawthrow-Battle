using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject 
{
    public float damage = 10f; 
    public float knockbackForce = 15f;

    [Header("Stun Effect")]
    public bool isHeavyItem = false;
    public float stunDuration = 2f;
}