using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Pawthrow/ItemData")]
public class ItemData : ScriptableObject
{
    public float damage = 10f; 
    public float knockbackForce = 15f;

    [Header("Stun Effect")]
    public bool isHeavyItem = false;
    public float stunDuration = 2f;
    
    [Header("Destroy Settings")]
    public bool destroyOnHitPlayer = true;  
    public bool destroyOnHitGround = false; 
    public float destroyDelay = 0.1f;       
    
    [Header("─── Consumable Settings ───")]
    public bool isConsumable = false;
    [Tooltip("เพิ่ม HP (ไก่ทอด)")]
    public float healAmount = 0f;
    [Tooltip("เพิ่ม moveSpeed ชั่วคราว (Energy Drink)")]
    public float speedBoostAmount = 0f;
    public float speedBoostDuration = 0f;
    [Tooltip("เพิ่ม throwForce ชั่วคราว (เหล้า/เบียร์)")]
    public float throwBoostAmount = 0f;
    public float throwBoostDuration = 0f;

    [Header("─── Explosion Settings ───")]
    public bool isExplosive = false;
    public float explosionRadius = 5f;
    public float explosionDamage = 25f;
    public float explosionKnockback = 35f;
    public float explosionStunDuration = 1.2f;
    public GameObject explosionVFXPrefab; 

    // --- เพิ่มส่วนนี้เข้าไป ---
    [Header("─── Fire Item Settings ───")]
    public bool isFireItem = false;
    public GameObject fireZonePrefab; // prefab ที่มี FireZone.cs + NetworkObject
}