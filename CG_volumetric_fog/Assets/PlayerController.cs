using UnityEngine;

/// <summary>
/// Script per il controllo del player con movimento WASD e rotazione camera mouse
/// Supporta sia First Person che Third Person
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private bool canRun = true;
    [SerializeField] private bool canJump = true;
    
    [Header("Controlli Mouse")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float maxLookAngle = 90.0f;
    [SerializeField] private bool invertYAxis = false;
    [SerializeField] private bool lockCursor = true;
    
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool firstPersonMode = true;
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0, 2, -5);
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.1f;
    [SerializeField] private LayerMask groundMask = 1;
    
    [Header("Boundaries (Sponza Limits)")]
    [SerializeField] private bool enableBoundaries = true;
    [SerializeField] private Vector3 minBounds = new Vector3(-12f, -5f, -25f);
    [SerializeField] private Vector3 maxBounds = new Vector3(12f, 15f, 25f);
    [SerializeField] private bool autoDetectBounds = true;
    
    [Header("Audio (Opzionale)")]
    [SerializeField] private AudioSource footstepAudio;
    [SerializeField] private AudioClip[] footstepSounds;
    
    // Componenti
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private Camera playerCamera;
    
    // Stato movimento
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private float currentSpeed;
    private float footstepTimer;
    
    // Rotazione camera
    private float mouseX;
    private float mouseY;
    private float xRotation = 0f;
    
    // Input
    private float horizontal;
    private float vertical;
    private bool jumpInput;
    private bool runInput;

    void Start()
    {
        InitializeComponents();
        SetupCamera();
        SetupCursor();
    }

    void Update()
    {
        HandleInput();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        HandleFootsteps();
        
        // Debug info
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleFirstPersonMode();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }

    /// <summary>
    /// Inizializza i componenti necessari
    /// </summary>
    private void InitializeComponents()
    {
        // Prova a trovare CharacterController prima di Rigidbody
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody == null)
            {
                Debug.LogWarning("PlayerController: Nessun CharacterController o Rigidbody trovato. Aggiungendo CharacterController...");
                characterController = gameObject.AddComponent<CharacterController>();
            }
        }
        
        // Setup camera
        if (cameraTransform == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
            
            if (playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
            }
        }
        else
        {
            playerCamera = cameraTransform.GetComponent<Camera>();
        }
        
        // Setup ground check se non assegnato
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
        // Setup audio
        if (footstepAudio == null)
        {
            footstepAudio = GetComponent<AudioSource>();
        }
        
        // Auto-detect Sponza bounds se abilitato
        if (autoDetectBounds)
        {
            DetectSponzaBounds();
        }
        
        Debug.Log($"PlayerController: Bounds attivi da {minBounds} a {maxBounds}");
    }

    /// <summary>
    /// Configura la camera
    /// </summary>
    private void SetupCamera()
    {
        if (cameraTransform != null)
        {
            if (firstPersonMode)
            {
                cameraTransform.SetParent(transform);
                cameraTransform.localPosition = new Vector3(0, 1.8f, 0);
                cameraTransform.localRotation = Quaternion.identity;
            }
            else
            {
                cameraTransform.SetParent(null);
            }
        }
    }

    /// <summary>
    /// Configura il cursore
    /// </summary>
    private void SetupCursor()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Gestisce l'input del player
    /// </summary>
    private void HandleInput()
    {
        // Input movimento
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        
        // Input corsa
        runInput = canRun && Input.GetKey(KeyCode.LeftShift);
        
        // Input salto
        jumpInput = canJump && Input.GetButtonDown("Jump");
        
        // Mouse input
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertYAxis)
            mouseY = -mouseY;
    }

    /// <summary>
    /// Gestisce la rotazione della camera con il mouse
    /// </summary>
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        
        // Rotazione orizzontale del player
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotazione verticale della camera
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        
        if (firstPersonMode)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else
        {
            // Third person camera logic
            Vector3 targetPosition = transform.position + transform.TransformDirection(thirdPersonOffset);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, Time.deltaTime * 5f);
            cameraTransform.LookAt(transform.position + Vector3.up * 1.5f);
        }
    }

    /// <summary>
    /// Gestisce il movimento del player
    /// </summary>
    private void HandleMovement()
    {
        // Controlla se è a terra
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        // Determina velocità
        isRunning = runInput && (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
        currentSpeed = isRunning ? runSpeed : moveSpeed;
        
        // Calcola direzione movimento
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction = Vector3.ClampMagnitude(direction, 1.0f);
        
        if (characterController != null)
        {
            // Movimento con CharacterController
            Vector3 move = direction * currentSpeed;
            Vector3 newPosition = transform.position + move * Time.deltaTime;
            
            // Controlla boundaries prima di muoversi
            if (enableBoundaries)
            {
                newPosition = ClampToBounds(newPosition);
                move = (newPosition - transform.position) / Time.deltaTime;
            }
            
            characterController.Move(move * Time.deltaTime);
            
            // Applica gravità
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Piccola forza per mantenere a terra
            }
            
            velocity.y += Physics.gravity.y * Time.deltaTime;
            
            // Controlla boundaries anche per la gravità
            Vector3 gravityMove = velocity * Time.deltaTime;
            Vector3 finalPosition = transform.position + gravityMove;
            if (enableBoundaries)
            {
                finalPosition = ClampToBounds(finalPosition);
                gravityMove = finalPosition - transform.position;
            }
            
            characterController.Move(gravityMove);
        }
        else if (playerRigidbody != null)
        {
            // Movimento con Rigidbody
            Vector3 move = direction * currentSpeed;
            Vector3 newPosition = transform.position + move * Time.deltaTime;
            
            // Controlla boundaries
            if (enableBoundaries)
            {
                newPosition = ClampToBounds(newPosition);
            }
            
            playerRigidbody.MovePosition(newPosition);
        }
    }

    /// <summary>
    /// Gestisce il salto
    /// </summary>
    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            if (characterController != null)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            }
            else if (playerRigidbody != null)
            {
                playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            
            Debug.Log("Player jumped!");
        }
    }

    /// <summary>
    /// Gestisce i suoni dei passi
    /// </summary>
    private void HandleFootsteps()
    {
        if (footstepAudio == null || footstepSounds == null || footstepSounds.Length == 0) return;
        
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f) && isGrounded;
        
        if (isMoving)
        {
            float stepInterval = isRunning ? 0.3f : 0.5f;
            footstepTimer += Time.deltaTime;
            
            if (footstepTimer >= stepInterval)
            {
                PlayRandomFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    /// <summary>
    /// Riproduce un suono casuale di passo
    /// </summary>
    private void PlayRandomFootstep()
    {
        if (footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            footstepAudio.PlayOneShot(footstepSounds[randomIndex]);
        }
    }

    /// <summary>
    /// Cambia tra modalità First Person e Third Person
    /// </summary>
    public void ToggleFirstPersonMode()
    {
        firstPersonMode = !firstPersonMode;
        SetupCamera();
        Debug.Log($"Modalità camera: {(firstPersonMode ? "First Person" : "Third Person")}");
    }

    /// <summary>
    /// Blocca/sblocca il cursore
    /// </summary>
    public void ToggleCursor()
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
    /// Teletrasporta il player ad una posizione
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        else
        {
            transform.position = position;
        }
        
        velocity = Vector3.zero;
    }

    /// <summary>
    /// Metodi pubblici per controlli esterni
    /// </summary>
    public bool IsGrounded => isGrounded;
    public bool IsRunning => isRunning;
    public float CurrentSpeed => currentSpeed;
    public Vector3 Velocity => velocity;

    /// <summary>
    /// Rileva automaticamente i bounds della scena Sponza
    /// </summary>
    private void DetectSponzaBounds()
    {
        // Cerca oggetti Sponza nella scena
        GameObject sponzaObj = GameObject.Find("Sponza");
        if (sponzaObj == null)
        {
            // Cerca per componenti Renderer nella scena
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length > 0)
            {
                Bounds sceneBounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    // Esclude il player stesso e oggetti UI
                    if (renderer.gameObject != gameObject && 
                        !renderer.gameObject.name.Contains("UI") &&
                        !renderer.gameObject.name.Contains("Canvas"))
                    {
                        sceneBounds.Encapsulate(renderer.bounds);
                    }
                }
                
                // Espandi leggermente per sicurezza e aggiungi margini ragionevoli
                minBounds = sceneBounds.min - Vector3.one * 2f;
                maxBounds = sceneBounds.max + Vector3.one * 2f;
                
                // Limiti Y più restrittivi per evitare di cadere sotto la scena
                minBounds.y = Mathf.Max(minBounds.y, -2f);
                maxBounds.y = Mathf.Min(maxBounds.y, 20f);
                
                Debug.Log($"PlayerController: Bounds auto-rilevati - Min: {minBounds}, Max: {maxBounds}");
            }
        }
        else
        {
            // Usa i bounds del GameObject Sponza
            Renderer sponzaRenderer = sponzaObj.GetComponent<Renderer>();
            if (sponzaRenderer != null)
            {
                Bounds sponzaBounds = sponzaRenderer.bounds;
                minBounds = sponzaBounds.min - Vector3.one * 1f;
                maxBounds = sponzaBounds.max + Vector3.one * 1f;
                minBounds.y = Mathf.Max(minBounds.y, -1f);
                
                Debug.Log($"PlayerController: Bounds Sponza rilevati - Min: {minBounds}, Max: {maxBounds}");
            }
        }
    }

    /// <summary>
    /// Limita la posizione ai bounds definiti
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(position.y, minBounds.y, maxBounds.y),
            Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
        );
    }

    /// <summary>
    /// Controlla se il player è vicino ai bounds
    /// </summary>
    private bool IsNearBoundary(float threshold = 2f)
    {
        Vector3 pos = transform.position;
        return (pos.x <= minBounds.x + threshold || pos.x >= maxBounds.x - threshold ||
                pos.z <= minBounds.z + threshold || pos.z >= maxBounds.z - threshold ||
                pos.y <= minBounds.y + threshold);
    }

    /// <summary>
    /// Metodi pubblici per controllare e modificare i bounds
    /// </summary>
    [ContextMenu("Rileva Bounds Automaticamente")]
    public void ReScanBounds()
    {
        DetectSponzaBounds();
    }

    public void SetCustomBounds(Vector3 min, Vector3 max)
    {
        minBounds = min;
        maxBounds = max;
        enableBoundaries = true;
    }

    void OnDrawGizmosSelected()
    {
        // Visualizza ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
        
        // Visualizza boundaries
        if (enableBoundaries)
        {
            Gizmos.color = IsNearBoundary() ? Color.yellow : Color.cyan;
            Vector3 center = (minBounds + maxBounds) * 0.5f;
            Vector3 size = maxBounds - minBounds;
            Gizmos.DrawWireCube(center, size);
            
            // Visualizza warning se vicino ai bounds
            if (Application.isPlaying && IsNearBoundary())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
        
        // Visualizza direzione movimento
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 direction = transform.right * horizontal + transform.forward * vertical;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Gestisce collisioni con CharacterController se necessario
        if (hit.gameObject.CompareTag("Movable"))
        {
            Rigidbody hitRigidbody = hit.collider.attachedRigidbody;
            if (hitRigidbody != null)
            {
                Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
                hitRigidbody.AddForce(pushDirection * currentSpeed, ForceMode.Impulse);
            }
        }
    }
}
