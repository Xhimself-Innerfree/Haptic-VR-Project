using UnityEngine;

public class Balance_GUI : MonoBehaviour
{
    // GUI
    public Vector2 center = new Vector2(900, 230); // Center of the GUI
    private int[] sectorStates = new int[7]; // Stores the state of each sector (0: green, 1: yellow, 2: red, 3: orange, 4: purple)

    // Ground detection
    public Transform player; // Reference to the player's transform
    public float raycastDistance = 2f; // Distance for ground detection
    public float raycastOffset = 0.5f; // Offset ahead of the player for ground detection
    private Vector3 groundNormal = Vector3.up; // Default ground normal
    private float heightDifference = 0f; // Stores the height difference for the central ball

    void Start()
    {
        // Initialize sector states
        for (int i = 0; i < 7; i++) // Updated to initialize 7 elements
        {
            sectorStates[i] = 0; // Default to green
        }
    }

    void Update()
    {
        // Detect ground and calculate balance angle
        DetectGroundAndUpdateBalance();
        
    }

    void DetectGroundAndUpdateBalance()
    {
        // Reset all sector states to green (0) at the start of each update
        for (int i = 0; i < 7; i++)
        {
            sectorStates[i] = 0; // Default to green
        }

        // Calculate the raycast origin slightly ahead of the player
        Vector3 raycastOrigin = player.position + player.forward * raycastOffset; // Use the public parameter raycastOffset

        // Perform a raycast to detect the ground ahead
        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hitAhead, raycastDistance))
        {
            groundNormal = hitAhead.normal; // Get the ground normal ahead

            // Project the ground normal onto the horizontal plane
            Vector3 projectedNormal = Vector3.ProjectOnPlane(groundNormal, Vector3.up).normalized;

            // Calculate the angle between the projected normal and the player's forward direction
            float angle = Vector3.SignedAngle(player.forward, projectedNormal, Vector3.up);

            // Determine the sector index based on the angle
            int sectorIndex = Mathf.FloorToInt((angle + 180f) / 60f) % 6;

            // Update the corresponding sector state based on the angle
            if (Mathf.Abs(angle) < 5f)
            {
                sectorStates[sectorIndex] = 0; // Green
            }
            else if (Mathf.Abs(angle) < 15f)
            {
                sectorStates[sectorIndex] = 1; // Yellow
            }
            else if (Mathf.Abs(angle) < 30f)
            {
                sectorStates[sectorIndex] = 2; // Red
            }
            else
            {
                sectorStates[sectorIndex] = 3; // Orange
            }
        }
        else
        {
            // No ground detected ahead, set all sectors to purple
            for (int i = 0; i < 7; i++) sectorStates[i] = 4; // Purple
        }
    }

    // Draw GUI for obstacle states
    void OnGUI()
    {
        float radius = 30f; // Reduced size for smaller GUI
        float hexRadius = 50f; // Proportionally smaller hex radius

        Vector2[] positions = new Vector2[7];

        positions[0] = center + new Vector2(0, -hexRadius);
        positions[1] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[2] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[3] = center + new Vector2(0, hexRadius);
        positions[4] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[5] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[6] = center;

        for (int i = 0; i < 6; i++) // Loop for outer panels
        {
            Rect rect = new Rect(positions[i].x - radius / 2, positions[i].y - radius / 2, radius, radius);

            // Determine the color based on sector state
            Color color;
            switch (sectorStates[i])
            {
                case 4: // Purple
                    color = new Color(0.5f, 0, 0.5f);
                    break;
                case 3: // Orange
                    color = new Color(1f, 0.5f, 0);
                    break;
                case 2: // Red
                    color = Color.red;
                    break;
                case 1: // Yellow
                    color = Color.yellow;
                    break;
                default: // Green
                    color = Color.green;
                    break;
            }

            // Draw the circle
            GUI.DrawTexture(rect, MakeCircleTex((int)radius, color));
        }

        // Draw the central ball
        Rect centralRect = new Rect(center.x - radius / 2, center.y - radius / 2, radius, radius); // Use the same radius as surrounding balls

        // Determine the color of the central ball based on the 7th element of sectorStates
        Color centralColor;
        switch (sectorStates[6]) // Use the 7th element
        {
            case 4: // Purple
                centralColor = new Color(0.5f, 0, 0.5f);
                break;
            case 3: // Orange
                centralColor = new Color(1f, 0.5f, 0);
                break;
            case 2: // Red
                centralColor = Color.red;
                break;
            case 1: // Yellow
                centralColor = Color.yellow;
                break;
            default: // Green
                centralColor = Color.green;
                break;
        }

        // Draw the central ball
        GUI.DrawTexture(centralRect, MakeCircleTex((int)radius, centralColor)); // Use the same radius
    }

    // Helper function to create a circular texture for GUI elements
    private Texture2D MakeCircleTex(int diameter, Color col)
    {
        Texture2D tex = new Texture2D(diameter, diameter);
        Color[] pix = new Color[diameter * diameter];
        Vector2 center = new Vector2(diameter / 2f, diameter / 2f);
        float radius = diameter / 2f;

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                Vector2 pos = new Vector2(x, y);
                if (Vector2.Distance(pos, center) <= radius)
                {
                    pix[y * diameter + x] = col;
                }
                else
                {
                    pix[y * diameter + x] = Color.clear; // Transparent outside the circle
                }
            }
        }

        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
