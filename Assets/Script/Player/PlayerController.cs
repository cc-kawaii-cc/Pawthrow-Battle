using Unity.Netcode;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    public enum CharacterType { DogKnight, NekoCat }

    [Header("Character Skills ")]
    public CharacterType characterType = CharacterType.DogKnight;
    
    [Header("Dog Skill (Shield)")]
    public NetworkVariable<bool> isShieldActive = new NetworkVariable<bool>(false);
    public float shieldCooldown = 5f;
    private float shieldTimer = 0f;
    public GameObject shieldVisual;

    [Header("Cat Skill (Dash)")]
    public float dashForce = 20f;
    public float dashCooldown = 3f;
    private float dashTimer = 0f;

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

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        if (IsOwner)
        {
            mainCamera = Camera.main; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update() 
    {
        if (!IsOwner) return;

        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return; 
        }
        
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (transform.position.y < fallDeathY) 
        {
            FallDeathServerRpc(); return;
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
        
        if (shieldTimer > 0) shieldTimer -= Time.deltaTime;
        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        
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
        
        if (Input.GetKeyDown(KeyCode.E) && currentItem == null) TryPickupItem();

        if (Input.GetButtonDown("Fire1") && currentItem != null)
        {
            isCharging = true; currentCharge = 0f;
        }
        if (Input.GetButton("Fire1") && isCharging) 
        {
            currentCharge += Time.deltaTime; currentCharge = Mathf.Clamp(currentCharge, 0, maxChargeTime);
        }
        if (Input.GetButtonUp("Fire1") && isCharging) 
        {
            isCharging = false;
            float chargeMultiplier = 1f + (currentCharge / maxChargeTime); 
            ThrowItemServerRpc(mainCamera.transform.forward, chargeMultiplier);
        }
        
        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        if (Input.GetButtonDown("Jump") && controller.isGrounded) verticalVelocity = jumpForce;
        verticalVelocity += gravity * Time.deltaTime;

        float x = Input.GetAxis("Horizontal"); 
        float z = Input.GetAxis("Vertical");   
        Vector3 moveInput = new Vector3(x, 0f, z).normalized;
        Vector3 moveDirection = Vector3.zero;

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

        Vector3 finalMovement = moveDirection * moveSpeed;
        finalMovement.y = verticalVelocity; 
        controller.Move(finalMovement * Time.deltaTime);

        if (animator != null) animator.SetFloat("Speed", moveInput.magnitude); 
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
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
    
    [ServerRpc]
    void ActivateShieldServerRpc()
    {
        isShieldActive.Value = true;
        UpdateShieldVisualClientRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakShieldServerRpc()
    {
        isShieldActive.Value = false;
        UpdateShieldVisualClientRpc(false);
    }

    [ClientRpc]
    void UpdateShieldVisualClientRpc(bool active)
    {
        if (shieldVisual != null) shieldVisual.SetActive(active);
    }
    
    void Dash()
    {
        Vector3 dashDir = transform.forward;
        AddImpact(dashDir * dashForce);
    }
    
    [ClientRpc]
    public void ApplyStunClientRpc(float duration) 
    {
        if (!IsOwner) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration) 
    {
        isStunned = true; yield return new WaitForSeconds(duration); isStunned = false; 
    }

    void TryPickupItem() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders) 
        {
            if (hitCollider.TryGetComponent(out ThrowableItem item)) 
            {
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                if (netObj.IsSpawned && netObj.transform.parent == null)
                {
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
    [ServerRpc] void ThrowItemServerRpc(Vector3 aimDirection, float chargeMultiplier) 
    {
        if (currentItem != null) 
        {
            currentItem.GetComponent<ThrowableItem>().Throw(aimDirection, throwForce, chargeMultiplier); 
            ClearItemClientRpc();
        }
    }

    [ClientRpc]
    void ClearItemClientRpc()
    {
        currentItem = null; 
    }

    [ServerRpc]
    void FallDeathServerRpc()
    {
        if (TryGetComponent(out PlayerHealth health)) health.Die(); 
    }
}