using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class SpectatorCameraController : MonoBehaviour
{
    public enum SpectatorMode { FollowPlayer, FreeFly }
    
    [Header("Current Mode")]
    public SpectatorMode currentMode = SpectatorMode.FollowPlayer;

    [Header("Follow Settings")]
    public float cameraDistance = 5f;
    public float heightOffset = 1.5f;

    [Header("Free Fly Settings")]
    public float flySpeed = 15f;       
    public float fastFlySpeedMultiplier = 2f; 
    public float lookSensitivity = 2f;    
    public float mapHeightLimit = 50f; 

    private List<Transform> targets = new List<Transform>();
    private int currentIndex = 0;
    
    private float rotationX = 0f;
    private float rotationY = 0f;

    private float refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 0.5f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= REFRESH_INTERVAL)
        {
            refreshTimer = 0f;
            RefreshTargets();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentMode == SpectatorMode.FollowPlayer)
            {
                SwitchToFreeFly();
            }
            else
            {
                SwitchToFollowPlayer();
            }
        }
        if (currentMode == SpectatorMode.FollowPlayer)
        {
            HandleFollowInput();
        }
        else
        {
            HandleFreeFlyInput();
        }
    }
    
    void SwitchToFreeFly()
    {
        currentMode = SpectatorMode.FreeFly;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Vector3 rot = transform.eulerAngles;
        rotationX = rot.y;
        rotationY = rot.x; 
    }

    void SwitchToFollowPlayer()
    {
        currentMode = SpectatorMode.FollowPlayer;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HandleFollowInput()
    {
        if (targets.Count == 0) return;

        bool nextTarget = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || 
                         (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject());
                         
        bool prevTarget = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || 
                         (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject());

        if (nextTarget) 
        {
            currentIndex = (currentIndex + 1) % targets.Count;
        }
        else if (prevTarget)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = targets.Count - 1;
        }
    }

    void HandleFreeFlyInput()
    {
        rotationX += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationY = Mathf.Clamp(rotationY, -80f, 80f);
        transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);
        float finalSpeed = flySpeed;
        if (Input.GetKey(KeyCode.LeftShift)) finalSpeed *= fastFlySpeedMultiplier;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");  
        Vector3 move = (transform.forward * v + transform.right * h).normalized * finalSpeed * Time.deltaTime;
        transform.position += move;
        if (Input.GetKey(KeyCode.E)) transform.position += Vector3.up * finalSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) transform.position -= Vector3.up * finalSpeed * Time.deltaTime;
        float clampedY = Mathf.Clamp(transform.position.y, 2f, mapHeightLimit);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    void LateUpdate()
    {
        if (currentMode == SpectatorMode.FollowPlayer)
        {
            if (targets.Count == 0 || targets[currentIndex] == null) 
            {
                SwitchToFreeFly();
                return;
            }

            Transform target = targets[currentIndex];
            Vector3 lookAtPosition = target.position + new Vector3(0, heightOffset, 0);
            Vector3 targetCamPos = lookAtPosition - (target.forward * cameraDistance);
            
            transform.position = Vector3.Lerp(transform.position, targetCamPos, Time.deltaTime * 10f);
            transform.LookAt(lookAtPosition);
        }
    }

    void RefreshTargets()
    {
        targets.Clear();
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
        foreach (var p in players)
        {
            if (p.currentHealth.Value > 0)
            {
                targets.Add(p.transform);
            }
        }
        
        if (targets.Count > 0 && currentIndex >= targets.Count)
        {
            currentIndex = 0;
        }
    }
}