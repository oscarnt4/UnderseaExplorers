using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;

public class MermaidBehaviourTree : MonoBehaviour
{
    public int behaviour;
    public LayerMask wallLayer;
    public float visionDistance = 10f;
    public float wallDistance = 3f;
    [Range(0f, 1f)]
    public float wallAvoidanceFactor = 0.8f;
    public float persuitDuration = 5f;
    [Range(0f, 1f)]
    public float figure8Size = 0.8f;


    private List<GameObject> targets;
    private MermaidMovement movement;
    private Root tree;
    private Blackboard blackboard;
    private Quaternion startRotation;
    private bool loopAnticlockwise;
    private float timeSinceLoopSwitch;

    void Awake()
    {
        targets = new List<GameObject>();
    }

    void Start()
    {
        movement = GetComponent<MermaidMovement>();
        startRotation = transform.rotation;
        loopAnticlockwise = false;
        timeSinceLoopSwitch = 0f;

        tree = CreateBehaviourTree();
        blackboard = tree.Blackboard;

        tree.Start();
    }

    private Root CreateBehaviourTree()
    {
        switch (behaviour)
        {
            case 0:
                return null;//Figure8Behaviour();
            case 1:
                return SeekingBehaviour();

            default:
                return null;//Figure8Behaviour();
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

    private void UpdatePerception()
    {
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, 45) * transform.up * 10, Color.red);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -45) * transform.up * 10, Color.blue);

        RaycastHit2D hitOnRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -45) * transform.up, Mathf.Infinity, wallLayer);
        RaycastHit2D hitOnLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 45) * transform.up, Mathf.Infinity, wallLayer);

        RaycastHit2D immediateRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -90) * transform.up, Mathf.Infinity, wallLayer);
        RaycastHit2D immediateLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 90) * transform.up, Mathf.Infinity, wallLayer);

        if (Mathf.Abs(transform.rotation.eulerAngles.z - startRotation.eulerAngles.z) < 0.5f && timeSinceLoopSwitch > 1f)
        {
            timeSinceLoopSwitch = 0f;
            loopAnticlockwise = !loopAnticlockwise;
        }
        timeSinceLoopSwitch += Time.deltaTime;

        Transform closestTarget = ClosestTarget();
        Vector3 targetPosition = closestTarget == null ? Vector3.positiveInfinity : closestTarget.position;
        Vector3 targetRelativePosition = this.transform.InverseTransformPoint(targetPosition);
        Vector3 targetDirection = targetRelativePosition.normalized;

        blackboard["wallDistanceOnRight"] = hitOnRight.distance;
        blackboard["wallDistanceOnLeft"] = hitOnLeft.distance;
        blackboard["rightWallCloser"] = immediateRight.distance <= immediateLeft.distance;
        blackboard["loopAnticlockwise"] = loopAnticlockwise;
        blackboard["distanceToNearestTarget"] = targetRelativePosition.magnitude;
        blackboard["targetInFront"] = Mathf.Abs(targetDirection.x) < 0.1f && targetDirection.y > 0;
        blackboard["targetToTheRight"] = targetDirection.x > 0;

    }

    public void AddTarget(GameObject target)
    {
        targets.Add(target);
    }

    private Transform ClosestTarget()
    {
        float currentLowestDistance = Mathf.Infinity;
        Transform targetTransform = null;
        foreach (GameObject target in targets)
        {
            if (target == null) continue;
            float distance = this.transform.InverseTransformPoint(target.transform.position).magnitude;
            if (distance < currentLowestDistance)
            {
                currentLowestDistance = distance;
                targetTransform = target.transform;
            }
        }
        return targetTransform;
    }


    private Root SeekingBehaviour()
    {
        return new Root(new Service(() => UpdatePerception(),
                            new Selector(
                                new TimeMax(persuitDuration, true, Persue()),
                                Figure8Behaviour()
                                )
                            ));
    }

    private Node Figure8Behaviour()
    {
        return new Selector(
                Figure8Motion(),
                TurnAwayFromWall());
    }

    private Node TurnAwayFromWall()
    {
        return new Selector(
                    new Sequence(
                        new BlackboardCondition("rightWallCloser", Operator.IS_EQUAL, true, Stops.SELF,
                            new BlackboardCondition("wallDistanceOnRight", Operator.IS_SMALLER_OR_EQUAL, wallDistance, Stops.SELF,
                                new Action(() => Turn(wallAvoidanceFactor)))),
                        new Action(() => Move(0.3f))),
                    new Sequence(
                        new BlackboardCondition("wallDistanceOnLeft", Operator.IS_SMALLER_OR_EQUAL, wallDistance, Stops.SELF,
                            new Action(() => Turn(-wallAvoidanceFactor))),
                        new Action(() => Move(0.3f))
                        ));
    }

    private Node Figure8Motion()
    {
        return new BlackboardCondition("wallDistanceOnRight", Operator.IS_GREATER_OR_EQUAL, wallDistance, Stops.SELF,
                new BlackboardCondition("wallDistanceOnLeft", Operator.IS_GREATER_OR_EQUAL, wallDistance, Stops.SELF,
                    new Selector(new BlackboardCondition("loopAnticlockwise", Operator.IS_EQUAL, true, Stops.SELF,
                                    new Sequence(
                                        new Action(() => Turn(1f - figure8Size)),
                                        new Action(() => Move(1f))
                                        )),
                                new Sequence(
                                    new Action(() => Turn(-1f + figure8Size)),
                                    new Action(() => Move(1f))))
                    ));
    }

    private Node Persue()
    {
        return new BlackboardCondition("distanceToNearestTarget", Operator.IS_SMALLER_OR_EQUAL, visionDistance, Stops.SELF,
                new Selector(
                    TurnAwayFromWall(),
                    new Sequence(
                        PointAtTarget(),
                        new Action(() => Move(1f)))
                    ));
    }

    private Node PointAtTarget()
    {
        return new Selector(new BlackboardCondition("targetInFront", Operator.IS_EQUAL, false, Stops.SELF,
                                new Selector(new BlackboardCondition("targetToTheRight", Operator.IS_EQUAL, true, Stops.SELF,
                                                new Action(() => Turn(-1f))),
                                            new Action(() => Turn(1f)))),
                             new Action(() => Turn(0f)));
    }

    private void OnDestroy()
    {
        StopBehaviorTree();
    }

    public void StopBehaviorTree()
    {
        if (tree != null && tree.CurrentState == Node.State.ACTIVE)
        {
            tree.Stop();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int targetIdx = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            if (collision.gameObject == targets[i])
            {
                targetIdx = i;
            }
        }

        if (targetIdx != -1)
        {
            Destroy(targets[targetIdx]);
            targets.RemoveAt(targetIdx);
        }
    }
}
