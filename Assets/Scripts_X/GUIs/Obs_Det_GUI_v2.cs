using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using System.Net.Sockets;

//This is the updated obstacle detection script with raycast for detection
//by JL May 8 2025
public class Obs_Det_GUI_v2 : MonoBehaviour
{
    // OBS_DETECTION
    public Transform player; // Player transform
    public float farDetectionRadius = 5f; // Far detection radius
    public float nearDetectionRadius = 1f; // Near detection radius
    public LayerMask obstacleLayer; // Layer for all obstacles
    public int raysPerSector = 5; // Number of rays per sector for precision
    
    // Define the updated struct
    public struct Obs_Sector_State
    {
        public float currentDistance; // Distance to the detected object
        public float minDistance; // Minimum distance detected
        public float preDistance; // Previous distance to the detected object
        public float timer; // Timer for when minDistance < currentDistance
        public int realState; // Actual state
        public int showState; // Displayed state
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
                showState = 0, // Default to green
                resetTimer = 0f,
                preDistance = 6f // Default to a value greater than detection range
            };
        }
    }

    void Update()
    {
        // Update the bottom of the player
        playerBottom = player.position - new Vector3(0, playerBottomOffset, 0);

        // Perform obstacle detection
        DetectObstaclesWithRayCast();

        // Check traffic light state and update GUI
        CheckTrafficLightState();

        // Send sectorStates via TCP
        SendSectorStates();
        
    }

    // Detect obstacles using multiple RayCasts per sector
    void DetectObstaclesWithRayCast()
    {
        Vector3 forward = player.forward;

        // Cast multiple rays in each sector
        for (int i = 0; i < 6; i++)
        {
            float sectorStartAngle = i * 60f - 30f;
            float sectorEndAngle = (i + 1) * 60f - 30f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;
            int tempState = 0; // Temporary variable to store the sector state
            float closestDistance = float.MaxValue; // Track the closest detected object

            // --- Add a temporary variable for showState ---
            int tempShowState = 0;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

                // Check near radius first (higher priority)
                if (Physics.Raycast(playerBottom, rayDirection, out RaycastHit nearHit, nearDetectionRadius, obstacleLayer))
                {
                    int verticalSteps = 0;
                    Vector3 verticalStart = playerBottom;

                    // Perform vertical raycasting
                    while (verticalSteps < StepThreshold)
                    {
                        verticalStart += Vector3.up * verticalStep; // Move up by verticalStep
                        if (Physics.Raycast(verticalStart, rayDirection, nearDetectionRadius, obstacleLayer))
                        {
                            verticalSteps++;
                        }
                        else
                        {
                            break; // Stop if no collision
                        }
                    }

                    // Determine obstacle type based on vertical steps
                    int newState = (verticalSteps < StepThreshold) ? 3 : 4; // 3: Orange (Low), 4: Purple (High)

                    // Update sector state only if the new state has higher priority
                    if (newState > tempState)
                    {
                        tempState = newState;
                    }

                    // Update closest distance
                    closestDistance = Mathf.Min(closestDistance, nearHit.distance);

                    break; // Skip further rays in this sector
                }

                // Check far radius if no near obstacle is detected
                if (Physics.Raycast(playerBottom, rayDirection, out RaycastHit farHit, farDetectionRadius, obstacleLayer))
                {
                    int verticalSteps = 0;
                    Vector3 verticalStart = playerBottom;

                    // Perform vertical raycasting
                    while (verticalSteps < StepThreshold)
                    {
                        verticalStart += Vector3.up * verticalStep; // Move up by verticalStep
                        if (Physics.Raycast(verticalStart, rayDirection, farDetectionRadius, obstacleLayer))
                        {
                            verticalSteps++;
                        }
                        else
                        {
                            break; // Stop if no collision
                        }
                    }

                    // Determine obstacle type based on vertical steps
                    int newState = (verticalSteps < StepThreshold) ? 1 : 2; // 1: Yellow (Low), 2: Red (High)

                    // Update sector state only if the new state has higher priority
                    if (newState > tempState)
                    {
                        tempState = newState;
                    }

                    // Update closest distance
                    closestDistance = Mathf.Min(closestDistance, farHit.distance);

                    break; // Skip further rays in this sector
                }
            }

            // Update the sector state
            sectorStates[i].realState = tempState;
            sectorStates[i].preDistance = sectorStates[i].currentDistance; // Store previous distance
            sectorStates[i].currentDistance = closestDistance;

            // --- All logic below now uses tempShowState instead of directly writing to showState ---
            if (sectorStates[i].realState == 0) // No obstacles detected
            {
                sectorStates[i].minDistance = 6f; // Reset minDistance to default
                tempShowState = 0; // Set panel to green
            }
            else
            {
                // Update currentDistance and start timer
                if (sectorStates[i].currentDistance <= sectorStates[i].minDistance)
                {
                    sectorStates[i].minDistance = sectorStates[i].currentDistance;
                    tempShowState = sectorStates[i].realState; // Update panel with obstacle state
                    sectorStates[i].timer = 0f; // Reset timer

                    // Ensure realState is assigned to showState when minDistance decreases
                    tempShowState = sectorStates[i].realState;
                }
                else
                {
                    sectorStates[i].timer += Time.deltaTime;

                    // Determine the state of distance change
                    bool isApproaching = sectorStates[i].currentDistance < sectorStates[i].preDistance - 0.01f;
                    bool isMovingAway = sectorStates[i].currentDistance > sectorStates[i].preDistance + 0.01f;
                    bool isUnchanged = Mathf.Abs(sectorStates[i].currentDistance - sectorStates[i].preDistance) <= 0.01f;

                    // Handle approaching state: continuously display the panel
                    if (isApproaching)
                    {
                        tempShowState = sectorStates[i].realState; // Update panel with obstacle state
                        sectorStates[i].timer = 0f; // Reset timer
                        Debug.Log($"Sector {i}: Approaching - continuously displaying panel.");
                    }
                    // Handle moving away state: do not display the panel
                    else if (isMovingAway)
                    {
                        tempShowState = 0; // Reset panel to green
                        Debug.Log($"Sector {i}: Moving away - hiding panel.");
                    }
                    // Handle unchanged state: display for 0.5s, then hide, and show every 3s
                    else if (isUnchanged)
                    {
                        if (sectorStates[i].timer < 0.5f)
                        {
                            tempShowState = sectorStates[i].realState; // Display panel for 0.5s
                            Debug.Log($"Sector {i}: Unchanged - displaying panel for 0.5s.");
                        }
                        else
                        {
                            tempShowState = 0; // Reset panel to green after 0.5s
                            Debug.Log($"Sector {i}: Unchanged - resetting panel to green after 0.5s.");
                            
                            // Reset the timer for the next 3-second cycle
                            if (sectorStates[i].timer >= 3.5f)
                            {
                                sectorStates[i].timer = 0f; // Reset timer after 3.5s
                                Debug.Log($"Sector {i}: Unchanged - resetting timer for next display cycle.");
                            }
                        }
                    }

                    // If currentDistance > minDistance, adjust minDistance
                    sectorStates[i].minDistance = Mathf.Max(sectorStates[i].currentDistance - 0.001f, nearDetectionRadius);

                    // If minDistance > currentDistance for 3 seconds, update panel
                    if (sectorStates[i].timer >= 3f && isUnchanged)
                    {
                        tempShowState = sectorStates[i].realState; // Remind player of obstacle
                        Debug.Log($"Sector {i}: Unchanged - displaying panel after 3 seconds.");
                    }
                }

                // After 0.5 seconds, set panel to green but retain realState
                if (sectorStates[i].timer >= 0.5f)
                {
                    tempShowState = 0; // Set panel to green
                }
            }

            // --- Only after all logic, assign the final value to showState ---
            sectorStates[i].showState = tempShowState;

            // Debugging logs to track values
            //Debug.Log($"Sector {i}: realState={sectorStates[i].realState}, showState={sectorStates[i].showState}, minDistance={sectorStates[i].minDistance}, currentDistance={sectorStates[i].currentDistance},preDistance={sectorStates[i].preDistance}£¬tim={sectorStates[i].timer},");
        }
    }

    void CheckTrafficLightState()
    {
        if (trafficLight != null && trafficLight.GetState() == HaloTrafficLight.LightState.Red) // Check if the traffic light is red
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

                // Set only the corresponding sector state to purple
                sectorStates[sectorIndex].realState = 4; // Purple for the specific sector
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
                switch (sectorStates[i].showState)
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

        for (int i = 0; i < 6; i++)
        {
            Rect rect = new Rect(positions[i].x - radius / 2, positions[i].y - radius / 2, radius, radius);

            // Determine the color based on showState
            Color color;
            switch (sectorStates[i].showState)
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
