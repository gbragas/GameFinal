using UnityEngine;
using UnityEngine.AI;

public class SlimeAttackState : StateMachineBehaviour
{

    Transform player;
    private NavMeshAgent agent;
    public float rotationSpeed = 8f;
    public bool playerDead;

     override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        playerDead = animator.GetBool("PlayerDead");

        player = GameObject.FindGameObjectWithTag("Player").transform;
        if(agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }


    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();

        Vector3 target = player.position;
        target.y = animator.transform.position.y;
        animator.transform.LookAt(target);

        float distance = Vector3.Distance(player.position, animator.transform.position);
        
        if (agent != null)
        {
            float attackRange = 4.1f;

            if (distance < attackRange)
            {
                agent.velocity = Vector3.Lerp(agent.velocity, Vector3.zero, Time.deltaTime * 10f);
                agent.isStopped = true;
                agent.ResetPath();
            }
            else
            {
                float slowFactor = Mathf.Clamp01(distance / attackRange);
                agent.velocity *= slowFactor;
            }
        }
        if(player == null || !player.gameObject.activeInHierarchy)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsChasing", false);
            agent.isStopped = false;
            animator.SetBool("IsPatrolling", false);
            animator.SetBool("PlayerDead", true);
        }

        if (distance > 1.2f)
            animator.SetBool("IsAttacking", false);
        
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       if(agent != null)
       {
            agent.isStopped = false;
       }
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
