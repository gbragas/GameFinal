using UnityEngine;
using UnityEngine.AI;

public class TurtleChaseState : StateMachineBehaviour
{

    NavMeshAgent agent;
    Transform player;

     override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       if (player == null)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            return;
        }

        if (!player.gameObject.activeInHierarchy)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(player.position, animator.transform.position);

        if (distance > 30f)
            animator.SetBool("IsChasing", false);
        

        if (distance < 4.5f)
        {    
            agent.isStopped = true;
            agent.ResetPath();  
            animator.SetBool("IsAttacking", true);
        }
        else
            animator.SetBool("IsAttacking", false);
        

    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

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
