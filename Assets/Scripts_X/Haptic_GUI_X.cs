using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Windows;
using Unity.VisualScripting;
//by JL April 16 2025
public class Haptic_GUI_X : MonoBehaviour
{
    //              0
    //       5             1
    //              6
    //       4             2
    //              3
    
    //OBS_DETECTION
    public Transform player; // add the player here in Scene to get the transform (the position, orientation and scale of a object)
    public float detectionRadius = 5f; // the radius of your detection radar
    public LayerMask obstacleLayer; // this is associated with the layer of the object you want to detect, you can set it in the inspector
    private bool[] obstacleSectors = new bool[6]; // this stores whether there is obstacle in the sector or not

    //GUI
    public Vector2 center = new Vector2(790, 70);// 366   690 The center of the panels
    private int[] Haptic_ID = new int[7] { 0, 0, 0, 0, 0, 0, 0 };

    void Start()
    {
        
    }

    
    void Update()
    {
        DetectObstacles();
        OnGUI();
    }

    // this function detects the obstacles in the scene,
    // the player is the center of the radar, and the obstacles are in the layer you set in the inspector
    // the obstacles are divided into 6 sectors, each sector is 60 degrees
    // the function will return a bool array, each element of the array indicates whether there is an obstacle in the sector or not
    void DetectObstacles() 
    {
        
        for (int i = 0; i < obstacleSectors.Length; i++)
        {
            obstacleSectors[i] = false;
        }

        // player dir
        Vector3 forward = player.forward;

        // get all the colliders in the detection radius
        Collider[] colliders = Physics.OverlapSphere(player.position, detectionRadius, obstacleLayer);

        // in the foreach loop, we check if the obstacle is in the sector or not
        // if it is, we set the corresponding element of the array to true
        // the angle is calculated by the signed angle between the player forward direction and the direction to the obstacle
        // the angle is in the range of 0 to 360 degrees
        // the sector index is calculated by dividing the angle by 60 degrees
        // the sector index is in the range of 0 to 5
        // the sector index is used to set the corresponding element of the array to true
        foreach (var collider in colliders)
        {
            Vector3 directionToObstacle = (collider.transform.position - player.position).normalized;
            float angle = Vector3.SignedAngle(forward, directionToObstacle, Vector3.up);

            if (angle < 0)
            {
                angle += 360;
            }

            int sectorIndex = Mathf.FloorToInt(angle / 60f);
            if (sectorIndex >= 0 && sectorIndex < 6)
            {
                obstacleSectors[sectorIndex] = true;
            }
        }

        
        for (int i = 0; i < obstacleSectors.Length; i++)
        {
            Debug.Log($"Partition {i}: {(obstacleSectors[i] ? "Yes" : "No")}");
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

        // draw the detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, detectionRadius);

        // Draw sector-shaped areas
        Vector3 forward = player.forward;
        for (int i = 0; i < 6; i++)
        {
            float startAngle = i * 60f;
            float endAngle = (i + 1) * 60f;

            Vector3 startDir = Quaternion.Euler(0, startAngle, 0) * forward;
            Vector3 endDir = Quaternion.Euler(0, endAngle, 0) * forward;

            Gizmos.color = obstacleSectors[i] ? Color.red : Color.green;
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
        positions[6] = center ;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 9; 
        buttonStyle.alignment = TextAnchor.MiddleCenter; 

        for (int i = 0; i < positions.Length; i++)
        {

            //Rect rect = new Rect(positions[i].x - radius, positions[i].y - radius, radius * 2, radius * 2);
            Rect rect = new Rect(positions[i].x, positions[i].y , radius , radius);
            /*if (GUI.Button(rect, $"Haptic {i}", buttonStyle))
            {
                
            }*/
            if (obstacleSectors[i]) {
                buttonStyle.normal.background = MakeTex(2, 2, Color.red);
                GUI.Button(rect, $"Haptic {i}", buttonStyle);
            }
            else
            {
                buttonStyle.normal.background = MakeTex(2, 2, Color.green);
                GUI.Button(rect, $"Haptic {i}", buttonStyle);
            }
        }
    }

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
    } //change the color of the panel, red or green

}
