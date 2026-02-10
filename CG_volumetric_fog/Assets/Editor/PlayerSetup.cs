using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Script per creare rapidamente un Player Controller configurato
/// </summary>
public class PlayerSetup : EditorWindow
{
    [Header("Configurazione Player")]
    private Vector3 playerPosition = Vector3.zero;
    private bool useCharacterController = true;
    private bool setupCamera = true;
    private bool addCollider = true;
    
    [MenuItem("Sponza Tools/Setup Player Controller")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PlayerSetup), false, "Player Controller Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Setup Player Controller", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Configurazione", EditorStyles.boldLabel);
        playerPosition = EditorGUILayout.Vector3Field("Posizione Player", playerPosition);
        useCharacterController = EditorGUILayout.Toggle("Usa CharacterController", useCharacterController);
        setupCamera = EditorGUILayout.Toggle("Setup Camera Automatico", setupCamera);
        addCollider = EditorGUILayout.Toggle("Aggiungi Collider", addCollider);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Crea Player Controller Completo", GUILayout.Height(40)))
        {
            CreatePlayerController();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Configura GameObject Selezionato", GUILayout.Height(30)))
        {
            ConfigureSelectedObject();
        }
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Posizioni Preimpostate:", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Origine"))
            playerPosition = Vector3.zero;
        if (GUILayout.Button("Sponza Centro"))
            playerPosition = new Vector3(0, 1, 0);
        if (GUILayout.Button("Sponza Entrata"))
            playerPosition = new Vector3(0, 1, -10);
        GUILayout.EndHorizontal();
    }

    private void CreatePlayerController()
    {
        // Crea il GameObject player
        GameObject player = new GameObject("Player");
        player.transform.position = playerPosition;
        
        // Aggiungi il componente di movimento
        if (useCharacterController)
        {
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1, 0);
        }
        else
        {
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 5.0f;
            rb.freezeRotation = true;
        }
        
        // Aggiungi collider se richiesto
        if (addCollider && !useCharacterController)
        {
            CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
            collider.height = 2.0f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1, 0);
        }
        
        // Aggiungi il PlayerController
        PlayerController playerController = player.AddComponent<PlayerController>();
        
        // Setup camera se richiesto
        if (setupCamera)
        {
            SetupPlayerCamera(player, playerController);
        }
        
        // Setup ground check
        SetupGroundCheck(player);
        
        // Seleziona il player
        Selection.activeGameObject = player;
        
        Debug.Log("Player Controller creato con successo!");
        
        EditorUtility.DisplayDialog("Successo!", 
            "Player Controller creato e configurato!\n\n" +
            "Controlli:\n" +
            "• WASD: Movimento\n" +
            "• Shift: Corsa\n" +
            "• Spazio: Salto\n" +
            "• Mouse: Rotazione\n" +
            "• Tab: Cambia modalità camera\n" +
            "• Esc: Sblocca cursore", 
            "OK");
    }

    private void ConfigureSelectedObject()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Nessun Oggetto Selezionato", 
                "Seleziona un GameObject prima di configurarlo.", "OK");
            return;
        }
        
        // Aggiungi componenti mancanti
        if (useCharacterController && selected.GetComponent<CharacterController>() == null)
        {
            CharacterController controller = selected.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1, 0);
        }
        else if (!useCharacterController && selected.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = selected.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 5.0f;
            rb.freezeRotation = true;
        }
        
        // Aggiungi PlayerController se non presente
        PlayerController playerController = selected.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = selected.AddComponent<PlayerController>();
        }
        
        // Setup camera se richiesto
        if (setupCamera)
        {
            SetupPlayerCamera(selected, playerController);
        }
        
        Debug.Log($"Configurato PlayerController su {selected.name}!");
        
        EditorUtility.DisplayDialog("Successo!", 
            $"PlayerController configurato su {selected.name}!", "OK");
    }

    private void SetupPlayerCamera(GameObject player, PlayerController playerController)
    {
        // Cerca camera esistente o creane una nuova
        Camera existingCamera = Camera.main;
        if (existingCamera == null)
        {
            existingCamera = FindObjectOfType<Camera>();
        }
        
        if (existingCamera != null)
        {
            // Configura camera esistente
            SetCameraReference(playerController, existingCamera.transform);
        }
        else
        {
            // Crea nuova camera
            GameObject cameraObj = new GameObject("Player Camera");
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.8f, 0);
            
            Camera newCamera = cameraObj.AddComponent<Camera>();
            newCamera.tag = "MainCamera";
            
            // Aggiungi AudioListener se non presente
            if (FindObjectOfType<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }
            
            SetCameraReference(playerController, cameraObj.transform);
        }
    }

    private void SetupGroundCheck(GameObject player)
    {
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -1f, 0);
        
        // Configura reference tramite SerializedObject
        PlayerController playerController = player.GetComponent<PlayerController>();
        SerializedObject serializedController = new SerializedObject(playerController);
        serializedController.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        serializedController.ApplyModifiedProperties();
    }

    private void SetCameraReference(PlayerController playerController, Transform cameraTransform)
    {
        SerializedObject serializedController = new SerializedObject(playerController);
        serializedController.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
        serializedController.ApplyModifiedProperties();
    }
}
