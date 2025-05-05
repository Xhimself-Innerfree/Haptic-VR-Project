using UnityEngine;
using UnityEngine.AI;

public class AI_Navi_Debug : MonoBehaviour
{
    public NavMeshAgent nav; 
    public Transform target; 

    private void Update()
    {
        nav.SetDestination(target.position); 
    }

}
