using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Script per configurare facilmente la sfera colorata nella scena Sponza
/// </summary>
public class SponzaSphereSetup : EditorWindow
{
    [Header("Configurazione Sfera")]
    private Vector3 spherePosition = new Vector3(0, 2, 0);
    private float sphereScale = 1.0f;
    private Color initialColor = Color.red;
    
    [Header("Configurazione SphereColorizer")]
    private bool useRandomColor = false;
    private bool animateColor = true;
    private float colorChangeSpeed = 1.0f;
    private bool cycleColors = true;
    private bool changeColorOnClick = true;
    private bool changeColorOnHover = false;

    [MenuItem("Sponza Tools/Setup Colored Sphere")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SponzaSphereSetup), false, "Sponza Sphere Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Setup Sfera Colorata per Sponza", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Configurazione Posizione e Scala", EditorStyles.boldLabel);
        spherePosition = EditorGUILayout.Vector3Field("Posizione Sfera", spherePosition);
        sphereScale = EditorGUILayout.FloatField("Scala Sfera", sphereScale);
        initialColor = EditorGUILayout.ColorField("Colore Iniziale", initialColor);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Configurazione SphereColorizer", EditorStyles.boldLabel);
        useRandomColor = EditorGUILayout.Toggle("Usa Colore Casuale", useRandomColor);
        animateColor = EditorGUILayout.Toggle("Anima Colore", animateColor);
        colorChangeSpeed = EditorGUILayout.FloatField("Velocità Animazione", colorChangeSpeed);
        cycleColors = EditorGUILayout.Toggle("Cicla Colori Palette", cycleColors);
        changeColorOnClick = EditorGUILayout.Toggle("Cambia al Click", changeColorOnClick);
        changeColorOnHover = EditorGUILayout.Toggle("Cambia al Hover", changeColorOnHover);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Crea Sfera in Sponza", GUILayout.Height(40)))
        {
            CreateSphereInSponza();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Trova e Configura Sfera Esistente", GUILayout.Height(30)))
        {
            ConfigureExistingSphere();
        }
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Posizioni Preimpostate:", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Centro"))
            spherePosition = new Vector3(0, 2, 0);
        if (GUILayout.Button("Sinistra"))
            spherePosition = new Vector3(-5, 2, 0);
        if (GUILayout.Button("Destra"))
            spherePosition = new Vector3(5, 2, 0);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Davanti"))
            spherePosition = new Vector3(0, 2, 5);
        if (GUILayout.Button("Dietro"))
            spherePosition = new Vector3(0, 2, -5);
        if (GUILayout.Button("Alto"))
            spherePosition = new Vector3(0, 5, 0);
        GUILayout.EndHorizontal();
    }

    private void CreateSphereInSponza()
    {
        // Verifica se siamo nella scena Sponza
        string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
        if (!currentScenePath.Contains("Sponza"))
        {
            if (EditorUtility.DisplayDialog("Scena Sbagliata", 
                "Non sei nella scena Sponza. Vuoi aprirla automaticamente?", 
                "Sì", "No"))
            {
                OpenSponzaScene();
                return;
            }
            else
            {
                EditorUtility.DisplayDialog("Attenzione", 
                    "Apri la scena Sponza prima di creare la sfera.", "OK");
                return;
            }
        }

        // Crea la sfera
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Colored Sphere";
        
        // Imposta posizione e scala
        sphere.transform.position = spherePosition;
        sphere.transform.localScale = Vector3.one * sphereScale;
        
        // Aggiungi il component SphereColorizer
        SphereColorizer colorizer = sphere.AddComponent<SphereColorizer>();
        
        // Configura il colorizer tramite reflection (dato che i campi sono private)
        ConfigureColorizerComponent(colorizer);
        
        // Assicurati che abbia un collider per l'interazione
        SphereCollider collider = sphere.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = sphere.AddComponent<SphereCollider>();
        }
        
        // Seleziona la sfera nell'editor
        Selection.activeGameObject = sphere;
        
        Debug.Log($"Sfera colorata creata in posizione {spherePosition} nella scena Sponza!");
        
        EditorUtility.DisplayDialog("Successo!", 
            "Sfera colorata creata con successo nella scena Sponza!\n\n" +
            "La sfera è ora selezionata nell'Inspector dove puoi modificare ulteriormente le impostazioni.", 
            "OK");
    }

    private void ConfigureExistingSphere()
    {
        GameObject[] spheres = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject targetSphere = null;
        
        foreach (GameObject obj in spheres)
        {
            if (obj.name.Contains("Sphere") || obj.GetComponent<SphereCollider>() != null)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null && 
                    meshFilter.sharedMesh.name.Contains("Sphere"))
                {
                    targetSphere = obj;
                    break;
                }
            }
        }
        
        if (targetSphere == null)
        {
            EditorUtility.DisplayDialog("Nessuna Sfera Trovata", 
                "Non è stata trovata nessuna sfera nella scena. Usa 'Crea Sfera in Sponza' invece.", "OK");
            return;
        }
        
        // Aggiungi o configura il SphereColorizer
        SphereColorizer colorizer = targetSphere.GetComponent<SphereColorizer>();
        if (colorizer == null)
        {
            colorizer = targetSphere.AddComponent<SphereColorizer>();
        }
        
        ConfigureColorizerComponent(colorizer);
        
        // Seleziona la sfera
        Selection.activeGameObject = targetSphere;
        
        Debug.Log($"Configurato SphereColorizer su {targetSphere.name}!");
        
        EditorUtility.DisplayDialog("Successo!", 
            $"SphereColorizer configurato su {targetSphere.name}!\n\n" +
            "La sfera è ora selezionata nell'Inspector.", "OK");
    }

    private void ConfigureColorizerComponent(SphereColorizer colorizer)
    {
        // Usa SerializedObject per modificare i campi privati
        SerializedObject serializedColorizer = new SerializedObject(colorizer);
        
        serializedColorizer.FindProperty("targetColor").colorValue = initialColor;
        serializedColorizer.FindProperty("useRandomColor").boolValue = useRandomColor;
        serializedColorizer.FindProperty("animateColor").boolValue = animateColor;
        serializedColorizer.FindProperty("colorChangeSpeed").floatValue = colorChangeSpeed;
        serializedColorizer.FindProperty("cycleColors").boolValue = cycleColors;
        serializedColorizer.FindProperty("changeColorOnClick").boolValue = changeColorOnClick;
        serializedColorizer.FindProperty("changeColorOnHover").boolValue = changeColorOnHover;
        
        serializedColorizer.ApplyModifiedProperties();
    }

    private void OpenSponzaScene()
    {
        string sponzaScenePath = "Assets/Sponza/DemoScene/Sponza.unity";
        
        if (System.IO.File.Exists(sponzaScenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sponzaScenePath);
            Debug.Log("Scena Sponza aperta!");
        }
        else
        {
            EditorUtility.DisplayDialog("Errore", 
                "Impossibile trovare la scena Sponza nel percorso: " + sponzaScenePath, "OK");
        }
    }
}

/// <summary>
/// Script di utility per aggiungere rapidamente sfere colorate alla scena
/// </summary>
public class QuickSphereCreator
{
    [MenuItem("GameObject/3D Object/Colored Sphere", false, 1)]
    public static void CreateColoredSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Colored Sphere";
        sphere.AddComponent<SphereColorizer>();
        
        // Posiziona davanti alla camera
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            sphere.transform.position = sceneCamera.transform.position + sceneCamera.transform.forward * 5;
        }
        
        Selection.activeGameObject = sphere;
    }
}
