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
    public Transform chunkDisplayParent; 
    [Tooltip("Type the exact name of your Sorting Layer (e.g., 'Floor' or 'Gore')")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = 5;
    
    [Header("Preloading")]
    [Tooltip("Transform to center chunk preloading around (e.g., player or camera)")]
    public Transform preloadTarget;
    [Tooltip("Number of chunks to preload in each direction from the target (e.g., 5 = 11x11 grid)")]
    public int preloadRadius = 5;
    
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
        
        // Preload chunks on start
        PreloadChunks();
    }

    void Update()
    {
        // Continuously update preloaded chunks as the target moves
        if (preloadTarget != null)
        {
            PreloadChunks();
        }
    }

    /// <summary>
    /// Preloads chunks in a grid around the preloadTarget.
    /// Creates new chunks that don't exist, keeps existing ones.
    /// </summary>
    void PreloadChunks()
    {
        if (preloadTarget == null) return;
        
        Vector2Int centerChunk = WorldToChunkCoord(preloadTarget.position);
        
        for (int x = centerChunk.x - preloadRadius; x <= centerChunk.x + preloadRadius; x++)
        {
            for (int y = centerChunk.y - preloadRadius; y <= centerChunk.y + preloadRadius; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                
                // Only create if it doesn't already exist
                if (!loadedChunks.ContainsKey(coord))
                {
                    BloodChunk newChunk = new BloodChunk(coord, chunkResolution, displayShader);
                    CreateChunkDisplay(newChunk);
                    loadedChunks[coord] = newChunk;
                }
            }
        }
    }

    public void PaintSplat(Vector3 worldPos)
    {
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
        // Only paint if chunk already exists (preloaded)
        if (!loadedChunks.TryGetValue(coord, out BloodChunk chunk))
            return;
        
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
    
    void CreateChunkDisplay(BloodChunk chunk)
    {
        GameObject displayObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        displayObj.name = $"Chunk_{chunk.chunkCoord.x}_{chunk.chunkCoord.y}";
        
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


    /// <summary>
    /// Call this from your dungeon generator before regenerating the map.
    /// Cleans up all blood chunks, textures, and display objects.
    /// </summary>
    public void ClearAllChunks()
    {
        // ✅ Cleanup each chunk's RenderTexture, material, and GameObject
        foreach (var chunk in loadedChunks.Values)
        {
            chunk.Cleanup();
        }

        loadedChunks.Clear();

        // ✅ Safety pass — destroy any leftover child GameObjects under Gore_Root
        // in case Cleanup() missed anything
        if (chunkDisplayParent != null)
        {
            for (int i = chunkDisplayParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(chunkDisplayParent.GetChild(i).gameObject);
            }
        }

        Debug.Log("[ChunkedGorePainter] All gore chunks cleared.");
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