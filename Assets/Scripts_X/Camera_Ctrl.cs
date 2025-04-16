using UnityEngine;

// by JL April 15 2025 
public class Camera_Ctrl : MonoBehaviour
{
    public Transform Player; // Reference to the player's body
    private float mouseX, mouseY;
    public float mouseSensitivity = 100f;

    public float xRotation;

    private void Update()
    {
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY; // Invert the mouse Y axis
        xRotation = Mathf.Clamp(xRotation, -70f, 70f); // Clamp the rotation to prevent flipping

        Player.Rotate(Vector3.up * mouseX); // Rotate the player body around the Y-axis
        //transform.Rotate(Vector3.left * mouseY); // Rotate the camera around the X-axis
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Rotate the camera around the X-axis
    }

}
