using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MineralData
{
    public string name;
    public TileBase tile;            // Tile-ul asociat din Tile Palette (opțional)
    public int baseValue;           // Valoarea de vânzare ($)
    public float weight;            // Greutate (kg)
    public float commonDepthMeters; // Adâncimea comună în metri (ex: -8m, -75m, etc.)
    public float digFuelCost = 2f;  // Consum de combustibil la săpare
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Mișcare și Combustibil")]
    public float speed = 5f;
    public float maxFuel = 100f;
    public float currentFuel;
    [Tooltip("Consum automat de combustibil pe secundă (chiar și când vehiculul stă pe loc)")]
    public float fuelConsumptionPerSecond = 1f;

    [Header("Săpare Teren")]
    public Tilemap groundTilemap;
    [Tooltip("Consum de combustibil pentru săparea unui DirtBlock simplu")]
    public float digFuelCost = 2f;

    [Header("Inventar și Minerale")]
    public int money = 0;
    public float cargoWeight = 0f;
    public float maxCargoWeight = 100f;
    public List<MineralData> minerals = new List<MineralData>();

    [Header("Urmărire Cameră & Zoom (Automat)")]
    public bool followCamera = true;
    [Tooltip("Decalaj cameră față de jucător. Y negativ (-1.5) arată mai mult teren dedesubt")]
    public Vector3 cameraOffset = new Vector3(0f, -1.5f, -10f);
    public float cameraSmoothSpeed = 5f;
    [Tooltip("Permite zoom din rotița mouse-ului în timpul jocului")]
    public bool allowMouseZoom = true;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 25f;

    [Header("Interfață (HUD)")]
    public bool showHUD = true;
    public float surfaceY = 0f;

    // Referințe interne
    private Camera mainCamera;
    private BoxCollider2D playerCollider;

    // Texturi și stiluri pentru OnGUI HUD scalabil
    private Texture2D panelBgTexture;
    private Texture2D barBgTexture;
    private Texture2D fuelFillTexture;
    private GUIStyle labelStyle;
    private GUIStyle depthStyle;
    private float lastCalculatedScale = -1f;

    private void Awake()
    {
        // Inițializăm lista de minerale cu datele actualizate (adâncimi în metri, Unobtanium, Ruby)
        if (minerals == null || minerals.Count == 0)
        {
            minerals = new List<MineralData>()
            {
                new MineralData { name = "Ironium", baseValue = 30, weight = 10f, commonDepthMeters = -8f, digFuelCost = 2f },
                new MineralData { name = "Bronzium", baseValue = 60, weight = 10f, commonDepthMeters = -8f, digFuelCost = 2f },
                new MineralData { name = "Silverium", baseValue = 100, weight = 10f, commonDepthMeters = -8f, digFuelCost = 3f },
                new MineralData { name = "Goldium", baseValue = 250, weight = 20f, commonDepthMeters = -75f, digFuelCost = 4f },
                new MineralData { name = "Platinum", baseValue = 750, weight = 30f, commonDepthMeters = -500f, digFuelCost = 5f },
                new MineralData { name = "Unobtanium", baseValue = 2000, weight = 40f, commonDepthMeters = -800f, digFuelCost = 6f },
                new MineralData { name = "Emerald", baseValue = 5000, weight = 60f, commonDepthMeters = -1200f, digFuelCost = 8f },
                new MineralData { name = "Ruby", baseValue = 20000, weight = 80f, commonDepthMeters = -1500f, digFuelCost = 10f },
                new MineralData { name = "Diamond", baseValue = 100000, weight = 100f, commonDepthMeters = -1750f, digFuelCost = 12f },
                new MineralData { name = "Amazonite", baseValue = 500000, weight = 120f, commonDepthMeters = -1900f, digFuelCost = 15f }
            };
        }
    }

    void Start()
    {
        currentFuel = maxFuel;

        // 1. Detectăm automat Tilemap-ul dacă nu este asociat în Inspector
        if (groundTilemap == null)
        {
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj != null)
            {
                groundTilemap = groundObj.GetComponent<Tilemap>();
            }
            if (groundTilemap == null)
            {
                groundTilemap = FindAnyObjectByType<Tilemap>();
            }
        }

        // 2. Asigurăm pornirea automată a generatorului de lume procedurală
        if (FindAnyObjectByType<WorldGenerator>() == null)
        {
            GameObject genObj = new GameObject("WorldGenerator");
            WorldGenerator wg = genObj.AddComponent<WorldGenerator>();
            wg.groundTilemap = groundTilemap;
            wg.surfaceY = surfaceY;
        }

        // 3. Setăm collider-ul puțin mai îngust (0.85f lățime)
        playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider != null)
        {
            playerCollider.size = new Vector2(0.85f, 0.95f);
        }

        // 4. Conectăm automat camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>();
            }
        }

        if (mainCamera != null && mainCamera.orthographic && Screen.height > Screen.width && mainCamera.orthographicSize <= 5.5f)
        {
            mainCamera.orthographicSize = 9f;
        }
    }

    void Update()
    {
        // Consumăm combustibil constant (chiar și când jucătorul stă pe loc)
        if (currentFuel > 0)
        {
            currentFuel -= fuelConsumptionPerSecond * Time.deltaTime;
            if (currentFuel < 0) currentFuel = 0;
        }
        else
        {
            return; // Oprește mișcarea dacă nu mai e combustibil
        }

        // Preluăm comenzile de la tastatură (Săgeți sau WASD)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Săpare continuă și receptivă la apăsarea tastelor (Jos, Stânga, Dreapta)
        TryDigDirect(moveX, moveY);

        // Mișcăm jucătorul
        Vector3 movement = new Vector3(moveX, moveY, 0f).normalized;
        transform.Translate(movement * speed * Time.deltaTime);

        // Limităm jucătorul în interiorul lumii de 100m și limită de înălțime la suprafață
        float clampedX = Mathf.Clamp(transform.position.x, -49.2f, 49.2f);
        float clampedY = Mathf.Min(transform.position.y, surfaceY + 5.0f); // Nu zboară la infinit în cer
        if (!Mathf.Approximately(clampedX, transform.position.x) || !Mathf.Approximately(clampedY, transform.position.y))
        {
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }
    }

    private float nextDigTime = 0f;
    [Tooltip("Intervalul minim între două săpături consecutive în secunde")]
    public float digCooldown = 0.08f;

    /// <summary>
    /// Săpare directă și instantanee la comanda jucătorului
    /// </summary>
    private void TryDigDirect(float moveX, float moveY)
    {
        if (groundTilemap == null || currentFuel <= 0 || Time.time < nextDigTime) return;

        Bounds bounds = playerCollider != null ? playerCollider.bounds : new Bounds(transform.position, Vector3.one);

        // 1. SĂPARE ÎN JOS (prioritate când se apasă Săgeată Jos sau S)
        if (moveY < -0.1f)
        {
            Vector3 probePos = new Vector3(bounds.center.x, bounds.min.y - 0.2f, 0f);
            Vector3Int cell = groundTilemap.WorldToCell(probePos);

            if (groundTilemap.HasTile(cell))
            {
                TileBase hitTile = groundTilemap.GetTile(cell);
                string tileName = hitTile != null ? hitTile.name : "";

                if (tileName.Equals("Boulder", System.StringComparison.OrdinalIgnoreCase))
                {
                    return; // Boulder e indestructibil la burghiu
                }

                if (tileName.Equals("Lava", System.StringComparison.OrdinalIgnoreCase))
                {
                    currentFuel -= 15f;
                    if (currentFuel < 0) currentFuel = 0;
                    nextDigTime = Time.time + 0.4f;
                    return;
                }

                // Centrăm jucătorul exact pe coloana blocului pentru o coborâre perfect verticală
                Vector3 colCenter = groundTilemap.GetCellCenterWorld(cell);
                transform.position = new Vector3(colCenter.x, transform.position.y, transform.position.z);

                DigTileAt(cell, hitTile, tileName);
                nextDigTime = Time.time + digCooldown;
                return;
            }
        }
        // 2. SĂPARE LATERALĂ (Stânga sau Dreapta)
        else if (Mathf.Abs(moveX) > 0.1f)
        {
            float probeX = moveX > 0 ? (bounds.max.x + 0.2f) : (bounds.min.x - 0.2f);
            Vector3 probePos = new Vector3(probeX, bounds.center.y, 0f);
            Vector3Int cell = groundTilemap.WorldToCell(probePos);

            if (groundTilemap.HasTile(cell))
            {
                TileBase hitTile = groundTilemap.GetTile(cell);
                string tileName = hitTile != null ? hitTile.name : "";

                if (tileName.Equals("Boulder", System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (tileName.Equals("Lava", System.StringComparison.OrdinalIgnoreCase))
                {
                    currentFuel -= 15f;
                    if (currentFuel < 0) currentFuel = 0;
                    nextDigTime = Time.time + 0.4f;
                    return;
                }

                DigTileAt(cell, hitTile, tileName);
                nextDigTime = Time.time + digCooldown;
                return;
            }
        }
    }

    void LateUpdate()
    {
        // Urmărim automat jucătorul cu camera
        if (followCamera && mainCamera != null)
        {
            Vector3 targetCamPos = new Vector3(
                transform.position.x + cameraOffset.x,
                transform.position.y + cameraOffset.y,
                cameraOffset.z
            );

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetCamPos,
                cameraSmoothSpeed * Time.deltaTime
            );

            // Zoom din rotița mouse-ului (ScrollWheel)
            if (allowMouseZoom && mainCamera.orthographic)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    mainCamera.orthographicSize = Mathf.Clamp(
                        mainCamera.orthographicSize - (scroll * zoomSpeed * 2f),
                        minZoom,
                        maxZoom
                    );
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDigDirectional(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDigDirectional(collision);
    }

    /// <summary>
    /// Săpare declanșată și de coliziune fizică (fallback)
    /// </summary>
    private void TryDigDirectional(Collision2D collision)
    {
        if (groundTilemap == null || currentFuel <= 0) return;

        // Verificăm dacă obiectul lovit este Tilemap-ul terenului
        if (collision.gameObject != groundTilemap.gameObject &&
            collision.collider.gameObject != groundTilemap.gameObject &&
            collision.collider.GetComponentInParent<Tilemap>() != groundTilemap)
            return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        TryDigDirect(moveX, moveY);
    }

    private void DigTileAt(Vector3Int tilePosition, TileBase hitTile, string tileName)
    {
        if (hitTile == null) return;

        // Căutăm dacă tile-ul este un mineral din lista noastră
        MineralData mineral = null;
        if (minerals != null)
        {
            mineral = minerals.Find(m =>
                (m.tile != null && m.tile == hitTile) ||
                (!string.IsNullOrEmpty(tileName) && tileName.Equals(m.name, System.StringComparison.OrdinalIgnoreCase))
            );
        }

        float currentDepth = surfaceY - transform.position.y;

        if (mineral != null)
        {
            money += mineral.baseValue;
            cargoWeight += mineral.weight;
            currentFuel -= mineral.digFuelCost;
            Debug.Log($"💎 [MINERAL SĂPAT] {mineral.name}! +${mineral.baseValue} (Total: ${money}) | +{mineral.weight}kg | Adâncime: {Mathf.RoundToInt(currentDepth)} m | Fuel rămas: {Mathf.Round(currentFuel)}");
        }
        else
        {
            currentFuel -= digFuelCost;
            Debug.Log($"⛏️ [PĂMÂNT SĂPAT] {tileName} (-{digFuelCost} fuel) | Adâncime: {Mathf.RoundToInt(currentDepth)} m | Fuel: {Mathf.Round(currentFuel)}");
        }

        if (currentFuel < 0) currentFuel = 0;

        // Ștergem Tile-ul de pe Tilemap
        groundTilemap.SetTile(tilePosition, null);
    }

    /// <summary>
    /// Afișează HUD-ul în stânga sus, scalat automat indiferent de rezoluție (inclusiv 1440x3120 S24 Ultra)
    /// </summary>
    private void OnGUI()
    {
        if (!showHUD) return;

        float minDimension = Mathf.Min(Screen.width, Screen.height);
        float scale = Mathf.Max(0.7f, minDimension / 720f);

        InitGUI(scale);

        float panelWidth = Mathf.Min(Screen.width * 0.85f, 320f * scale);
        float panelHeight = 140f * scale;

        float panelX = 20f * scale;
        float panelY = 25f * scale;

        // 1. Panou de fundal semi-transparent
        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), panelBgTexture);

        float padding = 16f * scale;
        float contentWidth = panelWidth - (padding * 2f);
        float currentY = panelY + (12f * scale);

        // 2. Text Combustibil
        float fuelRatio = Mathf.Clamp01(currentFuel / maxFuel);
        GUI.Label(new Rect(panelX + padding, currentY, contentWidth, 24f * scale),
                  $"Combustibil: {Mathf.CeilToInt(currentFuel)} / {Mathf.CeilToInt(maxFuel)}", labelStyle);

        // 3. Bara de Combustibil
        currentY += 26f * scale;
        float barHeight = 20f * scale;
        GUI.DrawTexture(new Rect(panelX + padding, currentY, contentWidth, barHeight), barBgTexture);

        Color fillColor = fuelRatio > 0.5f ? new Color(0.2f, 0.85f, 0.3f) :
                         (fuelRatio > 0.2f ? new Color(0.95f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f));

        Color oldColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(panelX + padding, currentY, contentWidth * fuelRatio, barHeight), fuelFillTexture);
        GUI.color = oldColor;

        // 4. Adâncimea curentă în metri (evidențiată sub bara de combustibil)
        currentY += barHeight + (8f * scale);
        float depthMeters = surfaceY - transform.position.y;
        int depthInt = Mathf.RoundToInt(depthMeters);
        string depthDisplay = depthInt >= 0 ? $"Adâncime: {depthInt} m" : $"Altitudine: +{Mathf.Abs(depthInt)} m";

        GUI.Label(new Rect(panelX + padding, currentY, contentWidth, 30f * scale), depthDisplay, depthStyle);

        // 5. Bani și Încărcătură (Cargo)
        currentY += 30f * scale;
        GUI.Label(new Rect(panelX + padding, currentY, contentWidth, 22f * scale),
                  $"Bani: ${money}   |   Cargo: {cargoWeight:F0}/{maxCargoWeight:F0} kg", labelStyle);
    }

    private void InitGUI(float scale)
    {
        if (labelStyle == null || depthStyle == null || !Mathf.Approximately(lastCalculatedScale, scale))
        {
            lastCalculatedScale = scale;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            depthStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1f) }
            };
        }

        if (panelBgTexture == null)
            panelBgTexture = CreateSolidTexture(new Color(0.08f, 0.08f, 0.1f, 0.88f));
        if (barBgTexture == null)
            barBgTexture = CreateSolidTexture(new Color(0.22f, 0.22f, 0.25f, 0.95f));
        if (fuelFillTexture == null)
            fuelFillTexture = CreateSolidTexture(Color.white);
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (panelBgTexture != null) Destroy(panelBgTexture);
        if (barBgTexture != null) Destroy(barBgTexture);
        if (fuelFillTexture != null) Destroy(fuelFillTexture);
    }
}
