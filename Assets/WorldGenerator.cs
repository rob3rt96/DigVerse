using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generează procedural întreaga lume Motherload în DigVerse:
/// - Lățime 300 metri (de la x = -150 la +150)
/// - Adâncime până la -8000 metri generată dinamic în bucăți (chunk-uri) pentru performanță 60+ FPS
/// - Nivelul 0 la suprafață cu gazon verde (GrassBlock) și cele 4 clădiri
/// - Goluri naturale conectate (1-3 blocuri verticale, orizontale sau în L)
/// - Distribuție de minerale, stânci (Boulder) și lavă în funcție de adâncimea exactă
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    [Header("Dimensiuni Hartă")]
    [Tooltip("Lățimea totală a lumii în metri/blocuri")]
    public int mapWidth = 100;
    [Tooltip("Adâncimea maximă a lumii")]
    public int maxDepth = 8000;
    public float surfaceY = 0f;

    [Header("Optimizare / Generare pe Bucăți (Streaming)")]
    [Tooltip("Câți metri în adâncime se generează la pornire")]
    public int initialGeneratedDepth = 60;
    [Tooltip("Mărimea unui nou chunk subteran adăugat când jucătorul coboară")]
    public int chunkDepth = 40;
    [Tooltip("Distanța minimă până la capătul generat înainte de a crea următorul chunk")]
    public int bufferDistance = 20;

    [Header("Probabilități Goluri Naturale")]
    [Range(0.01f, 0.15f)]
    public float cavitySpawnChance = 0.045f;

    [Header("Referințe Tilemap")]
    public Tilemap groundTilemap;

    [Header("Tile-uri Minerale & Blocuri")]
    public TileBase dirtTile;
    public TileBase grassTile;
    public TileBase boulderTile;
    public TileBase lavaTile;

    public TileBase ironiumTile;
    public TileBase bronziumTile;
    public TileBase silveriumTile;
    public TileBase goldiumTile;
    public TileBase platinumTile;
    public TileBase unobtaniumTile;
    public TileBase emeraldTile;
    public TileBase rubyTile;
    public TileBase diamondTile;
    public TileBase amazoniteTile;

    private int lowestGeneratedY = 1;
    private Transform playerTransform;
    private HashSet<Vector2Int> cavityCells = new HashSet<Vector2Int>();

    void Awake()
    {
        if (groundTilemap == null)
        {
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj != null) groundTilemap = groundObj.GetComponent<Tilemap>();
            if (groundTilemap == null) groundTilemap = FindAnyObjectByType<Tilemap>();
        }

        LoadTilesFromResources();
    }

    void Start()
    {
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            // Poziționăm jucătorul la suprafață, pe nivelul 0 (pe iarbă)
            playerTransform.position = new Vector3(0.5f, 1.5f, 0f);
        }

        // Curățăm plăcile vechi testate manual și asigurăm culoarea albă pură
        if (groundTilemap != null)
        {
            groundTilemap.color = Color.white;
            groundTilemap.ClearAllTiles();
        }

        // Asigurăm prezența fundalului subteran radial
        SetupSubterraneanBackground();

        // Construim clădirile la suprafață (Nivelul 0)
        SpawnSurfaceBuildings();

        // Generăm Nivelul 0 (Iarbă) și primul chunk subteran
        GenerateWorldToDepth(initialGeneratedDepth);
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Când jucătorul se apropie de fundul lumii generate, generăm următorul chunk în jos
        int playerCurrentDepth = Mathf.RoundToInt(surfaceY - playerTransform.position.y);
        int currentGeneratedDepth = Mathf.Abs(lowestGeneratedY);

        if (playerCurrentDepth + bufferDistance >= currentGeneratedDepth && currentGeneratedDepth < maxDepth)
        {
            int nextTargetDepth = Mathf.Min(maxDepth, currentGeneratedDepth + chunkDepth);
            GenerateWorldToDepth(nextTargetDepth);
        }
    }

    private void LoadTilesFromResources()
    {
        if (dirtTile == null) dirtTile = Resources.Load<TileBase>("Minerals/DirtBlock");
        if (grassTile == null) grassTile = Resources.Load<TileBase>("Minerals/GrassBlock");
        if (boulderTile == null) boulderTile = Resources.Load<TileBase>("Minerals/Boulder");
        if (lavaTile == null) lavaTile = Resources.Load<TileBase>("Minerals/Lava");

        if (ironiumTile == null) ironiumTile = Resources.Load<TileBase>("Minerals/Ironium");
        if (bronziumTile == null) bronziumTile = Resources.Load<TileBase>("Minerals/Bronzium");
        if (silveriumTile == null) silveriumTile = Resources.Load<TileBase>("Minerals/Silverium");
        if (goldiumTile == null) goldiumTile = Resources.Load<TileBase>("Minerals/Goldium");
        if (platinumTile == null) platinumTile = Resources.Load<TileBase>("Minerals/Platinum");
        if (unobtaniumTile == null) unobtaniumTile = Resources.Load<TileBase>("Minerals/Unobtanium");
        if (emeraldTile == null) emeraldTile = Resources.Load<TileBase>("Minerals/Emerald");
        if (rubyTile == null) rubyTile = Resources.Load<TileBase>("Minerals/Ruby");
        if (diamondTile == null) diamondTile = Resources.Load<TileBase>("Minerals/Diamond");
        if (amazoniteTile == null) amazoniteTile = Resources.Load<TileBase>("Minerals/Amazonite");
    }

    private void SetupSubterraneanBackground()
    {
        if (FindAnyObjectByType<SubterraneanBackground>() == null)
        {
            GameObject bgObj = new GameObject("SubterraneanBackground");
            SubterraneanBackground bg = bgObj.AddComponent<SubterraneanBackground>();
            bg.surfaceY = surfaceY;
            if (playerTransform != null) bg.player = playerTransform;
            if (Camera.main != null) bg.targetCamera = Camera.main.transform;
        }
    }

    /// <summary>
    /// Plasează cele 4 clădiri la Nivelul 0, împărțite egal de-a lungul celor 300 metri
    /// Plasează cele 4 clădiri la Nivelul 0, împărțite egal de-a lungul celor 100 metri
    /// </summary>
    private void SpawnSurfaceBuildings()
    {
        // 4 Clădiri împărțite echidistant în cei 100m (de la stânga la dreapta):
        // 1. Stație Fuel: x = -36
        // 2. Centru Vânzare Minerale: x = -12
        // 3. Magazin Piese/Upgrade-uri: x = +12
        // 4. Magazin Echipamente Opționale (Junk Shop): x = +36

        CreateBuilding("FuelStation", BuildingType.FuelStation, "Stație Combustibil", -36f, 1.0f);
        CreateBuilding("MineralProcessing", BuildingType.MineralProcessing, "Procesare Minerale", -12f, 1.0f);
        CreateBuilding("UpgradeShop", BuildingType.UpgradeShop, "Magazin Piese & Upgrade-uri", 12f, 1.0f);
        CreateBuilding("JunkShop", BuildingType.JunkShop, "Echipamente Opționale", 36f, 1.0f);
    }

    private void CreateBuilding(string spriteResourceName, BuildingType type, string bName, float xPos, float yOffset)
    {
        if (GameObject.Find(bName) != null) return;

        GameObject bObj = new GameObject(bName);
        bObj.transform.position = new Vector3(xPos, surfaceY + yOffset, 0f);

        SpriteRenderer sr = bObj.AddComponent<SpriteRenderer>();
        Sprite bSprite = Resources.Load<Sprite>($"Buildings/{spriteResourceName}");
        if (bSprite != null)
        {
            sr.sprite = bSprite;
        }
        sr.sortingOrder = 5; // În fața solului

        BuildingTrigger trigger = bObj.AddComponent<BuildingTrigger>();
        trigger.buildingType = type;
        trigger.buildingName = bName;

        BoxCollider2D col = bObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(6f, 5f);
        col.offset = new Vector2(0f, 2.5f);
    }

    /// <summary>
    /// Generează terenul pe straturi verticale până la adâncimea specificată
    /// </summary>
    public void GenerateWorldToDepth(int targetDepth)
    {
        if (groundTilemap == null) return;

        int halfWidth = mapWidth / 2; // de la -150 la +150
        int startY = lowestGeneratedY > 0 ? 0 : lowestGeneratedY - 1;
        int endY = -targetDepth;

        if (startY < endY) return;

        int rowsToGenerate = Mathf.Abs(startY - endY) + 1;
        int totalCells = (mapWidth + 2) * rowsToGenerate;

        List<Vector3Int> positions = new List<Vector3Int>(totalCells);
        List<TileBase> tiles = new List<TileBase>(totalCells);

        // Pre-generăm golurile (cavities) pentru acest segment
        PrecalculateCavities(startY, endY, halfWidth);

        for (int y = startY; y >= endY; y--)
        {
            int depth = Mathf.Abs(y);

            // Pereți laterali de barieră (indestructibili) la marginile hărții
            positions.Add(new Vector3Int(-halfWidth - 1, y, 0));
            tiles.Add(boulderTile);

            positions.Add(new Vector3Int(halfWidth, y, 0));
            tiles.Add(boulderTile);

            for (int x = -halfWidth; x < halfWidth; x++)
            {
                positions.Add(new Vector3Int(x, y, 0));

                if (y == 0)
                {
                    // Nivelul 0: Gazon verde (GrassBlock)
                    tiles.Add(grassTile != null ? grassTile : dirtTile);
                }
                else
                {
                    // Verificăm dacă celula face parte dintr-un gol natural (cavitate)
                    if (cavityCells.Contains(new Vector2Int(x, y)))
                    {
                        tiles.Add(null); // Gol (aer) - se vede fundalul colorat din spate
                    }
                    else
                    {
                        // Alegem mineralul sau blocul corespunzător adâncimii
                        tiles.Add(GetTileForDepth(depth));
                    }
                }
            }
        }

        // Aplicăm toate blocurile dintr-o singură mișcare rapidă (SetTiles în masă)
        groundTilemap.SetTiles(positions.ToArray(), tiles.ToArray());

        lowestGeneratedY = endY;
    }

    /// <summary>
    /// Generează goluri naturale cuplate (1-3 blocuri verticale, orizontale sau în formă de L)
    /// </summary>
    private void PrecalculateCavities(int startY, int endY, int halfWidth)
    {
        for (int y = startY; y >= endY; y--)
        {
            if (y == 0) continue; // Nu punem goluri la suprafață (Nivelul 0)

            for (int x = -halfWidth + 2; x < halfWidth - 2; x++)
            {
                if (Random.value < cavitySpawnChance)
                {
                    int shape = Random.Range(0, 3);
                    int length = Random.Range(1, 4); // 1, 2 sau 3 blocuri

                    switch (shape)
                    {
                        case 0:
                            // Orizontal: 1 la 3 blocuri
                            for (int dx = 0; dx < length; dx++)
                            {
                                cavityCells.Add(new Vector2Int(x + dx, y));
                            }
                            x += length; // Sărim peste celulele generate
                            break;

                        case 1:
                            // Vertical: 1 la 3 blocuri
                            for (int dy = 0; dy < length; dy++)
                            {
                                cavityCells.Add(new Vector2Int(x, y - dy));
                            }
                            break;

                        case 2:
                            // Formă de L (3 blocuri în total: 2 verticale + 1 orizontal)
                            cavityCells.Add(new Vector2Int(x, y));
                            cavityCells.Add(new Vector2Int(x, y - 1));
                            cavityCells.Add(new Vector2Int(x + 1, y - 1));
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Selectează procedural mineralul conform adâncimilor din jocul Motherload
    /// </summary>
    private TileBase GetTileForDepth(int depth)
    {
        float roll = Random.value * 100f;

        // 1. Lavă (pericol letal): apare rar de la -1000m și crește adânc
        if (depth >= 1000 && roll < GetLavaChance(depth))
        {
            return lavaTile != null ? lavaTile : dirtTile;
        }

        // 2. Stâncă (Boulder - bloc masiv indestructibil): de la -25m în jos
        if (depth >= 25 && roll < (GetLavaChance(depth) + GetBoulderChance(depth)))
        {
            return boulderTile != null ? boulderTile : dirtTile;
        }

        // 3. Minerale prețioase de la cele mai rare (adânci) la cele comune:
        if (depth >= 1900 && Random.value < 0.005f) return amazoniteTile ?? dirtTile;
        if (depth >= 1750 && Random.value < 0.008f) return diamondTile ?? dirtTile;
        if (depth >= 1500 && Random.value < 0.012f) return rubyTile ?? dirtTile;
        if (depth >= 1200 && Random.value < 0.016f) return emeraldTile ?? dirtTile;
        if (depth >= 800  && Random.value < 0.022f) return unobtaniumTile ?? dirtTile;
        if (depth >= 500  && Random.value < 0.030f) return platinumTile ?? dirtTile;
        if (depth >= 75   && Random.value < 0.045f) return goldiumTile ?? dirtTile;
        if (depth >= 8    && Random.value < 0.055f) return silveriumTile ?? dirtTile;
        if (depth >= 8    && Random.value < 0.070f) return bronziumTile ?? dirtTile;
        if (depth >= 8    && Random.value < 0.090f) return ironiumTile ?? dirtTile;

        // Implicit: Pământ simplu (DirtBlock)
        return dirtTile;
    }

    private float GetLavaChance(int depth)
    {
        if (depth < 1000) return 0f;
        if (depth < 3000) return 0.5f;
        if (depth < 5500) return 1.5f;
        return 3.5f;
    }

    private float GetBoulderChance(int depth)
    {
        if (depth < 25) return 0f;
        if (depth < 500) return 2.5f;
        if (depth < 2000) return 4.5f;
        return 7.0f;
    }
}
