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
    public Transform player; // 
    public float detectionRadius = 5f; // 
    public LayerMask obstacleLayer; // 
    private bool[] obstacleSectors = new bool[6]; // 

    //GUI
    public Vector2 center = new Vector2(790, 70);// 366   690
    private int[] Haptic_ID = new int[7] { 0, 0, 0, 0, 0, 0, 0 };

    void Start()
    {
        
    }

    
    void Update()
    {
        DetectObstacles();
    }

    void DetectObstacles()
    {
        
        for (int i = 0; i < obstacleSectors.Length; i++)
        {
            obstacleSectors[i] = false;
        }

        // player dir
        Vector3 forward = player.forward;

        
        Collider[] colliders = Physics.OverlapSphere(player.position, detectionRadius, obstacleLayer);

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

    void OnDrawGizmos()
    {
        if (player == null) return;

        // »æÖÆ¼ì²â·¶Î§
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, detectionRadius);

        // »æÖÆÉÈÐÎÇøÓò
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
    }

}
