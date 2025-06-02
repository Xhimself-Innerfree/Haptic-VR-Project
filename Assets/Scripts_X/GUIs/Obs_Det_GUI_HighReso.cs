using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;
using System.Net.Sockets;

public class Obs_Det_GUI_HighReso : MonoBehaviour
{
    public int sectorCount = 12; // 可设置为12或18
    public Transform player;
    public float farDetectionRadius = 5f;
    public float nearDetectionRadius = 1f;
    public LayerMask obstacleLayer;
    public int raysPerSector = 5;

    public float CarRadius = 3f;
    public LayerMask carLayer;

    public struct Obs_Sector_State
    {
        public float currentDistance;
        public float minDistance;
        public float preDistance;
        public float timer;
        public int realState;
        public int showState;
        public float resetTimer;
    }

    private Obs_Sector_State[] sectorStates;

    public HaloTrafficLight trafficLight;

    public float playerBottomOffset = 0.85f;
    private Vector3 playerBottom;
    public float verticalStep = 1f;
    public int StepThreshold = 1;

    public bool enableGizmos = true;

    public Vector2 center = new Vector2(900, 70);

    public TCP_Client_X tcpClient;

    void Start()
    {
        sectorStates = new Obs_Sector_State[sectorCount];
        for (int i = 0; i < sectorCount; i++)
        {
            sectorStates[i] = new Obs_Sector_State
            {
                currentDistance = 6f,
                minDistance = 6f,
                timer = 0f,
                realState = 0,
                showState = 0,
                resetTimer = 0f,
                preDistance = 6f
            };
        }
    }

    void Update()
    {
        playerBottom = player.position - new Vector3(0, playerBottomOffset, 0);
        DetectObstaclesWithRayCast();
        CheckTrafficLightState();
        SendSectorStates();
    }

    void DetectObstaclesWithRayCast()
    {
        Vector3 forward = player.forward;
        float sectorAngleSpan = 360f / sectorCount;

        for (int i = 0; i < sectorCount; i++)
        {
            float sectorStartAngle = i * sectorAngleSpan - sectorAngleSpan / 2f;
            float sectorEndAngle = (i + 1) * sectorAngleSpan - sectorAngleSpan / 2f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;
            int tempState = 0;
            float closestDistance = float.MaxValue;
            int tempShowState = 0;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

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
                    {
                        inSector = normAngleToCar >= normSectorStart && normAngleToCar < normSectorEnd;
                    }
                    else
                    {
                        inSector = normAngleToCar >= normSectorStart || normAngleToCar < normSectorEnd;
                    }

                    if (inSector)
                    {
                        tempState = 4;
                        closestDistance = Mathf.Min(closestDistance, distToCar);
                        break;
                    }
                }
            }

            sectorStates[i].preDistance = sectorStates[i].currentDistance;
            sectorStates[i].currentDistance = closestDistance;
            sectorStates[i].realState = tempState;

            if (sectorStates[i].realState == 0)
            {
                sectorStates[i].minDistance = 6f;
                sectorStates[i].timer = 0f;
                sectorStates[i].resetTimer = 0f;
                tempShowState = 0;
            }
            else
            {
                float prev = sectorStates[i].preDistance;
                float curr = sectorStates[i].currentDistance;
                bool isApproaching = curr < prev - 0.01f;

                if (isApproaching)
                {
                    tempShowState = sectorStates[i].realState;
                    sectorStates[i].timer = 0f;
                    sectorStates[i].resetTimer = 0f;
                }
                else
                {
                    sectorStates[i].timer += Time.deltaTime;
                    if (sectorStates[i].timer < 0.5f)
                    {
                        tempShowState = sectorStates[i].realState;
                        sectorStates[i].resetTimer = 0f;
                    }
                    else
                    {
                        sectorStates[i].resetTimer += Time.deltaTime;
                        float cycle = sectorStates[i].resetTimer % 3.0f;
                        if (cycle < 0.5f)
                        {
                            tempShowState = sectorStates[i].realState;
                        }
                        else
                        {
                            tempShowState = 0;
                        }
                    }
                }

                if (curr <= sectorStates[i].minDistance)
                {
                    sectorStates[i].minDistance = curr;
                }
                else
                {
                    sectorStates[i].minDistance = Mathf.Max(curr - 0.001f, nearDetectionRadius);
                }
            }

            sectorStates[i].showState = tempShowState;
        }
    }

    void CheckTrafficLightState()
    {
        if (trafficLight != null && trafficLight.GetState() == HaloTrafficLight.LightState.Red)
        {
            Vector3 directionToPlayer = player.position - trafficLight.transform.position;
            float angle = Vector3.Angle(trafficLight.transform.forward, directionToPlayer);

            if (angle < 45f)
            {
                Vector3 forward = player.forward;
                float sectorAngle = Vector3.SignedAngle(forward, directionToPlayer, Vector3.up);
                float sectorAngleSpan = 360f / sectorCount;
                int sectorIndex = Mathf.FloorToInt((sectorAngle + 180f) / sectorAngleSpan) % sectorCount;

                if (sectorStates[sectorIndex].realState != 4)
                {
                    sectorStates[sectorIndex].realState = 4;
                    sectorStates[sectorIndex].timer = 0f;
                    sectorStates[sectorIndex].resetTimer = 0f;
                    sectorStates[sectorIndex].showState = 4;
                }
                else
                {
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

    void OnDrawGizmos()
    {
        if (player == null) return;
        if (!enableGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), farDetectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), nearDetectionRadius);

        Vector3 forward = player.forward;
        float sectorAngleSpan = 360f / (sectorCount > 0 ? sectorCount : 1);

        for (int i = 0; i < (sectorCount > 0 ? sectorCount : 1); i++)
        {
            float sectorStartAngle = i * sectorAngleSpan - sectorAngleSpan / 2f;
            float sectorEndAngle = (i + 1) * sectorAngleSpan - sectorAngleSpan / 2f;
            float angleStep = (sectorEndAngle - sectorStartAngle) / raysPerSector;

            for (int j = 0; j < raysPerSector; j++)
            {
                float currentAngle = sectorStartAngle + j * angleStep;
                Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * forward;

                switch (sectorStates != null && i < sectorStates.Length ? sectorStates[i].showState : 0)
                {
                    case 4:
                        Gizmos.color = new Color(0.5f, 0, 0.5f);
                        break;
                    case 3:
                        Gizmos.color = new Color(1f, 0.5f, 0);
                        break;
                    case 2:
                        Gizmos.color = Color.red;
                        break;
                    case 1:
                        Gizmos.color = Color.yellow;
                        break;
                    default:
                        Gizmos.color = Color.green;
                        break;
                }

                Gizmos.DrawRay(player.position - new Vector3(0, playerBottomOffset, 0), rayDirection * farDetectionRadius);
            }

            Gizmos.color = Color.black;
            Vector3 DivideDirection = Quaternion.Euler(0, sectorStartAngle, 0) * forward;
            Gizmos.DrawRay(player.position - new Vector3(0, playerBottomOffset, 0), DivideDirection * farDetectionRadius);
        }

        Gizmos.color = new Color(0.5f, 0, 0.5f, 0.2f);
        Gizmos.DrawWireSphere(player.position - new Vector3(0, playerBottomOffset, 0), CarRadius);
    }

    void OnGUI()
    {
        float radius = 20f;
        float hexRadius = 60f;
        Vector2[] positions = new Vector2[sectorCount + 1];

        float angleStep = 2 * Mathf.PI / sectorCount;
        for (int i = 0; i < sectorCount; i++)
        {
            float angle = -Mathf.PI / 2 + i * angleStep;
            positions[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * hexRadius;
        }
        positions[sectorCount] = center;

        for (int i = 0; i < sectorCount; i++)
        {
            Rect rect = new Rect(positions[i].x - radius / 2, positions[i].y - radius / 2, radius, radius);

            Color color;
            switch (sectorStates[i].showState)
            {
                case 4:
                    color = new Color(0.5f, 0, 0.5f);
                    break;
                case 3:
                    color = new Color(1f, 0.5f, 0);
                    break;
                case 2:
                    color = Color.red;
                    break;
                case 1:
                    color = Color.yellow;
                    break;
                default:
                    color = Color.green;
                    break;
            }

            GUI.DrawTexture(rect, MakeCircleTex((int)radius, color));
        }
    }

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
                    pix[y * diameter + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    void SendSectorStates()
    {
        if (tcpClient != null && tcpClient.Client_Socket != null && tcpClient.Client_Socket.Connected)
        {
            string message = string.Join(",", sectorStates.Select(s => s.realState));
            tcpClient.inputMes = message;
            tcpClient.SendFlag = true;
        }
    }
}
