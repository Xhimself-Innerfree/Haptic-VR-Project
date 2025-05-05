using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC_Move : MonoBehaviour
{
    public NavMeshAgent agent;
    public float Radius;

    public Transform centrePoint; // Center point, defines the center of the movement range

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent.remainingDistance <= agent.stoppingDistance) // If the target point is reached
        {
            Vector3 point;
            if (RandomPoint(centrePoint.position, Radius, out point)) // Generate a random point
            {
                Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f); // Visualize the random point with a blue ray
                agent.SetDestination(point); // Set the target point
            }
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range; // Generate a random point within a sphere
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas)) // Ensure the random point is on the navigation mesh
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // Draw the movement range Gizmos
    private void OnDrawGizmos()
    {
        if (centrePoint != null)
        {
            Gizmos.color = Color.green; // Set the Gizmos color to green
            Gizmos.DrawWireSphere(centrePoint.position, Radius); // Draw a green wireframe sphere to represent the movement range
        }
    }
}
