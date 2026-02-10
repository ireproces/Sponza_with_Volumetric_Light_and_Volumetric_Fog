using UnityEngine;

/// <summary>
/// Script semplice per controllo Player - Drag and Drop su GameObject "Player"
/// Movimento WASD + Mouse Look
/// </summary>
public class SimplePlayerController : MonoBehaviour
{
    [Header("🎮 Controlli Movimento")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float jumpHeight = 2.0f;
    
    [Header("🖱️ Controlli Mouse")]
    [SerializeField] private float mouseSensitivity = 100.0f;
    [SerializeField] private bool lockCursor = true;
    
    [Header("📷 Camera (Opzionale)")]
    [SerializeField] private Camera playerCamera;
    
    // Componenti interni
    private CharacterController controller;
    private Transform cameraTransform;
    
    // Stato
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;

    void Start()
    {
        Setup();
    }

    void Update()
    {
        CheckGrounded();
        HandleMovement();
        HandleMouseLook();
        HandleJump();
        
        // ESC per sbloccare cursore
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }

    /// <summary>
    /// Setup automatico componenti
    /// </summary>
    void Setup()
    {
        // Trova o crea CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1, 0);
        }
        
        // Setup camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                // Crea camera se non esiste
                GameObject cameraObj = new GameObject("Player Camera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                playerCamera = cameraObj.AddComponent<Camera>();
                
                // Aggiungi AudioListener
                if (FindObjectOfType<AudioListener>() == null)
                {
                    cameraObj.AddComponent<AudioListener>();
                }
            }
        }
        
        cameraTransform = playerCamera.transform;
        
        // Setup cursore
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        Debug.Log("✅ SimplePlayerController configurato!");
        Debug.Log("🎮 Controlli: WASD=Movimento, Shift=Corsa, Spazio=Salto, ESC=Cursore");
    }

    /// <summary>
    /// Controlla se il player è a terra
    /// </summary>
    void CheckGrounded()
    {
        isGrounded = controller.isGrounded;
    }

    /// <summary>
    /// Gestisce movimento WASD
    /// </summary>
    void HandleMovement()
    {
        // Input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // Calcola movimento
        Vector3 move = transform.right * x + transform.forward * z;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // Applica movimento
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // Gravità
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Gestisce rotazione con mouse
    /// </summary>
    void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        // Rotazione orizzontale del player
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotazione verticale della camera
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    /// <summary>
    /// Gestisce salto
    /// </summary>
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }
    }

    /// <summary>
    /// Blocca/sblocca cursore
    /// </summary>
    void ToggleCursor()
    {
        lockCursor = !lockCursor;
        
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Metodi di utilità
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        velocity = Vector3.zero;
    }

    public bool IsGrounded() => isGrounded;
    public float GetCurrentSpeed() => controller.velocity.magnitude;
}
