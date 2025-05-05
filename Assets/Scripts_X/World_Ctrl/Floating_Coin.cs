using UnityEngine;

public class Floating_Coin : MonoBehaviour
{
    public float floatSpeed = 1f; // Speed of floating up and down
    public float floatHeight = 0.5f; // Height of floating
    public float rotationSpeed = 50f; // Speed of rotation
    public GameObject targetObject; // The object to disappear when the coin is collected
    public Transform player; // Reference to the player's transform
    public float triggerDistance = 1.5f; // Distance threshold to trigger the action

    private Vector3 startPosition; // Initial position of the coin

    void Start()
    {
        // Store the initial position of the coin
        startPosition = transform.position;
    }

    void Update()
    {
        // Floating effect
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Rotation effect
        transform.Rotate(Vector3.left, rotationSpeed * Time.deltaTime);

        // Check distance to the player
        if (player != null && Vector3.Distance(transform.position, player.position) <= triggerDistance)
        {
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        // Toggle the active state of the target object
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }

        // Destroy the coin itself
        Destroy(gameObject);
    }

}

