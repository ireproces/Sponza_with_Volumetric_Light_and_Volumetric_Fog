using UnityEngine;

/// <summary>
/// Script per colorare una sfera con diverse modalità di colorazione
/// </summary>
public class SphereColorizer : MonoBehaviour
{
    [Header("Configurazione Colore")]
    [SerializeField] private Color targetColor = Color.green;
    [SerializeField] private bool useRandomColor = false;
    [SerializeField] private bool animateColor = false;
    
    [Header("Animazione Colore")]
    [SerializeField] private float colorChangeSpeed = 1.0f;
    [SerializeField] private bool cycleColors = false;
    [SerializeField] private Color[] colorPalette = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
    
    [Header("Interazione")]
    [SerializeField] private bool changeColorOnClick = false;
    [SerializeField] private bool changeColorOnHover = false;
    
    private Renderer sphereRenderer;
    private Material sphereMaterial;
    private Color originalColor;
    private int currentColorIndex = 0;
    private float timeElapsed = 0f;
    private bool isHovered = false;

    void Start()
    {
        InitializeComponent();
        
        if (useRandomColor)
        {
            SetRandomColor();
        }
        else
        {
            SetColor(targetColor);
        }
    }

    void Update()
    {
        if (animateColor)
        {
            AnimateColor();
        }
        
        HandleInput();
    }

    /// <summary>
    /// Inizializza i componenti necessari
    /// </summary>
    private void InitializeComponent()
    {
        sphereRenderer = GetComponent<Renderer>();
        
        if (sphereRenderer == null)
        {
            Debug.LogError("SphereColorizer: Nessun Renderer trovato su questo GameObject!");
            return;
        }
        
        // Crea una copia del materiale per evitare di modificare l'asset condiviso
        sphereMaterial = sphereRenderer.material;
        originalColor = sphereMaterial.color;
        
        // Debug informazioni materiale
        Debug.Log($"SphereColorizer: Materiale trovato: {sphereMaterial.name}");
        Debug.Log($"SphereColorizer: Shader: {sphereMaterial.shader.name}");
        Debug.Log($"SphereColorizer: Colore originale: {originalColor}");
        
        // Controlla proprietà colore disponibili
        if (sphereMaterial.HasProperty("_BaseColor"))
            Debug.Log("SphereColorizer: Proprietà _BaseColor disponibile");
        if (sphereMaterial.HasProperty("_Color"))
            Debug.Log("SphereColorizer: Proprietà _Color disponibile");
        if (sphereMaterial.HasProperty("_MainColor"))
            Debug.Log("SphereColorizer: Proprietà _MainColor disponibile");
        if (sphereMaterial.HasProperty("_Albedo"))
            Debug.Log("SphereColorizer: Proprietà _Albedo disponibile");
    }

    /// <summary>
    /// Imposta un colore specifico alla sfera
    /// </summary>
    /// <param name="color">Il colore da applicare</param>
    public void SetColor(Color color)
    {
        if (sphereMaterial != null)
        {
            // Prova diverse proprietà di colore comuni
            if (sphereMaterial.HasProperty("_BaseColor"))
            {
                sphereMaterial.SetColor("_BaseColor", color);
            }
            else if (sphereMaterial.HasProperty("_Color"))
            {
                sphereMaterial.SetColor("_Color", color);
            }
            else if (sphereMaterial.HasProperty("_MainColor"))
            {
                sphereMaterial.SetColor("_MainColor", color);
            }
            else if (sphereMaterial.HasProperty("_Albedo"))
            {
                sphereMaterial.SetColor("_Albedo", color);
            }
            else if (sphereMaterial.HasProperty("_EmissionColor"))
            {
                sphereMaterial.SetColor("_EmissionColor", color);
                sphereMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                // Fallback: prova la proprietà color standard
                sphereMaterial.color = color;
                Debug.LogWarning($"SphereColorizer: Proprietà colore non trovata per shader {sphereMaterial.shader.name}. Usando fallback.");
            }
        }
    }

    /// <summary>
    /// Imposta un colore casuale alla sfera
    /// </summary>
    public void SetRandomColor()
    {
        Color randomColor = new Color(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            1f
        );
        SetColor(randomColor);
    }

    /// <summary>
    /// Anima il colore della sfera
    /// </summary>
    private void AnimateColor()
    {
        timeElapsed += Time.deltaTime * colorChangeSpeed;
        
        if (cycleColors && colorPalette.Length > 0)
        {
            // Cicla attraverso i colori della palette
            if (timeElapsed >= 1f)
            {
                currentColorIndex = (currentColorIndex + 1) % colorPalette.Length;
                timeElapsed = 0f;
            }
            
            Color currentColor = colorPalette[currentColorIndex];
            Color nextColor = colorPalette[(currentColorIndex + 1) % colorPalette.Length];
            
            SetColor(Color.Lerp(currentColor, nextColor, timeElapsed));
        }
        else
        {
            // Animazione colore HSV (sfumatura arcobaleno)
            float hue = (timeElapsed * 0.5f) % 1f;
            Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
            SetColor(rainbowColor);
        }
    }

    /// <summary>
    /// Gestisce l'input del mouse
    /// </summary>
    private void HandleInput()
    {
        if (changeColorOnClick && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                OnSphereClicked();
            }
        }
    }

    /// <summary>
    /// Chiamato quando la sfera viene cliccata
    /// </summary>
    private void OnSphereClicked()
    {
        if (useRandomColor)
        {
            SetRandomColor();
        }
        else
        {
            // Cicla attraverso i colori della palette
            if (colorPalette.Length > 0)
            {
                currentColorIndex = (currentColorIndex + 1) % colorPalette.Length;
                SetColor(colorPalette[currentColorIndex]);
            }
        }
    }

    /// <summary>
    /// Chiamato quando il mouse entra nella sfera
    /// </summary>
    void OnMouseEnter()
    {
        if (changeColorOnHover && !isHovered)
        {
            isHovered = true;
            SetColor(Color.white);
        }
    }

    /// <summary>
    /// Chiamato quando il mouse esce dalla sfera
    /// </summary>
    void OnMouseExit()
    {
        if (changeColorOnHover && isHovered)
        {
            isHovered = false;
            SetColor(originalColor);
        }
    }

    /// <summary>
    /// Ripristina il colore originale
    /// </summary>
    public void ResetColor()
    {
        SetColor(originalColor);
    }

    /// <summary>
    /// Metodi pubblici per chiamare da altri script o eventi UI
    /// </summary>
    public void SetRedColor() => SetColor(Color.red);
    public void SetGreenColor() => SetColor(Color.green);
    public void SetBlueColor() => SetColor(Color.blue);
    public void SetYellowColor() => SetColor(Color.yellow);
    public void SetMagentaColor() => SetColor(Color.magenta);
    public void SetCyanColor() => SetColor(Color.cyan);
    public void SetWhiteColor() => SetColor(Color.white);
    public void SetBlackColor() => SetColor(Color.black);

    /// <summary>
    /// Forza l'uso di un materiale Standard che supporta la colorazione
    /// </summary>
    [ContextMenu("Forza Materiale Standard")]
    public void ForceStandardMaterial()
    {
        if (sphereRenderer != null)
        {
            // Crea un nuovo materiale con shader Standard
            Material newMaterial = new Material(Shader.Find("Standard"));
            newMaterial.color = targetColor;
            
            sphereRenderer.material = newMaterial;
            sphereMaterial = newMaterial;
            originalColor = targetColor;
            
            Debug.Log("SphereColorizer: Materiale Standard applicato con successo!");
        }
    }

    /// <summary>
    /// Prova a colorare usando shader URP
    /// </summary>
    [ContextMenu("Prova Materiale URP")]
    public void ForceURPMaterial()
    {
        if (sphereRenderer != null)
        {
            // Cerca shader URP comuni
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? 
                              Shader.Find("Universal Render Pipeline/Unlit") ??
                              Shader.Find("Universal Render Pipeline/Simple Lit");
            
            if (urpShader != null)
            {
                Material newMaterial = new Material(urpShader);
                if (newMaterial.HasProperty("_BaseColor"))
                {
                    newMaterial.SetColor("_BaseColor", targetColor);
                }
                
                sphereRenderer.material = newMaterial;
                sphereMaterial = newMaterial;
                originalColor = targetColor;
                
                Debug.Log($"SphereColorizer: Materiale URP ({urpShader.name}) applicato con successo!");
            }
            else
            {
                Debug.LogWarning("SphereColorizer: Nessuno shader URP trovato, uso Standard");
                ForceStandardMaterial();
            }
        }
    }

    void OnDestroy()
    {
        // Cleanup del materiale per evitare memory leaks
        if (sphereMaterial != null)
        {
            DestroyImmediate(sphereMaterial);
        }
    }
}
