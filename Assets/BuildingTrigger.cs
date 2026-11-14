using UnityEngine;

public enum BuildingType
{
    FuelStation,
    MineralProcessing,
    UpgradeShop,
    JunkShop
}

public class BuildingTrigger : MonoBehaviour
{
    public BuildingType buildingType;
    public string buildingName = "Clădire";

    [TextArea(2, 4)]
    public string description = "";

    private bool playerInside = false;
    private PlayerMovement player;
    private float messageTimer = 0f;
    private string statusMessage = "";

    void Start()
    {
        // Asigurăm un collider de tip Trigger dacă nu există
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(5f, 4f);
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            playerInside = true;
            player = other.GetComponent<PlayerMovement>();
            InteractWithBuilding();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            playerInside = false;
            statusMessage = "";
        }
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
        }
    }

    public void InteractWithBuilding()
    {
        if (player == null) return;

        switch (buildingType)
        {
            case BuildingType.FuelStation:
                // Alimentare completă cu combustibil
                if (player.currentFuel < player.maxFuel)
                {
                    player.currentFuel = player.maxFuel;
                    statusMessage = "⛽ Rezervor umplut la maxim (100%)!";
                    Debug.Log($"⛽ [STATIE FUEL] Vehicul alimentat complet! Combustibil: {player.maxFuel}");
                }
                else
                {
                    statusMessage = "⛽ Rezervorul este deja plin!";
                }
                break;

            case BuildingType.MineralProcessing:
                // Vânzare minerale din cargo
                if (player.cargoWeight > 0)
                {
                    float soldWeight = player.cargoWeight;
                    player.cargoWeight = 0;
                    statusMessage = $"💰 Minerale procesate! Cargo eliberat ({soldWeight:F0} kg vândute).";
                    Debug.Log($"💰 [MINERAL PROCESSING] Ai vândut toate mineralele din portbagaj! Banii tăi: ${player.money}");
                }
                else
                {
                    statusMessage = "💰 Nu ai minerale în cargo de vândut.";
                }
                break;

            case BuildingType.UpgradeShop:
                // Magazin Piese & Upgrade-uri
                statusMessage = "🔧 [PARTS SHOP] Magazin piese: Burghiu, Rezervor, Motor.";
                Debug.Log("🔧 [PARTS SHOP] Ai intrat în magazinul de upgrade-uri!");
                break;

            case BuildingType.JunkShop:
                // Magazin Echipamente Opționale
                statusMessage = "📦 [JUNK SHOP] Echipamente opționale: TNT, C4, Kit de reparații.";
                Debug.Log("📦 [JUNK SHOP] Ai intrat la Junk Shop!");
                break;
        }

        messageTimer = 3.5f;
    }

    void OnGUI()
    {
        // Afișăm o casetă informativă deasupra clădirii când jucătorul este în dreptul ei
        if (playerInside && messageTimer > 0 && !string.IsNullOrEmpty(statusMessage))
        {
            Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3.5f) : Vector3.zero;
            if (screenPos.z > 0)
            {
                float panelW = 340f;
                float panelH = 50f;
                float px = screenPos.x - (panelW / 2f);
                float py = Screen.height - screenPos.y - panelH;

                GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none);
                GUIStyle s = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow }
                };
                GUI.Label(new Rect(px + 5, py + 5, panelW - 10, panelH - 10), statusMessage, s);
            }
        }
    }
}
