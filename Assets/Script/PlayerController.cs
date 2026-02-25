using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
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
    private NetworkObject currentItem;
    private Camera mainCamera;
    private float yaw = 0f;
    private float pitch = 20f;
    private float verticalVelocity = 0f;
    private CharacterController controller; 
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
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (Input.GetKeyDown(KeyCode.E) && currentItem == null)
        {
            TryPickupItem();
        }
        if (Input.GetButtonDown("Fire1") && currentItem != null)
        {
            ThrowItemServerRpc(mainCamera.transform.forward);
        }
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
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
        if (animator != null)
        {
            animator.SetFloat("Speed", moveInput.magnitude); 
        }
    }
    void LateUpdate()
    {
        if (!IsOwner || mainCamera == null) return;
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 lookAtPosition = transform.position + targetOffset;
        mainCamera.transform.position = lookAtPosition - (camRotation * Vector3.forward * cameraDistance);
        mainCamera.transform.LookAt(lookAtPosition);
    }
    void TryPickupItem()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ThrowableItem item))
            {
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                if (netObj.IsSpawned && netObj.transform.parent == null) 
                {
                    PickupItemServerRpc(netObj.NetworkObjectId);
                    break; 
                }
            }
        }
    }
    [ServerRpc]
    void PickupItemServerRpc(ulong itemNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj))
        {
            itemObj.GetComponent<ThrowableItem>().Grab(transform, handPoint);
            SetCurrentItemClientRpc(itemNetworkId);
        }
    }
    [ClientRpc]
    void SetCurrentItemClientRpc(ulong itemNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj))
        {
            currentItem = itemObj;
        }
    }
    [ServerRpc]
    void ThrowItemServerRpc(Vector3 aimDirection)
    {
        if (currentItem != null)
        {
            currentItem.GetComponent<ThrowableItem>().Throw(aimDirection, throwForce);
            ClearItemClientRpc();
        }
    }
    [ClientRpc]
    void ClearItemClientRpc()
    {
        currentItem = null;
    }
}