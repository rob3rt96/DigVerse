using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelConsumptionPerSecond = 2f;

    void Start()
    {
        // Inițializăm combustibilul la maxim la începutul jocului
        currentFuel = maxFuel;
    }

    void Update()
    {
        // Consumăm combustibil constant
        if (currentFuel > 0)
        {
            currentFuel -= fuelConsumptionPerSecond * Time.deltaTime;
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
}