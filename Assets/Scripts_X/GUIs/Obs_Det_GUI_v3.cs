using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using System.Net.Sockets;

//This is the updated obstacle detection script with raycast for detection
//by JL May 9 2025
public class Obs_Det_GUI_v3 : MonoBehaviour
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

        // Check traffic light state and update GUI (now before sending sector states)
        CheckTrafficLightState();

        // Send sectorStates via TCP
        SendSectorStates();
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

            int tempShowState = 0;

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

            // Store previous distance
            sectorStates[i].preDistance = sectorStates[i].currentDistance;
            sectorStates[i].currentDistance = closestDistance;
            sectorStates[i].realState = tempState;

            // --- New display logic implementation ---
            if (sectorStates[i].realState == 0)
            {
                // No obstacle detected, reset everything
                sectorStates[i].minDistance = 6f;
                sectorStates[i].timer = 0f;
                sectorStates[i].resetTimer = 0f;
                tempShowState = 0;
            }
            else
            {
                // Obstacle detected (or will be overwritten by traffic light logic)
                float prev = sectorStates[i].preDistance;
                float curr = sectorStates[i].currentDistance;
                bool isApproaching = curr < prev - 0.01f;
                bool isUnchangedOrAway = curr >= prev - 0.01f;

                if (isApproaching)
                {
                    // Distance decreasing: always show, reset timers
                    tempShowState = sectorStates[i].realState;
                    sectorStates[i].timer = 0f;
                    sectorStates[i].resetTimer = 0f;
                }
                else
                {
                    // Distance unchanged or increasing
                    sectorStates[i].timer += Time.deltaTime;

                    if (sectorStates[i].timer < 0.5f)
                    {
                        // Show for 0.5s
                        tempShowState = sectorStates[i].realState;
                        sectorStates[i].resetTimer = 0f;
                    }
                    else
                    {
                        // After 0.5s, start 2.5s off/on cycle
                        sectorStates[i].resetTimer += Time.deltaTime;
                        float cycle = sectorStates[i].resetTimer % 3.0f;
                        if (cycle < 0.5f)
                        {
                            tempShowState = sectorStates[i].realState; // Show for 0.5s
                        }
                        else
                        {
                            tempShowState = 0; // Hide for 2.5s
                        }
                    }
                }

                // Update minDistance logic if needed
                if (curr <= sectorStates[i].minDistance)
                {
                    sectorStates[i].minDistance = curr;
                }
                else
                {
                    sectorStates[i].minDistance = Mathf.Max(curr - 0.001f, nearDetectionRadius);
                }
            }

            // Assign the final value to showState
            sectorStates[i].showState = tempShowState;
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

                // --- Traffic light purple logic: override both realState and showState with purple (4) ---
                // Use the same display logic as obstacles: if already purple, keep cycling, else reset timers
                if (sectorStates[sectorIndex].realState != 4)
                {
                    // Set to purple and reset timers for new red light detection
                    sectorStates[sectorIndex].realState = 4;
                    sectorStates[sectorIndex].timer = 0f;
                    sectorStates[sectorIndex].resetTimer = 0f;
                    sectorStates[sectorIndex].showState = 4;
                }
                else
                {
                    // Already purple, apply the same display logic as obstacles
                    // (simulate as if the distance is unchanged)
                    sectorStates[sectorIndex].timer += Time.deltaTime;
                    if (sectorStates[sectorIndex].timer < 0.5f)
                    {
                        sectorStates[sectorIndex].showState = 4;
                        sectorStates[sectorIndex].resetTimer = 0f;
                    }
                    else
                    {
                        sectorStates[sectorIndex].resetTimer += Time.deltaTime;
                        float cycle = sectorStates[sectorIndex].resetTimer % 3.0f;
                        if (cycle < 0.5f)
                        {
                            sectorStates[sectorIndex].showState = 4;
                        }
                        else
                        {
                            sectorStates[sectorIndex].showState = 0;
                        }
                    }
                }
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
