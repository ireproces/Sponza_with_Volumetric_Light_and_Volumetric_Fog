using UnityEngine;

/// <summary>
/// Script semplice per movimento base WASD
/// Da aggiungere al GameObject Player
/// </summary>
public class BasicMovement : MonoBehaviour
{
        [Header("Impostazioni Movimento")]
    [SerializeField] private float moveSpeed = 0.2f;  // Movimento molto lento
    [SerializeField] private float runSpeed = 0.4f;   // Corsa lenta e controllata
    [SerializeField] private float jumpHeight = 2.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private bool enableGravity = false; // Disabilita gravità di default
    [SerializeField] private bool snapToGround = false;  // Disabilita snap automatico
    
        [Header("Controlli Mouse")]
    [SerializeField] private float mouseSensitivity = 30f;  // MOLTO più lento e preciso
    [SerializeField] private Transform cameraTransform;
    
        [Header("Boundaries (Anti-Caduta)")]
    [SerializeField] private bool enableBoundaries = false; // Disabilitato di default
    [SerializeField] private float minY = -2f; // Altezza minima corretta per Sponza
    [SerializeField] private Vector3 spawnPoint = new Vector3(0, 0.5f, 0); // Y corretta per Sponza
    [SerializeField] private Vector3 minBounds = new Vector3(-15f, -2f, -30f);
    [SerializeField] private Vector3 maxBounds = new Vector3(15f, 20f, 30f);
    [SerializeField] private float respawnCooldown = 2f; // Cooldown più lungo
    
    // Componenti
    private CharacterController controller;
    
    // Variabili movimento
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float lastRespawnTime = 0f;

    void Start()
    {
        // Setup automatico componenti
        SetupComponents();
        
        // Se la camera non è assegnata, prova a trovarla
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }
        // Fissa il player al livello pavimento
        float floorY = -3.0f;
        Vector3 fixedPosition = new Vector3(transform.position.x, floorY, transform.position.z);
        
        controller.enabled = false;
        transform.position = fixedPosition;
        controller.enabled = true;
        
        spawnPoint = fixedPosition;
        minY = floorY - 1f; // Soglia di sicurezza
        
        // Reset velocità
        velocity = Vector3.zero;
        
        // Blocca cursore
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    void Update()
    {
        // Controlla se è a terra
        CheckGrounded();
        
        // Gestisci movimento
        HandleMovement();
        
        // Gestisci controllo mouse camera (mantiene altezza manuale)
        if (cameraTransform != null)
        {
            HandleMouseLook();
        }
        
        // Gestisci salto
        HandleJump();
        
        // FORZA posizione Y corretta ad ogni frame
        ForceFloorPosition();
        
        // Gestisci gravità solo se abilitata
        if (enableGravity)
        {
            HandleGravity();
        }
        else
        {
            velocity.y = 0f; // Nessun movimento verticale
        }
        
        // Controlla boundaries e previeni cadute
        if (enableBoundaries)
        {
            CheckBoundaries();
        }
        
        // ESC per sbloccare cursore
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
        
        // B per disabilitare/abilitare boundaries in caso di problemi
        if (Input.GetKeyDown(KeyCode.B))
        {
            enableBoundaries = !enableBoundaries;
            Debug.Log($"Boundaries {(enableBoundaries ? "ABILITATE" : "DISABILITATE")}");
            if (!enableBoundaries)
            {
                Debug.Log("Movimento completamente libero - nessun limite attivo");
            }
        }
        
        // R per reset manuale posizione
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Reset manuale posizione");
            RespawnPlayer();
        }
        
        // G per toggle gravità
        if (Input.GetKeyDown(KeyCode.G))
        {
            enableGravity = !enableGravity;
            Debug.Log($"Gravita {(enableGravity ? "ABILITATA" : "DISABILITATA")}");
            if (!enableGravity) velocity.y = 0f;
        }
        
        // T per toggle snap to ground
        if (Input.GetKeyDown(KeyCode.T))
        {
            snapToGround = !snapToGround;
            Debug.Log($"Snap to Ground {(snapToGround ? "ABILITATO" : "DISABILITATO")}");
        }
        
        // Q e E per regolare altezza Y manualmente
        if (Input.GetKey(KeyCode.Q))
        {
            Vector3 pos = transform.position;
            pos.y -= 0.5f * Time.deltaTime; // Abbassa lentamente
            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;
            Debug.Log($"Y abbassata a: {pos.y:F3}");
        }
        
        if (Input.GetKey(KeyCode.E))
        {
            Vector3 pos = transform.position;
            pos.y += 0.5f * Time.deltaTime; // Alza lentamente
            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;
            Debug.Log($"Y alzata a: {pos.y:F3}");
        }
        
        // Controlli camera rimossi - gestione manuale
        
        // + e - per regolare velocità al volo (incrementi più piccoli)
        if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
        {
            moveSpeed += 0.2f;
            runSpeed += 0.3f;
            Debug.Log($"Velocita aumentata: Camminata={moveSpeed:F1}, Corsa={runSpeed:F1}");
        }
        
        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed - 0.2f);
            runSpeed = Mathf.Max(0.2f, runSpeed - 0.3f);
            Debug.Log($"Velocita diminuita: Camminata={moveSpeed:F1}, Corsa={runSpeed:F1}");
        }
    }

    /// <summary>
    /// Setup automatico dei componenti
    /// </summary>
    void SetupComponents()
    {
        // Aggiungi CharacterController se non presente
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1, 0);
        }
        
        // Nessun setup automatico della camera - controllo manuale
    }

    /// <summary>
    /// Controlla se il player è a terra
    /// </summary>
    void CheckGrounded()
    {
        // Usa Physics raycast per detection più precisa del pavimento
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 2.1f);
        
        // Se non rileva pavimento, forza isGrounded = true per evitare cadute
        if (!isGrounded && transform.position.y > -2f)
        {
            isGrounded = true; // Forza grounded per evitare cadute indesiderate
        }
    }

    /// <summary>
    /// Gestisce movimento WASD - FISSO al pavimento
    /// </summary>
    void HandleMovement()
    {
        // Input movimento - MAPPATURA CORRETTA
        float horizontal = Input.GetAxis("Horizontal"); // A/D 
        float vertical = Input.GetAxis("Vertical");     // W/S
        
        // Determina velocità (Shift per correre)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        
        // CORREZIONE ASSI: W/S ok, A/D invertiti - invertiamo solo horizontal
        // W/S = avanti/dietro (OK), A/D = sinistra/destra (invertiti, quindi -horizontal)
        Vector3 move = transform.forward * (-horizontal) + transform.right * vertical;
        move.y = 0f; // FORZA Y = 0 per evitare movimento verticale
        
        // Applica movimento
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // FORZA la posizione Y al livello pavimento dopo ogni movimento
        Vector3 currentPos = transform.position;
        if (Mathf.Abs(currentPos.y - (-3.0f)) > 0.01f)
        {
            controller.enabled = false;
            transform.position = new Vector3(currentPos.x, -3.0f, currentPos.z);
            controller.enabled = true;
        }
    }

    // Controllo mouse rimosso - gestione manuale della camera

    /// <summary>
    /// Gestisce salto
    /// </summary>
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    /// <summary>
    /// Gestisce gravità - STABILE per Sponza
    /// </summary>
    void HandleGravity()
    {
        if (!enableGravity)
        {
            velocity.y = 0f; // Nessuna gravità
            return;
        }
        
        // Se è a terra, mantieni posizione Y stabile
        if (isGrounded)
        {
            velocity.y = 0f; // Ferma completamente la caduta
        }
        else
        {
            // Applica gravità solo se non sta facendo snap al pavimento
            if (!snapToGround)
            {
                velocity.y += gravity * Time.deltaTime;
                // Limita la velocità di caduta
                velocity.y = Mathf.Max(velocity.y, -10f);
            }
        }
        
        // Applica movimento verticale solo se necessario
        if (Mathf.Abs(velocity.y) > 0.01f)
        {
            controller.Move(velocity * Time.deltaTime);
        }
    }

    /// <summary>
    /// FORZA il player al livello pavimento esatto (Y = -3.0)
    /// </summary>
    void ForceFloorPosition()
    {
        float floorY = -3.0f; // ANCORA più basso
        Vector3 currentPos = transform.position;
        
        // Se Y si è spostato anche minimamente, correggilo
        if (Mathf.Abs(currentPos.y - floorY) > 0.001f)
        {
            controller.enabled = false;
            transform.position = new Vector3(currentPos.x, floorY, currentPos.z);
            controller.enabled = true;
            velocity.y = 0f; // Reset velocità verticale
        }
    }

    /// <summary>
    /// Mantiene il player al livello del pavimento
    /// </summary>
    void SnapToGround()
    {
        // Raycast verso il basso per trovare il pavimento
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 5f))
        {
            // Se trova il pavimento, mantieni il player a livello corretto
            float targetY = hit.point.y;
            float currentY = transform.position.y;
            
            // Solo se la differenza è significativa (evita jitter)
            if (Mathf.Abs(currentY - targetY) > 0.1f)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = targetY;
                
                controller.enabled = false;
                transform.position = newPosition;
                controller.enabled = true;
                
                velocity.y = 0f; // Reset velocità verticale
            }
        }
        else
        {
            // Se non trova pavimento sotto, mantieni Y attuale
            velocity.y = 0f;
        }
    }

    /// <summary>
    /// Rileva automaticamente i bounds della scena
    /// </summary>
    void DetectSceneBounds()
    {
        // Cerca tutti i renderer nella scena (escluso il player)
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        
        if (renderers.Length > 0)
        {
            Bounds sceneBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;
            
            foreach (Renderer renderer in renderers)
            {
                // Esclude il player stesso e oggetti UI/particelle
                if (renderer.gameObject != gameObject && 
                    !renderer.name.Contains("UI") &&
                    !renderer.name.Contains("Canvas") &&
                    !renderer.name.Contains("Particle") &&
                    renderer.bounds.size.magnitude > 0.1f) // Esclude oggetti troppo piccoli
                {
                    if (!boundsInitialized)
                    {
                        sceneBounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        sceneBounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            
            if (boundsInitialized)
            {
                // Usa bounds realistici per Sponza
                minBounds = sceneBounds.min - new Vector3(5f, 1f, 5f);
                maxBounds = sceneBounds.max + new Vector3(5f, 10f, 5f);
                
                // Y minimo basato sulla posizione del player, non sui bounds della scena
                minBounds.y = transform.position.y - 5f; // 5 metri sotto il player
                minY = minBounds.y;
                
                Debug.Log($"Bounds rilevati per Sponza: MinY={minY} (basato su posizione player)");
            }
            else
            {
                Debug.Log("Detection bounds fallita, usando valori di default");
            }
        }
    }

    /// <summary>
    /// Limita la posizione ai bounds
    /// </summary>
    Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Max(position.y, minBounds.y), // Non limitare Y verso l'alto
            Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
        );
    }

    /// <summary>
    /// Controlla se il player è uscito dai bounds e lo riposiziona
    /// </summary>
    void CheckBoundaries()
    {
        if (!enableBoundaries) return;
        
        Vector3 currentPos = transform.position;
        
        // Evita respawn continui con cooldown
        if (Time.time - lastRespawnTime < respawnCooldown)
        {
            return;
        }
        
        // Controlla caduta sotto il pavimento (solo se MOLTO sotto)
        if (currentPos.y < minY)
        {
            Debug.LogWarning($"Player caduto sotto {minY}! Posizione attuale: {currentPos.y}");
            RespawnPlayer();
            return;
        }
        
        // Controlla limiti X e Z solo se enableBoundaries è true
        float margin = 5f; // Margine molto generoso
        bool outOfBounds = currentPos.x < minBounds.x - margin || currentPos.x > maxBounds.x + margin ||
                          currentPos.z < minBounds.z - margin || currentPos.z > maxBounds.z + margin;
        
        if (outOfBounds)
        {
            Debug.LogWarning($"Player fuori bounds: {currentPos}");
            RespawnPlayer();
        }
    }

    /// <summary>
    /// Riposiziona il player al punto di spawn
    /// </summary>
    void RespawnPlayer()
    {
        // Evita respawn continui
        if (Time.time - lastRespawnTime < respawnCooldown)
        {
            return;
        }
        
        controller.enabled = false;
        transform.position = spawnPoint;
        controller.enabled = true;
        velocity = Vector3.zero;
        
        lastRespawnTime = Time.time;
        Debug.Log("Player riposizionato al spawn point");
    }

    /// <summary>
    /// Blocca/sblocca cursore
    /// </summary>
    void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Metodi di utilità
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void SetRunSpeed(float newRunSpeed)
    {
        runSpeed = newRunSpeed;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public float GetCurrentSpeed()
    {
        return controller.velocity.magnitude;
    }

    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        velocity = Vector3.zero;
    }

    /// <summary>
    /// Metodi di utilità per boundaries
    /// </summary>
    [ContextMenu("Rileva Bounds Automaticamente")]
    public void ReScanBounds()
    {
        DetectSceneBounds();
    }

    [ContextMenu("Respawn Player")]
    public void ForceRespawn()
    {
        RespawnPlayer();
    }

    public void SetSpawnPoint(Vector3 newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
    }

    public void SetBounds(Vector3 min, Vector3 max)
    {
        minBounds = min;
        maxBounds = max;
        enableBoundaries = true;
    }

    void OnDrawGizmosSelected()
    {
        if (enableBoundaries)
        {
            // Disegna boundaries
            Gizmos.color = Color.cyan;
            Vector3 center = (minBounds + maxBounds) * 0.5f;
            Vector3 size = maxBounds - minBounds;
            Gizmos.DrawWireCube(center, size);
            
            // Disegna spawn point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint, 0.5f);
            
            // Warning se vicino ai limiti
            if (Application.isPlaying)
            {
                Vector3 pos = transform.position;
                bool nearBoundary = pos.x <= minBounds.x + 2f || pos.x >= maxBounds.x - 2f ||
                                   pos.z <= minBounds.z + 2f || pos.z >= maxBounds.z - 2f ||
                                   pos.y <= minBounds.y + 1f;
                
                if (nearBoundary)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, 1f);
                }
            }
        }
    }

    void HandleMouseLook()
    {
        // Ottieni input del mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotazione orizzontale del player
        transform.Rotate(Vector3.up * mouseX);

        // Rotazione verticale della camera (con limiti)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Applica solo la rotazione, mantieni la posizione Y manuale della camera
        Vector3 currentCameraPos = cameraTransform.position;
        cameraTransform.localRotation = Quaternion.Euler(xRotation, cameraTransform.localRotation.eulerAngles.y, 0f);
        
        // Assicurati che la camera mantenga la sua posizione Y impostata manualmente
        cameraTransform.position = new Vector3(cameraTransform.position.x, currentCameraPos.y, cameraTransform.position.z);
    }
}