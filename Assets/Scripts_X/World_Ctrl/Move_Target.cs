using UnityEngine;
using UnityEngine.AI;

public class Move_Target : MonoBehaviour
{
    public NavMeshAgent nav;
    public Transform[] targets; // A set of target points
    private int currentTargetIndex = 0; // Current target index
    private bool isClosedPath = false; // Whether the path is closed
    public bool showPathGizmos = true; // Toggle to show the path

    private void Start()
    {
        // Check if the path is closed
        if (targets.Length > 1 && targets[0].position == targets[targets.Length - 1].position)
        {
            isClosedPath = true;
        }
    }

    private void Update()
    {
        if (targets.Length == 0) return; // If there are no targets, return immediately

        // If the current target point is reached
        if (!nav.pathPending && nav.remainingDistance <= nav.stoppingDistance)
        {
            // Update to the next target point
            currentTargetIndex++;

            // If the last target point is reached
            if (currentTargetIndex >= targets.Length)
            {
                if (isClosedPath)
                {
                    // If the path is closed, loop back to the first target point
                    currentTargetIndex = 0;
                }
                else
                {
                    // If the path is not closed, stop moving
                    nav.isStopped = true;
                    return;
                }
            }

            // Set the next target point
            nav.SetDestination(targets[currentTargetIndex].position);
        }
    }

    // Draw the path Gizmos
    private void OnDrawGizmos()
    {
        if (!showPathGizmos || targets == null || targets.Length == 0) return;

        Gizmos.color = Color.blue;

        // Draw lines between target points
        for (int i = 0; i < targets.Length - 1; i++)
        {
            if (targets[i] != null && targets[i + 1] != null)
            {
                Gizmos.DrawLine(targets[i].position, targets[i + 1].position);
            }
        }

        // If the path is closed, draw a line from the last point to the first point
        if (isClosedPath && targets[0] != null && targets[targets.Length - 1] != null)
        {
            Gizmos.DrawLine(targets[targets.Length - 1].position, targets[0].position);
        }

        // Draw the target points
        Gizmos.color = Color.red;
        foreach (var target in targets)
        {
            if (target != null)
            {
                Gizmos.DrawSphere(target.position, 0.2f);
            }
        }
    }
}
