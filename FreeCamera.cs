using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [Header("Vitesse de d�placement")]
    public float moveSpeed = 10f;      // vitesse normale
    public float sprintMultiplier = 2f; // vitesse quand on appuie sur Shift

    [Header("Vitesse de rotation")]
    public float lookSpeed = 2f;       // sensibilit� de la souris

    private float yaw = 0f;            // rotation horizontale
    private float pitch = 0f;          // rotation verticale

    void Update()
    {
        // --------- Rotation avec la souris ---------
        yaw += lookSpeed * Input.GetAxis("Mouse X");  // mouvement horizontal
        pitch -= lookSpeed * Input.GetAxis("Mouse Y"); // mouvement vertical invers�
        pitch = Mathf.Clamp(pitch, -90f, 90f);       // �viter que la cam�ra fasse un flip complet
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        // --------- Mouvement avec les touches ---------
        float h = Input.GetAxis("Horizontal"); // A/D ou Q/D
        float v = Input.GetAxis("Vertical");   // W/S ou Z/S
        float upDown = 0f;                     // monter/descendre
        if (Input.GetKey(KeyCode.E)) upDown += 1f;
        if (Input.GetKey(KeyCode.Q)) upDown -= 1f;

        Vector3 move = transform.forward * v + transform.right * h + transform.up * upDown;

        // --------- Sprint avec Shift ---------
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        transform.position += move * speed * Time.deltaTime;
    }
}
