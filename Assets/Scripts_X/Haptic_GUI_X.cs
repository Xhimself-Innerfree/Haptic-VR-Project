using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Windows;
using Unity.VisualScripting;
using System.Linq;
using static UnityEditor.VersionControl.Asset;
//by JL April 16 2025
public class Haptic_GUI_X : MonoBehaviour
{
    //              0
    //       5             1
    //              6
    //       4             2
    //              3


    // OBS_DETECTION
    public Transform player; // Player transform
    public float detectionRadius = 5f; // Detection radius
    public float collisionRadius = 1f; // Collision radius
    public LayerMask HighObstacleLayer; // Layer for High obstacles
    public LayerMask LowObstacleLayer;  // Layer for Low obstacles
    private float[] High_obstacleSectors = Enumerable.Repeat(float.MaxValue, 6).ToArray(); // Distances to High obstacles
    private float[] Low_obstacleSectors = Enumerable.Repeat(float.MaxValue, 6).ToArray();  // Distances to Low obstacles
    private int[] sectorStates = new int[6]; // Stores the state of each sector (0: green, 1: yellow, 2: red, 3: orange, 4: purple)

    // GUI
    public Vector2 center = new Vector2(790, 70); // Center of the GUI

    void Start()
    {
        // Initialize distances to a large value (indicating no obstacle detected)
        // Initialize states
        for (int i = 0; i < 6; i++)
        {
            High_obstacleSectors[i] = float.MaxValue;
            Low_obstacleSectors[i] = float.MaxValue;
            sectorStates[i] = 0; // Default to green
        }
    }


    void Update()
    {
        DetectObstacles();
    }

    // this function detects the obstacles in the scene,
    // the player is the center of the radar, and the obstacles are in the layer you set in the inspector
    // the obstacles are divided into 6 sectors, each sector is 60 degrees
    // the function will return a bool array, each element of the array indicates whether there is an obstacle in the sector or not
    void DetectObstacles() 
    {

        // Reset distances and states
        for (int i = 0; i < 6; i++)
        {
            High_obstacleSectors[i] = float.MaxValue;
            Low_obstacleSectors[i] = float.MaxValue;
            sectorStates[i] = 0; // Default to green
        }

        // player dir
        Vector3 forward = player.forward;

        // in the foreach loop, we check if the obstacle is in the sector or not
        // if it is, we set the corresponding element of the array to true
        // the angle is calculated by the signed angle between the player forward direction and the direction to the obstacle
        // the angle is in the range of 0 to 360 degrees
        // the sector index is calculated by dividing the angle by 60 degrees
        // the sector index is in the range of 0 to 5
        // the sector index is used to set the corresponding element of the array to true
        // Detect obstacles in collisionRadius (higher priority)

        Collider[] detectionLowColliders = Physics.OverlapSphere(player.position, detectionRadius, LowObstacleLayer);
        foreach (var collider in detectionLowColliders)
        {
            UpdateSectorState(collider, forward, 1); // Yellow for Low obstacles in detectionRadius
            /*
             // Perform RayCast to determine if it's truly a LowObstacle
            if (IsLowObstacle(collider))
            {
                UpdateSectorState(collider, forward, 1); // Yellow for Low obstacles in detectionRadius
            }
            else
            {
                UpdateSectorState(collider, forward, 2); // Red for High obstacles in detectionRadius
            }
             */
        }

        Collider[] detectionHighColliders = Physics.OverlapSphere(player.position, detectionRadius, HighObstacleLayer);
        foreach (var collider in detectionHighColliders)
        {
            UpdateSectorState(collider, forward, 2); // Red for High obstacles in detectionRadius
        }

        Collider[] collisionLowColliders = Physics.OverlapSphere(player.position, collisionRadius, LowObstacleLayer);
        foreach (var collider in collisionLowColliders)
        {
            UpdateSectorState(collider, forward, 3); // Orange for Low obstacles in collisionRadius
            /*
             // Perform RayCast to determine if it's truly a LowObstacle
            if (IsLowObstacle(collider))
            {
                UpdateSectorState(collider, forward, 3); // Orange for Low obstacles in collisionRadius
            }
            else
            {
                UpdateSectorState(collider, forward, 4); // Purple for High obstacles in collisionRadius
            }
             */
        }

        Collider[] collisionHighColliders = Physics.OverlapSphere(player.position, collisionRadius, HighObstacleLayer);
        foreach (var collider in collisionHighColliders)
        {
            UpdateSectorState(collider, forward, 4); // Purple for High obstacles in collisionRadius
        }
    }

    //Utilize raycast to determine if the obstacle is a LowObstacle still debugging to test
    //which method is better or the hybird one is better
    private bool IsLowObstacle(Collider collider)
    {
        // Define the ray origin at the player's leg height
        Vector3 rayOrigin = new Vector3(player.position.x, player.position.y - 0.5f, player.position.z); // Adjust height as needed
        Vector3 directionToObstacle = (collider.transform.position - rayOrigin).normalized;

        // Cast a ray towards the obstacle
        if (Physics.Raycast(rayOrigin, directionToObstacle, out RaycastHit hit, detectionRadius))
        {
            // Check if the hit object is the collider
            if (hit.collider == collider)
            {
                // Calculate the angle between the hit surface normal and the ground (Vector3.up)
                float angle = Vector3.Angle(hit.normal, Vector3.up);

                Debug.Log($"Ray hit {collider.name}, Angle: {angle}");

                // If the angle is small, consider it a LowObstacle
                return angle < 30f; // Adjust the threshold angle as needed
            }
        }

        return false; // If the ray doesn't hit or the angle is too large, consider it not a LowObstacle
    }


    void UpdateSectorState(Collider collider, Vector3 forward, int state)
    {
        Vector3 directionToObstacle = (collider.transform.position - player.position).normalized;
        float distance = Vector3.Distance(player.position, collider.transform.position);
        float angle = Vector3.SignedAngle(forward, directionToObstacle, Vector3.up);

        //April 21st
        //pls ignore this annotation, Still debugging (a minor bug which doesn't influence most functions)
        //there used to be a 30 degrees offset made by me to correct the direction of the player.forward and the sector
        //the direction of the player.forward is 0 degrees, and the sector is 30 degrees
        //but there is bug with the offset, so I remove it, and you can find the offset in the Main carmera 
        if (angle < 0) angle += 360f;
        if (angle >= 360f) angle -= 360f;

        Debug.Log($"Angle: {angle}, Distance: {distance}");

        int sectorIndex = Mathf.FloorToInt(angle / 60f);
        if (sectorIndex >= 0 && sectorIndex < 6)
        {
            // Update the state only if the new state has higher priority
            if (state > sectorStates[sectorIndex])
            {
                sectorStates[sectorIndex] = state;
            }
        }
    }


    // this function is used to draw the gizmos in the scene view
    // it will draw a wire sphere to show the detection range
    // and it will draw the sector-shaped areas to show the obstacles
    // the color of the sector is red if there is an obstacle in the sector, and green if there is no obstacle
    // the function is called when the game object is selected in the scene view
    void OnDrawGizmos()
    {
        if (player == null) return;

        // Draw the detectionRadius (outer circle)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, detectionRadius);

        // Draw the collisionRadius (inner circle)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, collisionRadius);

        // Draw the sectors with arcs
        Vector3 forward = player.forward;
        for (int i = 0; i < 6; i++)
        {
            // Calculate the start and end angles of the sector (redundancy)
            float startAngle = i * 60f;
            float endAngle = (i + 1) * 60f;

            // Ensure angles are within the range of 0 to 360 degrees
            if (startAngle < 0) startAngle += 360f;
            if (endAngle < 0) endAngle += 360f;

            // Calculate the start and end directions of the sector
            Vector3 startDir = Quaternion.Euler(0, startAngle, 0) * forward;
            Vector3 endDir = Quaternion.Euler(0, endAngle, 0) * forward;

            // Set the color based on the sector state
            switch (sectorStates[i])
            {
                case 4: // Purple for High obstacles in collisionRadius
                    Gizmos.color = new Color(0.5f, 0, 0.5f); // Purple
                    break;
                case 3: // Orange for Low obstacles in collisionRadius
                    Gizmos.color = new Color(1f, 0.5f, 0); // Orange
                    break;
                case 2: // Red for High obstacles in detectionRadius
                    Gizmos.color = Color.red;
                    break;
                case 1: // Yellow for Low obstacles in detectionRadius
                    Gizmos.color = Color.yellow;
                    break;
                default: // Green for no obstacles
                    Gizmos.color = Color.green;
                    break;
            }

            // Draw the sector boundary lines for detectionRadius
            Gizmos.DrawLine(player.position, player.position + startDir * detectionRadius);
            Gizmos.DrawLine(player.position, player.position + endDir * detectionRadius);

            // If there is a collision, draw the lines for collisionRadius
            if (sectorStates[i] == 4 || sectorStates[i] == 3)
            {
                Gizmos.DrawLine(player.position, player.position + startDir * collisionRadius);
                Gizmos.DrawLine(player.position, player.position + endDir * collisionRadius);
            }
        }
    }



    // this function is used to draw the GUI in the game view
    // it will draw the buttons (panels) in the hexagonal shape
    // the color of the button is red if there is an obstacle in the sector, and green if there is no obstacle
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
                    GUI.Button(rect, "Purple", buttonStyle);
                    break;
                case 3: // Orange
                    buttonStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.5f, 0));
                    GUI.Button(rect, "Orange", buttonStyle);
                    break;
                case 2: // Red
                    buttonStyle.normal.background = MakeTex(2, 2, Color.red);
                    GUI.Button(rect, "Red", buttonStyle);
                    break;
                case 1: // Yellow
                    buttonStyle.normal.background = MakeTex(2, 2, Color.yellow);
                    GUI.Button(rect, "Yellow", buttonStyle);
                    break;
                default: // Green
                    buttonStyle.normal.background = MakeTex(2, 2, Color.green);
                    GUI.Button(rect, "Green", buttonStyle);
                    break;
            }
        }
    }


    //change the color of the panel, red or green
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

}


/*
using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Windows;
using Unity.VisualScripting;
using System.Linq;
//by JL April 16 2025
public class Haptic_GUI_X : MonoBehaviour
{
    //              0
    //       5             1
    //              6
    //       4             2
    //              3

   
    // OBS_DETECTION
    public Transform player; // Player transform
    public float detectionRadius = 5f; // Detection radius
    public LayerMask HighObstacleLayer; // Layer for High obstacles
    public LayerMask LowObstacleLayer;  // Layer for Low obstacles
    private float[] High_obstacleSectors = Enumerable.Repeat(float.MaxValue, 6).ToArray(); // Distances to High obstacles
    private float[] Low_obstacleSectors = Enumerable.Repeat(float.MaxValue, 6).ToArray();  // Distances to Low obstacles

    // GUI
    public Vector2 center = new Vector2(790, 70); // Center of the GUI
    private int[] Haptic_ID = new int[7] { 0, 0, 0, 0, 0, 0, 0 };

    void Start()
    {
        // Initialize distances to a large value (indicating no obstacle detected)
        for (int i = 0; i < 6; i++)
        {
            High_obstacleSectors[i] = float.MaxValue;
            Low_obstacleSectors[i] = float.MaxValue;
        }
    }


    void Update()
    {
        DetectObstacles();
    }

    // this function detects the obstacles in the scene,
    // the player is the center of the radar, and the obstacles are in the layer you set in the inspector
    // the obstacles are divided into 6 sectors, each sector is 60 degrees
    // the function will return a bool array, each element of the array indicates whether there is an obstacle in the sector or not
    void DetectObstacles() 
    {

        // Reset distances
        for (int i = 0; i < 6; i++)
        {
            High_obstacleSectors[i] = float.MaxValue;
            Low_obstacleSectors[i] = float.MaxValue;
        }

        // player dir
        Vector3 forward = player.forward;

        // in the foreach loop, we check if the obstacle is in the sector or not
        // if it is, we set the corresponding element of the array to true
        // the angle is calculated by the signed angle between the player forward direction and the direction to the obstacle
        // the angle is in the range of 0 to 360 degrees
        // the sector index is calculated by dividing the angle by 60 degrees
        // the sector index is in the range of 0 to 5
        // the sector index is used to set the corresponding element of the array to true
        // Detect High obstacles
        Collider[] highColliders = Physics.OverlapSphere(player.position, detectionRadius, HighObstacleLayer);
        foreach (var collider in highColliders)
        {
            Vector3 directionToObstacle = (collider.transform.position - player.position).normalized;
            float distance = Vector3.Distance(player.position, collider.transform.position);
            float angle = Vector3.SignedAngle(forward, directionToObstacle, Vector3.up);

            // Adjust angle by 30 degrees
            angle += 30f;
            if (angle < 0) angle += 360f;
            if (angle >= 360f) angle -= 360f;

            int sectorIndex = Mathf.FloorToInt(angle / 60f);
            if (sectorIndex >= 0 && sectorIndex < 6)
            {
                // Update the distance if the new obstacle is closer
                if (distance < High_obstacleSectors[sectorIndex])
                {
                    High_obstacleSectors[sectorIndex] = distance;
                }
            }
        }

        // Detect Low obstacles
        Collider[] lowColliders = Physics.OverlapSphere(player.position, detectionRadius, LowObstacleLayer);
        foreach (var collider in lowColliders)
        {
            Vector3 directionToObstacle = (collider.transform.position - player.position).normalized;
            float distance = Vector3.Distance(player.position, collider.transform.position);
            float angle = Vector3.SignedAngle(forward, directionToObstacle, Vector3.up);

            // Adjust angle by 30 degrees
            angle += 30f;
            if (angle < 0) angle += 360f;
            if (angle >= 360f) angle -= 360f;

            int sectorIndex = Mathf.FloorToInt(angle / 60f);
            if (sectorIndex >= 0 && sectorIndex < 6)
            {
                // Update the distance if the new obstacle is closer
                if (distance < Low_obstacleSectors[sectorIndex])
                {
                    Low_obstacleSectors[sectorIndex] = distance;
                }
            }
        }
    }

    // this function is used to draw the gizmos in the scene view
    // it will draw a wire sphere to show the detection range
    // and it will draw the sector-shaped areas to show the obstacles
    // the color of the sector is red if there is an obstacle in the sector, and green if there is no obstacle
    // the function is called when the game object is selected in the scene view
    void OnDrawGizmos()
    {
        if (player == null) return;

        // 绘制检测范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, detectionRadius);

        // 绘制扇形区域
        Vector3 forward = player.forward;
        for (int i = 0; i < 6; i++)
        {
            // 计算扇形的起始角度和结束角度，并偏移30度
            float startAngle = i * 60f - 30f;
            float endAngle = (i + 1) * 60f - 30f;

            // 确保角度在0到360度范围内
            if (startAngle < 0) startAngle += 360f;
            if (endAngle < 0) endAngle += 360f;

            // 计算扇形的起始方向和结束方向
            Vector3 startDir = Quaternion.Euler(0, startAngle, 0) * forward;
            Vector3 endDir = Quaternion.Euler(0, endAngle, 0) * forward;

            // 根据障碍物类型设置颜色
            if (High_obstacleSectors[i] < float.MaxValue)
            {
                Gizmos.color = Color.red; // 高障碍物：红色
            }
            else if (Low_obstacleSectors[i] < float.MaxValue)
            {
                Gizmos.color = Color.yellow; // 低障碍物：黄色
            }
            else
            {
                Gizmos.color = Color.green; // 无障碍物：绿色
            }
            
            // 绘制扇形的边界线
            Gizmos.DrawLine(player.position, player.position + startDir * detectionRadius);
            Gizmos.DrawLine(player.position, player.position + endDir * detectionRadius);
        }
    }

    // this function is used to draw the GUI in the game view
    // it will draw the buttons (panels) in the hexagonal shape
    // the color of the button is red if there is an obstacle in the sector, and green if there is no obstacle
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

            // Determine the color based on obstacle type and priority
            if (High_obstacleSectors[i] < float.MaxValue)
            {
                buttonStyle.normal.background = MakeTex(2, 2, Color.red); // High obstacle: red
                GUI.Button(rect, $"High\n{High_obstacleSectors[i]:F1}m", buttonStyle);
            }
            else if (Low_obstacleSectors[i] < float.MaxValue)
            {
                buttonStyle.normal.background = MakeTex(2, 2, Color.yellow); // Low obstacle: yellow
                GUI.Button(rect, $"Low\n{Low_obstacleSectors[i]:F1}m", buttonStyle);
            }
            else
            {
                buttonStyle.normal.background = MakeTex(2, 2, Color.green); // No obstacle: green
                GUI.Button(rect, $"Clear", buttonStyle);
            }
        }
    }


    //change the color of the panel, red or green
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

}

 */