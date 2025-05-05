using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using System.Net.Sockets;

//This is the updated obstacle detection script with raycast for detection
//by JL April 25 2025
public class Obs_Det_GUI : MonoBehaviour
{
    // OBS_DETECTION
    public Transform player; // Player transform
    public float farDetectionRadius = 5f; // Far detection radius
    public float nearDetectionRadius = 1f; // Near detection radius
    public LayerMask obstacleLayer; // Layer for all obstacles
    public int raysPerSector = 5; // Number of rays per sector for precision
    private int[] sectorStates = new int[6]; // Stores the state of each sector (0: green, 1: yellow, 2: red, 3: orange, 4: purple)
    
    // New variable for HaloTrafficLight reference
    public HaloTrafficLight trafficLight; // Reference to the HaloTrafficLight instance

    // Offset for lower position
    public float playerBottomOffset = 0.85f; // Offset below the player's position
    private Vector3 playerBottom; // Position slightly below the player's current position
    public float verticalStep = 1f; // Step size for vertical height detection
    public int StepThreshold = 1; // Number of steps to check for vertical height

    // GUI
    public Vector2 center = new Vector2(790, 70); // Center of the GUI

    // TCP Client
    public TCP_Client_X tcpClient; // Reference to the TCP_Client_X script

    void Start()
    {
        // Initialize sector states
        for (int i = 0; i < 6; i++)
        {
            sectorStates[i] = 0; // Default to green
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
        Debug.Log($"{sectorStates[0]}{sectorStates[1]}{sectorStates[2]}{sectorStates[3]}{sectorStates[4]}{sectorStates[5]}");
        // Reset sector states
        for (int i = 0; i < 6; i++)
        {
            sectorStates[i] = 0; // Default to green
        }

        Vector3 forward = player.forward;

        // Cast multiple rays in each sector
        for (int i = 0; i < 6; i++)
        {
            float sectorStartAngle = i * 60f - 30f;
            float sectorEndAngle = (i + 1) * 60f - 30f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;
            int temp = 0; //A temporary variable to store the sector state

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
                    if (newState > temp)
                    {
                        temp = newState;
                    }

                    //Debug.Log($"Near Ray hit {nearHit.collider.name} in sector {i}, Vertical Steps: {verticalSteps}, Distance: {nearHit.distance}");
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
                    if (newState > temp)
                    {
                        temp = newState;
                    }

                    //Debug.Log($"Far Ray hit {farHit.collider.name} in sector {i}, Vertical Steps: {verticalSteps}, Distance: {farHit.distance}");
                    break; // Skip further rays in this sector
                }
            }

            sectorStates[i] = temp;
        }
    }

    // ...existing code...

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
                sectorStates[sectorIndex] = 4; // Purple for the specific sector
            }
        }
    }

    // Draw Gizmos for visualization
    void OnDrawGizmos()
    {
        if (player == null) return;

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
                switch (sectorStates[i])
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
        float radius = 50f;
        float hexRadius = 70f;

        Vector2[] positions = new Vector2[7];

        positions[0] = center + new Vector2(0, -hexRadius);
        positions[1] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[2] = center + new Vector2(hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[3] = center + new Vector2(0, hexRadius);
        positions[4] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[5] = center + new Vector2(-hexRadius * Mathf.Cos(Mathf.PI / 6), -hexRadius * Mathf.Sin(Mathf.PI / 6));
        positions[6] = center;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 9;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 6; i++)
        {
            Rect rect = new Rect(positions[i].x, positions[i].y, radius, radius);

            // Determine the color based on sector state
            switch (sectorStates[i])
            {
                case 4: // Purple
                    buttonStyle.normal.background = MakeTex(2, 2, new Color(0.5f, 0, 0.5f));
                    GUI.Button(rect, "Near High", buttonStyle);
                    break;
                case 3: // Orange
                    buttonStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.5f, 0));
                    GUI.Button(rect, "Near Low", buttonStyle);
                    break;
                case 2: // Red
                    buttonStyle.normal.background = MakeTex(2, 2, Color.red);
                    GUI.Button(rect, "Far High", buttonStyle);
                    break;
                case 1: // Yellow
                    buttonStyle.normal.background = MakeTex(2, 2, Color.yellow);
                    GUI.Button(rect, "Far Low", buttonStyle);
                    break;
                default: // Green
                    buttonStyle.normal.background = MakeTex(2, 2, Color.green);
                    GUI.Button(rect, "Clear", buttonStyle);
                    break;
            }
        }
    }

    // Helper function to create a texture for GUI buttons
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    // Send sectorStates via TCP
    void SendSectorStates()
    {
        if (tcpClient != null && tcpClient.Client_Socket != null && tcpClient.Client_Socket.Connected)
        {
            // Convert sectorStates to a comma-separated string
            string message = string.Join(",", sectorStates);

            // Send the message
            tcpClient.inputMes = message;
            tcpClient.SendFlag = true; // Trigger the send flag
        }
    }
}
