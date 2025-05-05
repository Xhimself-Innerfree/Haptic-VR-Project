using UnityEngine;
using System.Collections;

//=============================================================================
//  HaloTrafficLight
//  by Healthbar Games (http://healthbargames.pl)
//  author: Mariusz Skowroński
//
//  Simple implementation of TrafficLight
//  For each of three light colors (red, yellow and green) it uses
//  one mesh renderer and one object with halo effect attached.
//  To visualize the states of lights (on / off) it requires two materials:
//  - one with a texture for the lights turned off
//  - and one with a texture for the lights turned on.
//  You can use (like in demo scene) two different materials with single,
//  common texture for light states.
//=============================================================================


public class HaloTrafficLight : MonoBehaviour
{
    public Renderer RedRenderer;
    public GameObject RedHalo;

    public Renderer YellowRenderer;
    public GameObject YellowHalo;

    public Renderer GreenRenderer;
    public GameObject GreenHalo;

    public Material LightsOnMat;
    public Material LightsOffMat;

    public int GreenLightDuration = 10; // Duration of green light in seconds
    public int YellowLightDuration = 3; // Duration of yellow light in seconds
    public int RedLightDuration = 7; // Duration of red light in seconds

    private float mTimer = 0f;
    private LightState mCurrentState = LightState.Green; // Start with green light

    public enum LightState
    {
        Green,
        Yellow,
        Red
    }

    void Awake()
    {
        if ((RedRenderer == null && RedHalo == null) ||
            (YellowRenderer == null && YellowHalo == null) ||
            (GreenRenderer == null && GreenHalo == null))
        {
            Debug.LogError("Some variables haven't been assigned correctly for HaloTrafficLight script.", this);
        }
    }

    void Update()
    {
        mTimer += Time.deltaTime;

        switch (mCurrentState)
        {
            case LightState.Green:
                if (mTimer >= GreenLightDuration)
                {
                    SetLightState(LightState.Yellow);
                }
                break;

            case LightState.Yellow:
                if (mTimer >= YellowLightDuration)
                {
                    SetLightState(LightState.Red);
                }
                break;

            case LightState.Red:
                if (mTimer >= RedLightDuration)
                {
                    SetLightState(LightState.Green);
                }
                break;
        }
    }

    private void SetLightState(LightState newState)
    {
        mCurrentState = newState;
        mTimer = 0f;

        // Update light states
        bool redLight = (newState == LightState.Red);
        bool yellowLight = (newState == LightState.Yellow);
        bool greenLight = (newState == LightState.Green);

        if (RedHalo != null) RedHalo.SetActive(redLight);
        if (RedRenderer != null) RedRenderer.material = redLight ? LightsOnMat : LightsOffMat;

        if (YellowHalo != null) YellowHalo.SetActive(yellowLight);
        if (YellowRenderer != null) YellowRenderer.material = yellowLight ? LightsOnMat : LightsOffMat;

        if (GreenHalo != null) GreenHalo.SetActive(greenLight);
        if (GreenRenderer != null) GreenRenderer.material = greenLight ? LightsOnMat : LightsOffMat;
    }

    public LightState GetState()
    {
        return mCurrentState;
    }

    void OnDrawGizmos()
    {
        // Set the color for the Gizmos
        Gizmos.color = Color.red; // Changed from cyan to red

        // Get the forward direction of the traffic light
        Vector3 forward = transform.forward;

        // Draw the 90-degree sector
        int segments = 20; // Number of segments to approximate the arc
        float angleStep = 90f / segments; // Angle step for each segment
        float radius = 5f; // Radius of the sector

        Vector3 startPoint = transform.position;
        Vector3 previousPoint = startPoint + Quaternion.Euler(0, -45f, 0) * forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -45f + i * angleStep;
            Vector3 currentPoint = startPoint + Quaternion.Euler(0, currentAngle, 0) * forward * radius;

            // Draw a line between the previous point and the current point
            Gizmos.DrawLine(previousPoint, currentPoint);

            // Update the previous point
            previousPoint = currentPoint;
        }

        // Draw lines from the center to the edges of the sector
        Gizmos.DrawLine(startPoint, startPoint + Quaternion.Euler(0, -45f, 0) * forward * radius);
        Gizmos.DrawLine(startPoint, startPoint + Quaternion.Euler(0, 45f, 0) * forward * radius);
    }
}
