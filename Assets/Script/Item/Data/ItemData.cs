using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject 
{
    public float damage = 10f; 
    public float knockbackForce = 15f;

    [Header("Stun Effect")]
    public bool isHeavyItem = false;
    public float stunDuration = 2f;
    
    [Header("Destroy Settings")]
    public bool destroyOnHitPlayer = true;  // default: ทำลายเมื่อชนผู้เล่น
    public bool destroyOnHitGround = false; // บางไอเทม (เช่น ลูกบอล) กลิ้งต่อได้
    public float destroyDelay = 0.1f;       // delay เล็กน้อยก่อน despawn
}