using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    public enum CharacterType { DogKnight, NekoCat, Chicken, Bat, Fox, Bear, Bull, Octopus }

    [Header("Character Skills")]
    public CharacterType characterType;
    
    [Header("Dog Skill (Shield)")]
    public NetworkVariable<bool> isShieldActive = new NetworkVariable<bool>(false);
    public float shieldCooldown = 5f;
    private float shieldTimer = 0f;
    public GameObject shieldVisual;

    [Header("Cat Skill (Dash)")]
    public float dashForce = 20f;
    public float dashCooldown = 3f;
    private float dashTimer = 0f;
    
    [Header("=== CHICKEN SKILL: Double Jump ===")]
    public int maxJumps = 2;
    private int jumpsRemaining = 0;
    public ParticleSystem jumpParticle; 

    [Header("=== BAT SKILL: Echo ===")]
    public float echoRadius = 20f;
    public float echoDuration = 3f;
    public float echoCooldown = 8f;
    private float echoTimer = 0f;

    [Header("=== FOX SKILL: Decoy ===")]
    public GameObject decoyPrefab;
    public float decoyDuration = 4f;
    public float decoyCooldown = 10f;
    private float decoyTimer = 0f;

    [Header("=== BEAR SKILL: Rage ===")]
    public float rageThrowMultiplier = 2f;
    public float rageDuration = 5f;
    public float rageCooldown = 12f;
    private float rageTimer = 0f;
    public bool isRaging = false;
    public GameObject rageVFX; 

    [Header("=== BULL SKILL: Charge ===")]
    public float bullChargeSpeed = 22f;
    public float bullChargeDistance = 12f;
    public float bullChargeDamage = 30f;
    public float bullChargeKnockback = 25f;
    public float bullChargeCooldown = 8f;
    private float bullChargeTimer = 0f;
    private bool isBullCharging = false;
    private float bullChargeDistanceLeft = 0f;
    private HashSet<ulong> hitDuringCharge = new HashSet<ulong>();
    public ParticleSystem bullChargeVFX; 

    [Header("=== OCTOPUS SKILL: Ink ===")]
    public float inkRadius = 8f;
    public float inkBlindDuration = 3f;
    public float inkCooldown = 10f;
    private float inkTimer = 0f;
    public GameObject inkVFXPrefab; 

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 700f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public Animator animator;
    
    [Header("Camera Settings")]
    public float cameraDistance = 5f;
    public float mouseSensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 60f;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Pick & Throw Settings")]
    public Transform handPoint; 
    public float pickupRange = 2f;
    public float throwForce = 15f;

    [Header("Charge & Stun Settings")]
    public float maxChargeTime = 1.5f; 
    private float currentCharge = 0f;
    private bool isCharging = false;
    private bool isStunned = false;    
    
    private NetworkObject currentItem;
    private Camera mainCamera;
    private float yaw = 0f;
    private float pitch = 20f;
    private float verticalVelocity = 0f;
    private CharacterController controller; 

    public float fallDeathY = -15f;
    private Vector3 impact = Vector3.zero;
    private bool isDead = false;
    
    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        if (IsOwner)
        {
            mainCamera = Camera.main; 
            if (SceneManager.GetActiveScene().name == "CITY")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            StartCoroutine(SpawnRandomlyAndDropCoroutine());
        }
    }

    private IEnumerator SpawnRandomlyAndDropCoroutine()
    {
        if (controller != null) controller.enabled = false;

        Vector3 safeSkyPosition = GetSafeRandomPosition();
        transform.position = safeSkyPosition;

        yield return null; 

        if (controller != null) controller.enabled = true;
    }
    
    private Vector3 GetSafeRandomPosition()
    {
        PlayerSpawnZone spawnZone = FindObjectOfType<PlayerSpawnZone>();
        Vector3 center = Vector3.zero;
        Vector2 areaSize = new Vector2(20f, 20f);

        if (spawnZone != null)
        {
            center = spawnZone.transform.position;
            areaSize = spawnZone.spawnAreaSize;
        }
        else
        {
            Debug.LogWarning("เตือน: ด่านนี้ยังไม่มี PlayerSpawnZone! ระบบจะสุ่มเกิดตรงกลาง (0,0,0) แทน");
        }

        int maxAttempts = 30; 
        float playerRadius = 1f; 

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(-areaSize.x / 2, areaSize.x / 2);
            float randomZ = Random.Range(-areaSize.y / 2, areaSize.y / 2);
            
            Vector3 skyPos = center + new Vector3(randomX, 15f, randomZ);

            if (Physics.Raycast(skyPos, Vector3.down, out RaycastHit hit, 50f))
            {
                Vector3 checkPos = hit.point + new Vector3(0, playerRadius + 0.1f, 0);
                Collider[] hitColliders = Physics.OverlapSphere(checkPos, playerRadius);
                bool isStuck = false;

                foreach(var col in hitColliders)
                {
                    if (!col.CompareTag("Ground") && !col.name.Contains("Terrain") && !col.name.Contains("Plane"))
                    {
                        isStuck = true;
                        break; 
                    }
                }

                if (!isStuck) return skyPos; 
            }
        }

        return center + new Vector3(0, 15f, 0);
    }

    void Update() 
    {
        if (!IsOwner) return;
        if (controller != null && !controller.enabled) return; 

        if (SceneManager.GetActiveScene().name == "CITY")
        {
            if (Input.GetMouseButtonDown(0)) 
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return; 
        }
        
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (transform.position.y < fallDeathY && !isDead) 
        {
            isDead = true; 
            FallDeathServerRpc(); 
            return;
        }

        if (impact.magnitude > 0.2f) 
        {
            controller.Move(impact * Time.deltaTime);
            impact = Vector3.Lerp(impact, Vector3.zero, 5f * Time.deltaTime);
        }

        if (isStunned) 
        {
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            if (animator != null) animator.SetFloat("Speed", 0);
            return; 
        }
        
        // Cooldown Timers
        if (shieldTimer > 0) shieldTimer -= Time.deltaTime;
        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        if (echoTimer > 0) echoTimer -= Time.deltaTime;
        if (decoyTimer > 0) decoyTimer -= Time.deltaTime;
        if (rageTimer > 0) rageTimer -= Time.deltaTime;
        if (bullChargeTimer > 0) bullChargeTimer -= Time.deltaTime;
        if (inkTimer > 0) inkTimer -= Time.deltaTime;
        
        // Skills Dispatch
        if (characterType == CharacterType.DogKnight)
        {
            if (Input.GetButtonDown("Fire2") && shieldTimer <= 0 && !isShieldActive.Value)
            {
                ActivateShieldServerRpc();
                shieldTimer = shieldCooldown;
            }
        }
        else if (characterType == CharacterType.NekoCat)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0)
            {
                Dash();
                dashTimer = dashCooldown;
            }
        }

        switch (characterType)
        {
            case CharacterType.Chicken: HandleChickenSkill(); break;
            case CharacterType.Bat: HandleBatSkill(); break;
            case CharacterType.Fox: HandleFoxSkill(); break;
            case CharacterType.Bear: HandleBearSkill(); break;
            case CharacterType.Bull: HandleBullSkill(); break;
            case CharacterType.Octopus: HandleOctopusSkill(); break;
        }
        
        // Pick & Throw Logic
        if (Input.GetKeyDown(KeyCode.E) && currentItem == null) TryPickupItem();

        if (Input.GetButtonDown("Fire1") && currentItem != null)
        {
            isCharging = true; currentCharge = 0f;
        }
        if (Input.GetButton("Fire1") && isCharging) 
        {
            currentCharge += Time.deltaTime; 
            currentCharge = Mathf.Clamp(currentCharge, 0, maxChargeTime);
        }
        if (Input.GetButtonUp("Fire1") && isCharging) 
        {
            isCharging = false;
            float chargeMultiplier = 1f + (currentCharge / maxChargeTime); 

            ThrowItemServerRpc(mainCamera.transform.forward, chargeMultiplier);
        }
        
        // Jump & Gravity Logic
        if (controller.isGrounded && verticalVelocity < 0) 
        {
            verticalVelocity = -2f;
        }
        
        if (characterType != CharacterType.Chicken)
        {
            if (Input.GetButtonDown("Jump") && controller.isGrounded && !isStunned) 
            {
                verticalVelocity = jumpForce;
            }
        }
        verticalVelocity += gravity * Time.deltaTime;

        // BULL CHARGE MOVEMENT
        if (isBullCharging && characterType == CharacterType.Bull)
        {
            float step = bullChargeSpeed * Time.deltaTime;
            controller.Move(transform.forward * step);
            bullChargeDistanceLeft -= step;
            
            RequestBullHitCheckServerRpc(transform.position);
            
            if (bullChargeDistanceLeft <= 0f || (controller.collisionFlags & CollisionFlags.Sides) != 0)
            {
                isBullCharging = false;
            }
        }

        // NORMAL MOVEMENT
        float x = Input.GetAxis("Horizontal"); 
        float z = Input.GetAxis("Vertical");   
        Vector3 moveInput = new Vector3(x, 0f, z).normalized;
        Vector3 moveDirection = Vector3.zero;

        if (!isBullCharging && !isStunned)
        {
            if (moveInput.magnitude >= 0.1f && mainCamera != null) 
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;
                cameraForward.y = 0; cameraRight.y = 0;
                cameraForward.Normalize(); cameraRight.Normalize();

                moveDirection = (cameraForward * moveInput.z + cameraRight * moveInput.x).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        Vector3 finalMovement = moveDirection * moveSpeed;
        finalMovement.y = verticalVelocity; 
        controller.Move(finalMovement * Time.deltaTime);

        if (animator != null && !isStunned && !isBullCharging) 
        {
            animator.SetFloat("Speed", moveInput.magnitude); 
        }
    }

    void LateUpdate()
    {
        if (!IsOwner || !IsSpawned) return;
        if (mainCamera == null) return;

        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 lookAtPosition = transform.position + targetOffset;
        mainCamera.transform.position = lookAtPosition - (camRotation * Vector3.forward * cameraDistance);
        mainCamera.transform.LookAt(lookAtPosition);
    }

    public void AddImpact(Vector3 force) 
    {
        impact += force;
        if (force.y > 0) verticalVelocity = force.y; 
    }

    [ClientRpc] 
    public void AddImpactClientRpc(Vector3 force) 
    { 
        if (IsOwner) AddImpact(force); 
    }

    // ==========================================
    // 🐣 CHICKEN SKILL
    // ==========================================
    void HandleChickenSkill() 
    { 
        if (controller.isGrounded) jumpsRemaining = maxJumps;

        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0 && !isStunned)
        {
            verticalVelocity = jumpForce;
            jumpsRemaining--;
            TriggerJumpVFXServerRpc(); 
        
            if (animator != null) animator.SetTrigger("DoubleJump"); 
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void TriggerJumpVFXServerRpc() { TriggerJumpVFXClientRpc(); }

    [ClientRpc]
    void TriggerJumpVFXClientRpc()
    {
        if (jumpParticle != null) jumpParticle.Play();
        else Debug.Log("JUMP VFX");
    }

    // ==========================================
    // 🦇 BAT SKILL
    // ==========================================
    void HandleBatSkill()
    {
        if (Input.GetKeyDown(KeyCode.E) && echoTimer <= 0 && !isStunned)
        {
            echoTimer = echoCooldown;
            UseEchoServerRpc();
        }
    }

    [ServerRpc]
    void UseEchoServerRpc()
    {
        var allPlayers = FindObjectsOfType<PlayerController>();
        List<Vector3> positions = new List<Vector3>();
        List<ulong> ids = new List<ulong>();

        foreach (var p in allPlayers)
        {
            if (p.OwnerClientId == OwnerClientId) continue;
            if (Vector3.Distance(transform.position, p.transform.position) <= echoRadius)
            {
                positions.Add(p.transform.position);
                ids.Add(p.OwnerClientId);
            }
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
        ShowEchoResultClientRpc(positions.ToArray(), ids.ToArray(), echoDuration, clientRpcParams);
    }

    [ClientRpc]
    void ShowEchoResultClientRpc(Vector3[] positions, ulong[] ids, float duration, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        if (EchoMarkerUI.Instance != null) EchoMarkerUI.Instance.ShowMarkers(positions, ids, duration);
    }

    // ==========================================
    // 🦊 FOX SKILL
    // ==========================================
    void HandleFoxSkill()
    {
        if (decoyTimer > 0) return;
        if (Input.GetKeyDown(KeyCode.E) && decoyPrefab != null && !isStunned)
        {
            decoyTimer = decoyCooldown;
            SpawnDecoyServerRpc(transform.position, transform.rotation);
        }
    }

    [ServerRpc]
    void SpawnDecoyServerRpc(Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(decoyPrefab, pos, rot);
        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn();
    
        DecoyController decoy = go.GetComponent<DecoyController>();
        if (decoy != null) decoy.lifetime.Value = decoyDuration;
    }

    // ==========================================
    // 🐻 BEAR SKILL
    // ==========================================
    void HandleBearSkill() 
    { 
        if (rageTimer > 0) return;
        if (Input.GetKeyDown(KeyCode.E) && !isStunned)
        {
            rageTimer = rageCooldown;
            ActivateRageServerRpc();
        }
    }

    [ServerRpc] 
    void ActivateRageServerRpc()
    {
        SetRageClientRpc(true);
        StartCoroutine(RageEndRoutine());
    }

    private IEnumerator RageEndRoutine()
    {
        yield return new WaitForSeconds(rageDuration);
        SetRageClientRpc(false);
    }

    [ClientRpc] 
    void SetRageClientRpc(bool active)
    {
        isRaging = active;
        if (rageVFX != null) rageVFX.SetActive(active);
    }

    // ==========================================
    // 🐂 BULL SKILL
    // ==========================================
    void HandleBullSkill() 
    { 
        if (bullChargeTimer > 0) return;
        if (Input.GetKeyDown(KeyCode.E) && !isBullCharging && !isStunned)
        {
            bullChargeTimer = bullChargeCooldown;
            isBullCharging = true;
            bullChargeDistanceLeft = bullChargeDistance;
            hitDuringCharge.Clear(); 
            StartChargeVFXServerRpc();
        }
    }

    [ServerRpc]
    void RequestBullHitCheckServerRpc(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 1.3f);
        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out PlayerHealth health)) continue;
            if (health.OwnerClientId == OwnerClientId) continue; 
            if (hitDuringCharge.Contains(health.OwnerClientId)) continue; 

            hitDuringCharge.Add(health.OwnerClientId);
            Vector3 dir = (col.transform.position - transform.position).normalized;
            dir.y = 0.4f; 
            dir = dir.normalized;
            health.TakeDamage(bullChargeDamage, bullChargeKnockback, dir);
        }
    }

    [ServerRpc] void StartChargeVFXServerRpc() { StartChargeVFXClientRpc(); }
    [ClientRpc] void StartChargeVFXClientRpc() { if (bullChargeVFX != null) bullChargeVFX.Play(); }

    // ==========================================
    // 🐙 OCTOPUS SKILL
    // ==========================================
    void HandleOctopusSkill()
    {
        if (inkTimer > 0) return;
        if (Input.GetKeyDown(KeyCode.E) && !isStunned)
        {
            inkTimer = inkCooldown;
            UseInkServerRpc(transform.position);
        }
    }

    [ServerRpc]
    void UseInkServerRpc(Vector3 center)
    {
        var allPlayers = FindObjectsOfType<PlayerController>();
        List<ulong> targets = new List<ulong>();
        
        foreach (var p in allPlayers)
        {
            if (p.OwnerClientId == OwnerClientId) continue; 
            if (Vector3.Distance(center, p.transform.position) <= inkRadius) targets.Add(p.OwnerClientId);
        }
        
        if (targets.Count > 0)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = targets.ToArray() } };
            ApplyInkClientRpc(inkBlindDuration, rpcParams);
        }
        SpawnInkVFXClientRpc(center);
    }

    [ClientRpc]
    void ApplyInkClientRpc(float duration, ClientRpcParams _ = default)
    {
        if (InkBlindEffect.Instance != null) InkBlindEffect.Instance.ApplyBlind(duration);
    }

    [ClientRpc]
    void SpawnInkVFXClientRpc(Vector3 pos)
    {
        if (inkVFXPrefab != null) 
        {
            GameObject vfx = Instantiate(inkVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 5f); 
        }
    } 

    // ==========================================
    // OTHER SKILLS & MECHANICS
    // ==========================================

    [ServerRpc]
    void ActivateShieldServerRpc()
    {
        isShieldActive.Value = true; UpdateShieldVisualClientRpc(true); 
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakShieldServerRpc()
    {
        isShieldActive.Value = false; UpdateShieldVisualClientRpc(false);
    }

    [ClientRpc]
    void UpdateShieldVisualClientRpc(bool active)
    {
        if (shieldVisual != null) shieldVisual.SetActive(active); 
    }

    void Dash()
    {
        Vector3 dashDir = transform.forward; AddImpact(dashDir * dashForce); 
    }

    [ClientRpc]
    public void ApplyStunClientRpc(float duration)
    {
        if (!IsOwner) return; StartCoroutine(StunRoutine(duration)); 
    }

    private IEnumerator StunRoutine(float duration) 
    {
        isStunned = true; 
        isCharging = false; currentCharge = 0f;
        yield return new WaitForSeconds(duration); 
        isStunned = false; 
    }

    void TryPickupItem() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.TryGetComponent(out ThrowableItem item)) {
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                if (netObj.IsSpawned && netObj.transform.parent == null) {
                    PickupItemServerRpc(netObj.NetworkObjectId); break;
                }
            }
        }
    }

    [ServerRpc] void PickupItemServerRpc(ulong itemNetworkId) 
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj)) 
        {
            itemObj.GetComponent<ThrowableItem>().Grab(transform, handPoint); 
            SetCurrentItemClientRpc(itemNetworkId);
        }
    }
    
    [ClientRpc] void SetCurrentItemClientRpc(ulong itemNetworkId) 
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj)) currentItem = itemObj;
    }
    
    [ServerRpc] 
    void ThrowItemServerRpc(Vector3 aimDirection, float chargeMultiplier) 
    {
        if (currentItem != null) 
        {
            ThrowableItem itemComponent = currentItem.GetComponent<ThrowableItem>();
            if (GameManager.Instance != null && itemComponent != null && itemComponent.itemData != null)
            {
                float totalDmg = itemComponent.itemData.damage * chargeMultiplier * (isRaging ? rageThrowMultiplier : 1f);
                GameManager.Instance.RecordMatchStat(itemComponent.itemData.name, totalDmg);
            }

            float finalForce = isRaging ? throwForce * rageThrowMultiplier : throwForce;
            itemComponent.Throw(aimDirection, finalForce, chargeMultiplier); 
            ClearItemClientRpc();
        }
    }

    [ClientRpc] void ClearItemClientRpc() { currentItem = null; }
    [ServerRpc] void FallDeathServerRpc() { if (TryGetComponent(out PlayerHealth health)) health.Die(); }

    private void RecordItemUsageStats(string itemName, float baseDamage, float chargeMultiplier)
    {
        if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.disableAnalyticsForTesting) return;

        try
        {
            float totalDamage = baseDamage * chargeMultiplier;
            string throwerCharacter = characterType.ToString(); 

            CustomEvent itemEvent = new CustomEvent("item_usage_stats")
            {
                { "item_name", itemName },
                { "base_damage", baseDamage },
                { "charge_multiplier", chargeMultiplier },
                { "total_damage_potential", totalDamage },
                { "thrower_character", throwerCharacter }
            };
            AnalyticsService.Instance.RecordEvent(itemEvent);
            AnalyticsService.Instance.Flush(); 
        }
        catch (System.Exception e) { Debug.LogWarning("Analytics Error: " + e.Message); }
    }
}