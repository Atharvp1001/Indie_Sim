using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
public class ChunkedGorePainter : MonoBehaviour
{
    [Header("Chunk Settings")]
    public float chunkSize = 10f;
    public int chunkResolution = 512;
    public bool showDebugGrid = true; 
    
    [Header("Rendering")]
    public Shader splatShader;
    public Shader displayShader;
    public Texture2D brushTexture;
    
    [Header("Splat Settings")]
    public Vector2 splatSizeRange = new Vector2(0.2f, 0.6f);
    public Color bloodColor = new Color(0.6f, 0.05f, 0.05f, 1f);
    
    [Header("Display Settings")]
    // --- THIS IS THE MISSING VARIABLE ---
    public Transform chunkDisplayParent; 
    [Tooltip("Type the exact name of your Sorting Layer (e.g., 'Floor' or 'Gore')")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = 5; 
    
    private Dictionary<Vector2Int, BloodChunk> loadedChunks = new Dictionary<Vector2Int, BloodChunk>();
    private Material splatMaterial;
    private CompositeCollider2D compositeCollider;

    void Start()
    {
        compositeCollider = GetComponent<CompositeCollider2D>();
        
        if (splatShader == null) splatShader = Shader.Find("Hidden/SplatPainter");
        if (displayShader == null) displayShader = Shader.Find("Custom/BloodDisplay");
        
        splatMaterial = new Material(splatShader);
        
        // If no parent is assigned in Inspector, create one automatically
        if (chunkDisplayParent == null)
        {
            GameObject parent = new GameObject("Gore_Root");
            parent.transform.SetParent(transform); 
            parent.transform.localPosition = Vector3.zero;
            chunkDisplayParent = parent.transform;
        }
    }

    public void PaintSplat(Vector3 worldPos)
    {
        // Check if there is a ground tile here (permissive check)
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        
        float randomSize = Random.Range(splatSizeRange.x, splatSizeRange.y);
        float splatUVSize = randomSize / chunkSize;
        float radius = randomSize * 0.5f;

        Vector2Int minChunk = WorldToChunkCoord(new Vector3(worldPos.x - radius, worldPos.y - radius, 0));
        Vector2Int maxChunk = WorldToChunkCoord(new Vector3(worldPos.x + radius, worldPos.y + radius, 0));

        for (int x = minChunk.x; x <= maxChunk.x; x++)
        {
            for (int y = minChunk.y; y <= maxChunk.y; y++)
            {
                ExecutePaintOnChunk(new Vector2Int(x, y), worldPos, splatUVSize);
            }
        }
    }

    private void ExecutePaintOnChunk(Vector2Int coord, Vector3 worldPos, float uvSize)
    {
        BloodChunk chunk = GetOrCreateChunk(coord);
        Vector2 chunkWorldOrigin = ChunkCoordToWorldOrigin(coord);
        
        Vector2 localPos = new Vector2(worldPos.x - chunkWorldOrigin.x, worldPos.y - chunkWorldOrigin.y);
        Vector2 uv = new Vector2(localPos.x / chunkSize, localPos.y / chunkSize);
        
        splatMaterial.SetTexture("_BrushTex", brushTexture);
        splatMaterial.SetVector("_SplatPos", new Vector4(uv.x, uv.y, 0, 0));
        splatMaterial.SetFloat("_SplatSize", uvSize);
        splatMaterial.SetColor("_Color", bloodColor);
        
        RenderTexture tempRT = RenderTexture.GetTemporary(chunk.canvas.width, chunk.canvas.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(chunk.canvas, tempRT); 
        Graphics.Blit(tempRT, chunk.canvas, splatMaterial); 
        RenderTexture.ReleaseTemporary(tempRT);
    }
    
    BloodChunk GetOrCreateChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out BloodChunk existingChunk))
            return existingChunk;
        
        BloodChunk newChunk = new BloodChunk(chunkCoord, chunkResolution, displayShader);
        CreateChunkDisplay(newChunk);
        loadedChunks[chunkCoord] = newChunk;
        return newChunk;
    }
    
    void CreateChunkDisplay(BloodChunk chunk)
    {
        GameObject displayObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        displayObj.name = $"Chunk_{chunk.chunkCoord.x}_{chunk.chunkCoord.y}";
        
        // This line was causing your error because chunkDisplayParent wasn't defined
        displayObj.transform.SetParent(chunkDisplayParent);
        
        if (displayObj.TryGetComponent<Collider>(out var col)) Destroy(col);

        Mesh mesh = displayObj.GetComponent<MeshFilter>().mesh;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        displayObj.transform.rotation = Quaternion.identity;
        Vector2 worldOrigin = ChunkCoordToWorldOrigin(chunk.chunkCoord);
        
        float targetZ = transform.position.z - 0.05f;
        displayObj.transform.position = new Vector3(worldOrigin.x + chunkSize * 0.5f, worldOrigin.y + chunkSize * 0.5f, targetZ);
        displayObj.transform.localScale = new Vector3(chunkSize, chunkSize, 1);
        
        Renderer renderer = displayObj.GetComponent<Renderer>();
        renderer.material = chunk.displayMaterial;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        
        renderer.receiveShadows = false;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        chunk.displayObject = displayObj;
    }
    
    Vector2Int WorldToChunkCoord(Vector3 worldPos) => new Vector2Int(Mathf.FloorToInt(worldPos.x / chunkSize), Mathf.FloorToInt(worldPos.y / chunkSize));
    Vector2 ChunkCoordToWorldOrigin(Vector2Int chunkCoord) => new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);

    void OnDrawGizmos()
    {
        if (!showDebugGrid) return;
        Gizmos.color = Color.green;
        foreach (var coord in loadedChunks.Keys)
        {
            Vector2 origin = ChunkCoordToWorldOrigin(coord);
            Vector3 center = new Vector3(origin.x + chunkSize * 0.5f, origin.y + chunkSize * 0.5f, transform.position.z);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, chunkSize, 0.1f));
        }
    }

    void OnDestroy()
    {
        if (splatMaterial != null) Destroy(splatMaterial);
        foreach (var chunk in loadedChunks.Values) chunk.Cleanup();
        loadedChunks.Clear();
    }
}

public class BloodChunk
{
    public Vector2Int chunkCoord;
    public RenderTexture canvas;
    public GameObject displayObject;
    public Material displayMaterial;
    
    public BloodChunk(Vector2Int coord, int resolution, Shader displayShader)
    {
        chunkCoord = coord;
        canvas = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
        canvas.filterMode = FilterMode.Point;
        canvas.wrapMode = TextureWrapMode.Clamp;
        canvas.Create();
        
        RenderTexture.active = canvas;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
        
        displayMaterial = new Material(displayShader);
        displayMaterial.SetTexture("_MainTex", canvas);
    }
    
    public void Cleanup()
    {
        if (canvas != null) { canvas.Release(); Object.Destroy(canvas); }
        if (displayMaterial != null) Object.Destroy(displayMaterial);
        if (displayObject != null) Object.Destroy(displayObject);
    }
}