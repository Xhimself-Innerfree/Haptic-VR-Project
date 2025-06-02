using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using System.Net.Sockets;

//by JL Jun 1 2025
public class Active_Mtd : MonoBehaviour
{
    // OBS_DETECTION
    public Transform player; // Player transform
    public float farDetectionRadius = 5f; // Far detection radius
    public float nearDetectionRadius = 1f; // Near detection radius
    public LayerMask obstacleLayer; // Layer for all obstacles
    public int raysPerSector = 5; // Number of rays per sector for precision

    public float CarRadius = 3f; // Detection radius for cars
    public LayerMask carLayer;   // LayerMask for car objects

    // Define the updated struct
    public struct Obs_Sector_State
    {
        public float currentDistance; // Distance to the detected object
        public float minDistance; // Minimum distance detected
        public float preDistance; // Previous distance to the detected object
        public float timer; // Timer for when minDistance < currentDistance
        public int realState; // Actual state
        public float resetTimer; // Timer for resetting minDistance
    }

    // Replace sectorStates with an array of updated Obs_Sector_State
    private Obs_Sector_State[] sectorStates = new Obs_Sector_State[6];

    // New variable for HaloTrafficLight reference
    public HaloTrafficLight trafficLight; // Reference to the HaloTrafficLight instance

    // Offset for lower position
    public float playerBottomOffset = 0.85f; // Offset below the player's position
    private Vector3 playerBottom; // Position slightly below the player's current position
    public float verticalStep = 1f; // Step size for vertical height detection
    public int StepThreshold = 1; // Number of steps to check for vertical height

    //OnDrawGizmos
    public bool enableGizmos = true; // Enable or disable Gizmos for visualization

    // GUI
    public Vector2 center = new Vector2(900, 70); // Center of the GUI

    // TCP Client
    public TCP_Client_X tcpClient; // Reference to the TCP_Client_X script

    // Add timer and currentPanel for cycling GUI highlight
    private float panelTimer = 0f;
    private int currentPanel = 0;

    // For active navigation: per-sector, per-ray hit results
    private bool[][] sectorRayHits;
    private float panelCycleDuration = 1.0f; // 1 second per panel
    private int currentRayIndex = 0;
    private float rayTimer = 0f;

    void Start()
    {
        // Initialize sector states
        for (int i = 0; i < 6; i++)
        {
            sectorStates[i] = new Obs_Sector_State
            {
                currentDistance = 6f, // Default to a value greater than detection range
                minDistance = 6f, // Default to a value greater than detection range
                timer = 0f,
                realState = 0, // Default to green
                resetTimer = 0f,
                preDistance = 6f // Default to a value greater than detection range
            };
        }

        // Initialize per-sector, per-ray hit array
        sectorRayHits = new bool[6][];
        for (int i = 0; i < 6; i++)
        {
            sectorRayHits[i] = new bool[raysPerSector];
        }
    }

    void Update()
    {
        // Update the bottom of the player
        playerBottom = player.position - new Vector3(0, playerBottomOffset, 0);

        // Update per-sector, per-ray hit info for GUI
        UpdateSectorRayHits();

        // Perform obstacle detection
        DetectObstaclesWithRayCast();

        // Check traffic light state and update GUI (now before sending sector states)
        CheckTrafficLightState();

        // Send sectorStates via TCP
        SendSectorStates();

        // --- Panel cycling logic for GUI ---
        panelTimer += Time.deltaTime;
        rayTimer += Time.deltaTime;

        float raySliceDuration = panelCycleDuration / raysPerSector;

        // Determine which ray is active in this time slice
        int rayIdx = Mathf.FloorToInt(rayTimer / raySliceDuration);
        if (rayIdx != currentRayIndex && rayIdx < raysPerSector)
        {
            currentRayIndex = rayIdx;
        }

        if (panelTimer >= panelCycleDuration)
        {
            panelTimer = 0f;
            rayTimer = 0f;
            currentRayIndex = 0;
            currentPanel = (currentPanel + 1) % 6;
        }
    }

    // Update per-sector, per-ray hit info for GUI
    void UpdateSectorRayHits()
    {
        Vector3 forward = player.forward;
        for (int i = 0; i < 6; i++)
        {
            float sectorStartAngle = i * 60f - 30f;
            float sectorEndAngle = (i + 1) * 60f - 30f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

                bool hit = false;
                // Near radius check
                if (Physics.Raycast(playerBottom, rayDirection, nearDetectionRadius, obstacleLayer))
                {
                    hit = true;
                }
                // Far radius check
                else if (Physics.Raycast(playerBottom, rayDirection, farDetectionRadius, obstacleLayer))
                {
                    hit = true;
                }
                // Car detection logic
                else
                {
                    Collider[] cars = Physics.OverlapSphere(playerBottom, CarRadius, carLayer);
                    foreach (var car in cars)
                    {
                        Vector3 dirToCar = (car.transform.position - playerBottom);
                        float distToCar = dirToCar.magnitude;
                        if (distToCar < CarRadius)
                        {
                            float angleToCar = Vector3.SignedAngle(forward, dirToCar, Vector3.up);
                            float normAngleToCar = (angleToCar + 360f) % 360f;
                            float normSectorStart = (sectorStartAngle + 360f) % 360f;
                            float normSectorEnd = (sectorEndAngle + 360f) % 360f;

                            bool inSector = false;
                            if (normSectorStart < normSectorEnd)
                                inSector = normAngleToCar >= normSectorStart && normAngleToCar < normSectorEnd;
                            else
                                inSector = normAngleToCar >= normSectorStart || normAngleToCar < normSectorEnd;

                            // For this ray, check if the car is within the ray's angular slice
                            float rayStart = (sectorStartAngle + j * angleStep + 360f) % 360f;
                            float rayEnd = (sectorStartAngle + (j + 1) * angleStep + 360f) % 360f;
                            bool inRay = false;
                            if (rayStart < rayEnd)
                                inRay = normAngleToCar >= rayStart && normAngleToCar < rayEnd;
                            else
                                inRay = normAngleToCar >= rayStart || normAngleToCar < rayEnd;

                            if (inSector && inRay)
                            {
                                hit = true;
                                break;
                            }
                        }
                    }
                }
                sectorRayHits[i][j] = hit;
            }
        }
    }

    // Detect obstacles using multiple RayCasts per sector
    void DetectObstaclesWithRayCast()
    {
        Vector3 forward = player.forward;

        for (int i = 0; i < 6; i++)
        {
            float sectorStartAngle = i * 60f - 30f;
            float sectorEndAngle = (i + 1) * 60f - 30f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;
            int tempState = 0;
            float closestDistance = float.MaxValue;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

                // Near radius check
                if (Physics.Raycast(playerBottom, rayDirection, out RaycastHit nearHit, nearDetectionRadius, obstacleLayer))
                {
                    int verticalSteps = 0;
                    Vector3 verticalStart = playerBottom;
                    while (verticalSteps < StepThreshold)
                    {
                        verticalStart += Vector3.up * verticalStep;
                        if (Physics.Raycast(verticalStart, rayDirection, nearDetectionRadius, obstacleLayer))
                        {
                            verticalSteps++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    int newState = (verticalSteps < StepThreshold) ? 3 : 4;
                    if (newState > tempState)
                        tempState = newState;
                    closestDistance = Mathf.Min(closestDistance, nearHit.distance);
                    break;
                }
                // Far radius check
                if (Physics.Raycast(playerBottom, rayDirection, out RaycastHit farHit, farDetectionRadius, obstacleLayer))
                {
                    int verticalSteps = 0;
                    Vector3 verticalStart = playerBottom;
                    while (verticalSteps < StepThreshold)
                    {
                        verticalStart += Vector3.up * verticalStep;
                        if (Physics.Raycast(verticalStart, rayDirection, farDetectionRadius, obstacleLayer))
                        {
                            verticalSteps++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    int newState = (verticalSteps < StepThreshold) ? 1 : 2;
                    if (newState > tempState)
                        tempState = newState;
                    closestDistance = Mathf.Min(closestDistance, farHit.distance);
                    break;
                }
            }

            // --- Car detection logic (fixed angle normalization) ---
            Collider[] cars = Physics.OverlapSphere(playerBottom, CarRadius, carLayer);
            foreach (var car in cars)
            {
                Vector3 dirToCar = (car.transform.position - playerBottom);
                float distToCar = dirToCar.magnitude;
                if (distToCar < CarRadius)
                {
                    // Calculate angle between player's forward and direction to car
                    float angleToCar = Vector3.SignedAngle(forward, dirToCar, Vector3.up);

                    // Normalize angles to [0, 360)
                    float normAngleToCar = (angleToCar + 360f) % 360f;
                    float normSectorStart = (sectorStartAngle + 360f) % 360f;
                    float normSectorEnd = (sectorEndAngle + 360f) % 360f;

                    bool inSector = false;
                    if (normSectorStart < normSectorEnd)
                    {
                        inSector = normAngleToCar >= normSectorStart && normAngleToCar < normSectorEnd;
                    }
                    else
                    {
                        // Sector wraps around 360
                        inSector = normAngleToCar >= normSectorStart || normAngleToCar < normSectorEnd;
                    }

                    if (inSector)
                    {
                        // Set to purple (4) and break, highest priority
                        tempState = 4;
                        closestDistance = Mathf.Min(closestDistance, distToCar);
                        break;
                    }
                }
            }

            // Store previous distance
            sectorStates[i].preDistance = sectorStates[i].currentDistance;
            sectorStates[i].currentDistance = closestDistance;
            sectorStates[i].realState = tempState;
        }
    }

    // Enhanced: Traffic light detection and display logic
    void CheckTrafficLightState()
    {
        if (trafficLight != null && trafficLight.GetState() == HaloTrafficLight.LightState.Red)
        {
            Vector3 directionToPlayer = player.position - trafficLight.transform.position;
            float angle = Vector3.Angle(trafficLight.transform.forward, directionToPlayer);

            // Check if the player is in front of the traffic light (within a 90-degree cone)
            if (angle < 45f)
            {
                // Calculate the sector index based on the direction
                Vector3 forward = player.forward;
                float sectorAngle = Vector3.SignedAngle(forward, directionToPlayer, Vector3.up);
                int sectorIndex = Mathf.FloorToInt((sectorAngle + 180f) / 60f) % 6;

                // 直接覆盖realState为4（紫色），不再处理showState
                sectorStates[sectorIndex].realState = 4;
            }
        }
    }

    // Draw Gizmos for visualization
    void OnDrawGizmos()
    {
        if (player == null) return;
        if (!enableGizmos) return;

        // Draw far detection radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), farDetectionRadius);

        // Draw near detection radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), nearDetectionRadius);

        Vector3 forward = player.forward;

        // Draw rays for each sector
        for (int i = 0; i < 6; i++)
        {
            float sectorStartAngle = i * 60f - 30f;
            float sectorEndAngle = (i + 1) * 60f - 30f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

                // Set color based on sector state
                switch (sectorStates[i].realState)
                {
                    case 4: // Purple for near High obstacles
                        Gizmos.color = new Color(0.5f, 0, 0.5f);
                        break;
                    case 3: // Orange for near Low obstacles
                        Gizmos.color = new Color(1f, 0.5f, 0);
                        break;
                    case 2: // Red for far High obstacles
                        Gizmos.color = Color.red;
                        break;
                    case 1: // Yellow for far Low obstacles
                        Gizmos.color = Color.yellow;
                        break;
                    default: // Green for no obstacles
                        Gizmos.color = Color.green;
                        break;
                }

                // Draw the ray
                Gizmos.DrawRay(player.position - new Vector3(0, playerBottomOffset, 0), rayDirection * farDetectionRadius);
            }

            // Draw the sector lines
            // Draw dividing line between sectors
            Gizmos.color = Color.black;
            Vector3 DivideDirection = Quaternion.Euler(0, sectorStartAngle, 0) * forward;
            Gizmos.DrawRay(player.position - new Vector3(0, playerBottomOffset, 0), DivideDirection * farDetectionRadius);
        }

        // Draw CarRadius
        Gizmos.color = new Color(0.5f, 0, 0.5f, 0.2f); // semi-transparent purple
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), CarRadius);
    }

    // Draw GUI for obstacle states (active navigation style)
    void OnGUI()
    {
        float radius = 30f; // Size of each panel
        float hexRadius = 50f; // Distance from center to each panel

        // Static hex layout for 6 panels
        Vector2[] positions = new Vector2[6];
        positions[0] = center + new Vector2(0, -hexRadius); // Top (0 deg)
        positions[1] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6)); // 60 deg
        positions[2] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6));  // 120 deg
        positions[3] = center + new Vector2(0, hexRadius);  // 180 deg
        positions[4] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6)); // 240 deg
        positions[5] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6)); // 300 deg

        // Only two active panels: currentPanel and its opposite (currentPanel + 3) % 6
        bool[] isActive = new bool[6];
        isActive[currentPanel] = true;
        isActive[(currentPanel + 3) % 6] = true;

        for (int i = 0; i < 6; i++)
        {
            Rect rect = new Rect(positions[i].x - radius / 2, positions[i].y - radius / 2, radius, radius);

            Color color = Color.gray;
            if (isActive[i])
            {
                // Show red if current ray detects obstacle, otherwise green
                int rayIdx = currentRayIndex;
                if (rayIdx < raysPerSector && sectorRayHits[i][rayIdx])
                    color = Color.red;
                else
                    color = Color.green;
            }
            // else color remains gray for separator

            GUI.DrawTexture(rect, MakeCircleTex((int)radius, color));
        }
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

    // Send sectorStates via TCP
    void SendSectorStates()
    {
        if (tcpClient != null && tcpClient.Client_Socket != null && tcpClient.Client_Socket.Connected)
        {
            // Convert sectorStates to a comma-separated string
            string message = string.Join(",", sectorStates.Select(s => s.realState));

            // Send the message
            tcpClient.inputMes = message;
            tcpClient.SendFlag = true; // Trigger the send flag
        }
    }
}
