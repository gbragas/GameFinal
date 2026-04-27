using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TurtlePatrolState : StateMachineBehaviour
{
    float timer;
    List<Transform> wayPoints = new List<Transform>();
    Transform player;
    GameObject playerObj;
    float chaseRange = 16f;
    UnityEngine.AI.NavMeshAgent agent;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on the GameObject.");
            return;
        }
        timer = 0;
        playerObj = GameObject.FindGameObjectWithTag("Player");
        agent.speed = 3f;
        GameObject go = GameObject.FindGameObjectWithTag("WayPoints");
        foreach (Transform waypoint in go.transform)
        {
            wayPoints.Add(waypoint);
        }

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(wayPoints[Random.Range(0, wayPoints.Count)].position);

        animator.SetBool("IsAttacking", false);
        animator.SetBool("IsChasing", false);
        animator.SetBool("PlayerDead", false);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(agent == null || wayPoints == null || wayPoints.Count == 0)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
            agent.SetDestination(wayPoints[Random.Range(0, wayPoints.Count)].position);
        timer += Time.deltaTime;
        if (timer > 10)
            animator.SetBool("IsPatrolling", false);

        if (playerObj != null)
        {
            player = playerObj.transform;
            float distance = Vector3.Distance(player.position, animator.transform.position);
            if (distance < chaseRange)
                animator.SetBool("IsChasing", true);
        }
        else
        {
            Debug.Log("Player object not found.");
            animator.SetBool("IsChasing", false);
        }
    
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       agent.SetDestination(agent.transform.position);
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
