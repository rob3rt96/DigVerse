using UnityEngine;

/// <summary>
/// Atașează acest script pe "Main Camera" pentru a urmări vehiculul Player-ului
/// și pentru a permite zoom din rotița mouse-ului.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Țintă (Player)")]
    [Tooltip("Obiectul urmărit de cameră. Dacă este lăsat gol, îl va căuta automat pe 'Player'.")]
    public Transform target;

    [Header("Mișcare și Urmărire")]
    [Tooltip("Viteza de netezire a urmăririi (smooth). Valori recomandate: 3 - 10")]
    public float smoothSpeed = 5f;

    [Tooltip("Decalajul camerei față de Player. Y negativ (ex: -1.5) arată mai mult teren dedesubt când sapi în jos.")]
    public Vector3 offset = new Vector3(0f, -1.5f, -10f);

    [Header("Zoom Cameră (Scroll Mouse)")]
    [Tooltip("Permite modificarea ariei vizuale prin rotirea rotiței de la mouse în modul Play.")]
    public bool allowMouseZoom = true;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 25f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // Dacă nu a fost asociat manual în Inspector, căutăm automat obiectul 'Player'
        if (target == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        // Dacă camera pornește pe rezoluție verticală (Portrait), putem mări puțin aria inițială
        if (cam != null && cam.orthographic && Screen.height > Screen.width && cam.orthographicSize <= 5f)
        {
            // Pentru ecrane înalte (cum e S24 Ultra 1440x3120), o mărime de 8-10 permite vizualizarea terenului
            cam.orthographicSize = 9f;
        }
    }

    void Update()
    {
        // Control zoom din rotița mouse-ului (ScrollWheel)
        if (allowMouseZoom && cam != null && cam.orthographic)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (scroll * zoomSpeed * 2f), minZoom, maxZoom);
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculăm poziția dorită a camerei păstrând Z-ul corespunzător (-10)
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            offset.z
        );

        // Netezim mișcarea camerei pentru o tranziție lină
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
