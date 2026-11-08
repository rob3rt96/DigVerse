using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    [Header("Mișcare și Combustibil")]
    public float speed = 5f;
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelConsumptionPerSecond = 2f;

    [Header("Săpare Teren")]
    public Tilemap groundTilemap;
    public float digFuelCost = 5f;

    void Start()
    {
        // Inițializăm combustibilul la maxim la începutul jocului
        currentFuel = maxFuel;

        // Dacă nu a fost asociat manual în Inspector, căutăm automat GameObject-ul "Ground"
        if (groundTilemap == null)
        {
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj != null)
            {
                groundTilemap = groundObj.GetComponent<Tilemap>();
            }

            // Fallback: căutăm orice Tilemap existent în scenă
            if (groundTilemap == null)
            {
                groundTilemap = FindAnyObjectByType<Tilemap>();
            }

            if (groundTilemap != null)
            {
                Debug.Log("Tilemap detectat automat: " + groundTilemap.gameObject.name);
            }
            else
            {
                Debug.LogWarning("Atenție: Nu s-a găsit niciun Tilemap pe GameObject-ul 'Ground' sau în scenă!");
            }
        }
    }

    void Update()
    {
        // Consumăm combustibil constant
        if (currentFuel > 0)
        {
            currentFuel -= fuelConsumptionPerSecond * Time.deltaTime;
            if (currentFuel < 0) currentFuel = 0;
        }
        else
        {
            Debug.Log("Fără combustibil! Game Over.");
            return; // Oprește mișcarea dacă nu mai e combustibil
        }

        // Preluăm comenzile de la tastatură (Săgeți sau WASD)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Mișcăm jucătorul
        Vector3 movement = new Vector3(moveX, moveY, 0f).normalized;
        transform.Translate(movement * speed * Time.deltaTime);

        // Afișăm nivelul de combustibil în consolă (temporar, până facem interfața grafică)
        Debug.Log("Combustibil: " + Mathf.Round(currentFuel));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDig(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDig(collision);
    }

    private void TryDig(Collision2D collision)
    {
        // Verificăm dacă avem referință către Tilemap și dacă jucătorul mai are combustibil
        if (groundTilemap == null || currentFuel <= 0) return;

        // Verificăm dacă obiectul lovit este Tilemap-ul terenului
        if (collision.gameObject != groundTilemap.gameObject &&
            collision.collider.gameObject != groundTilemap.gameObject &&
            collision.collider.GetComponentInParent<Tilemap>() != groundTilemap)
            return;

        foreach (ContactPoint2D hit in collision.contacts)
        {
            // Direcția normalei punctului de contact:
            // hit.normal indică dinspre suprafața blocului spre jucător.
            // Dacă normal.y < -0.5f, lovim partea de jos a unui bloc situat deasupra (sus).
            // Permitem săparea doar dacă blocul este jos, stânga sau dreapta (nu și sus).
            if (hit.normal.y < -0.5f)
            {
                continue; // Ignorăm coliziunile de deasupra (nu săpăm în sus)
            }

            // Calculăm o poziție aflată ușor în interiorul blocului lovit (scăzând un mic offset de-a lungul normalei)
            Vector3 hitPosition = (Vector3)hit.point - (Vector3)(hit.normal * 0.01f);

            // Convertim poziția World în coordonate de celulă (Tile) pe grilă
            Vector3Int tilePosition = groundTilemap.WorldToCell(hitPosition);

            // Verificăm dacă există un bloc la acele coordonate
            if (groundTilemap.HasTile(tilePosition))
            {
                // Ștergem Tile-ul de pe Tilemap
                groundTilemap.SetTile(tilePosition, null);

                // Scădem 5 unități adiționale de combustibil
                currentFuel -= digFuelCost;
                if (currentFuel < 0) currentFuel = 0;

                Debug.Log($"Bloc săpat la coordonatele: {tilePosition} | Combustibil rămas: {Mathf.Round(currentFuel)}");
                break; // Procesăm un bloc per coliziune
            }
        }
    }
}
