using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;

public class AgentBehaviourTree : MonoBehaviour
{
    public int behaviour = 0;

    private Movement movement;
    private List<GameObject> targets;
    private List<GameObject> avoidances;
    private Root tree;
    private Blackboard blackboard;

    void Awake()
    {
        targets = new List<GameObject>();
        avoidances = new List<GameObject>();
    }
    void Start()
    {
        movement = GetComponent<Movement>();

        tree = CreateBehaviourTree();
        blackboard = tree.Blackboard;

        tree.Start();
    }

    private Root CreateBehaviourTree()
    {
        switch (behaviour)
        {
            case 0:
                return CirclingBehaviour();

            default:
                return CirclingBehaviour();
        }
    }

    private void Move(float amount)
    {
        movement.MoveBehaviour(amount);
    }

    private void Turn(float amount)
    {
        movement.TurnBehaviour(amount);
    }

    private Root CirclingBehaviour()
    {
        return new Root(new Sequence(
            new Action(() => Move(1f)),
            new Action(() => Turn(1f))
            ));
    }
}
