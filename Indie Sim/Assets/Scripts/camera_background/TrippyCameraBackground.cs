using UnityEngine;

public class TrippyCameraBackground : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color darkColor = new Color(0.05f, 0.05f, 0.05f);
    [SerializeField] private Color midColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color lightColor = new Color(0.3f, 0.3f, 0.3f);
    
    [Header("Section Settings")]
    [SerializeField] private int sectionsX = 3;
    [SerializeField] private int sectionsY = 3;
    
    [Header("Animation Settings")]
    [SerializeField] private float baseSpeed = 1f;
    [SerializeField] private float speedVariation = 0.5f;
    [SerializeField] private float strobeChance = 0.3f;
    [SerializeField] private float strobeSpeed = 4f;
    
    private Camera cam;
    private GameObject backgroundQuad;
    private Material backgroundMaterial;
    private float[,] sectionPhases;
    private float[,] sectionSpeeds;
    private bool[,] sectionStrobes;
    private float time;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("TrippyCameraBackground must be attached to a Camera!");
            enabled = false;
            return;
        }
        
        // Initialize section data
        sectionPhases = new float[sectionsX, sectionsY];
        sectionSpeeds = new float[sectionsX, sectionsY];
        sectionStrobes = new bool[sectionsX, sectionsY];
        
        for (int x = 0; x < sectionsX; x++)
        {
            for (int y = 0; y < sectionsY; y++)
            {
                sectionPhases[x, y] = Random.Range(0f, Mathf.PI * 2f);
                sectionSpeeds[x, y] = baseSpeed + Random.Range(-speedVariation, speedVariation);
                sectionStrobes[x, y] = Random.value < strobeChance;
            }
        }
        
        // Create background quad
        CreateBackgroundQuad();
        
        Debug.Log("Trippy background initialized!");
    }
    
    void CreateBackgroundQuad()
    {
        // Create a quad that covers the entire camera view
        backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundQuad.name = "TrippyBackground";
        
        // Remove collider
        Destroy(backgroundQuad.GetComponent<Collider>());
        
        // Position it far in front of camera
        backgroundQuad.transform.parent = transform;
        backgroundQuad.transform.localPosition = new Vector3(0, 0, cam.farClipPlane - 1f);
        
        // Scale to cover camera view
        float distance = cam.farClipPlane - 1f;
        float height = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;
        backgroundQuad.transform.localScale = new Vector3(width, height, 1f);
        
        // Create shader
        Shader shader = Shader.Find("Hidden/TrippyBackground");
        if (shader == null)
        {
            Debug.LogError("Shader not found! Using unlit shader instead.");
            shader = Shader.Find("Unlit/Color");
        }
        
        backgroundMaterial = new Material(shader);
        backgroundQuad.GetComponent<Renderer>().material = backgroundMaterial;
        
        // Set render queue to render first
        backgroundMaterial.renderQueue = 1000;
        
        Debug.Log($"Background quad created at distance {distance}");
    }
    
    void Update()
    {
        if (backgroundMaterial == null) return;
        
        time += Time.deltaTime;
        
        // Update shader parameters
        backgroundMaterial.SetColor("_DarkColor", darkColor);
        backgroundMaterial.SetColor("_MidColor", midColor);
        backgroundMaterial.SetColor("_LightColor", lightColor);
        backgroundMaterial.SetInt("_SectionsX", sectionsX);
        backgroundMaterial.SetInt("_SectionsY", sectionsY);
        backgroundMaterial.SetFloat("_Time", time);
        backgroundMaterial.SetFloat("_StrobeSpeed", strobeSpeed);
        
        // Pass section data
        float[] phaseArray = new float[sectionsX * sectionsY];
        float[] speedArray = new float[sectionsX * sectionsY];
        float[] strobeArray = new float[sectionsX * sectionsY];
        
        for (int x = 0; x < sectionsX; x++)
        {
            for (int y = 0; y < sectionsY; y++)
            {
                int idx = y * sectionsX + x;
                if (idx < phaseArray.Length)
                {
                    phaseArray[idx] = sectionPhases[x, y];
                    speedArray[idx] = sectionSpeeds[x, y];
                    strobeArray[idx] = sectionStrobes[x, y] ? 1f : 0f;
                }
            }
        }
        
        backgroundMaterial.SetFloatArray("_SectionPhases", phaseArray);
        backgroundMaterial.SetFloatArray("_SectionSpeeds", speedArray);
        backgroundMaterial.SetFloatArray("_SectionStrobes", strobeArray);
    }
    
    void OnDestroy()
    {
        if (backgroundQuad != null)
        {
            Destroy(backgroundQuad);
        }
        if (backgroundMaterial != null)
        {
            Destroy(backgroundMaterial);
        }
    }
    
    public void RandomizeSections()
    {
        for (int x = 0; x < sectionsX; x++)
        {
            for (int y = 0; y < sectionsY; y++)
            {
                sectionPhases[x, y] = Random.Range(0f, Mathf.PI * 2f);
                sectionSpeeds[x, y] = baseSpeed + Random.Range(-speedVariation, speedVariation);
                sectionStrobes[x, y] = Random.value < strobeChance;
            }
        }
    }
}