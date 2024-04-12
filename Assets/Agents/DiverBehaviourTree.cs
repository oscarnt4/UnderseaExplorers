using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using static UnityEngine.GraphicsBuffer;

public class DiverBehaviourTree : MonoBehaviour
{
    public int behaviour;
    public LayerMask wallLayer;
    public float visionDistance = 8f;
    public float wallAvoidDistance = 3f;
    public float wallHugDistance = 5f;
    [Range(0f, 1f)]
    public float wallAvoidanceFactor = 0.8f;

    private DiverMovement movement;
    private List<GameObject> targets;
    private List<GameObject> enemies;
    private Root tree;
    private Blackboard blackboard;

    void Awake()
    {
        targets = new List<GameObject>();
        enemies = new List<GameObject>();
    }
    void Start()
    {
        movement = GetComponent<DiverMovement>();

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
            case 1:
                return EvadingBehaviour();

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

    private void UpdatePerception()
    {
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, 45) * transform.up * 10, Color.red);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -45) * transform.up * 10, Color.blue);

        RaycastHit2D hitOnRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -45) * transform.up, Mathf.Infinity, wallLayer);
        RaycastHit2D hitOnLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 45) * transform.up, Mathf.Infinity, wallLayer);

        RaycastHit2D immediateRight = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, -90) * transform.up, Mathf.Infinity, wallLayer);
        RaycastHit2D immediateLeft = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, 90) * transform.up, Mathf.Infinity, wallLayer);

        Transform closestEnemy = ClosestObject(enemies);
        Vector3 enemyPosition = enemies != null ? closestEnemy.position : Vector3.positiveInfinity;
        Vector3 enemyRelativePosition = this.transform.InverseTransformPoint(enemyPosition);
        Vector3 enemyDirection = enemyRelativePosition.normalized;

        Transform closestTarget = ClosestObject(targets);
        Vector3 targetPosition = targets != null ? closestTarget.position : Vector3.positiveInfinity;
        Vector3 targetRelativePosition = this.transform.InverseTransformPoint(targetPosition);
        Vector3 targetDirection = targetRelativePosition.normalized;

        blackboard["wallDistanceOnRight"] = hitOnRight.distance;
        blackboard["wallDistanceOnLeft"] = hitOnLeft.distance;
        blackboard["rightWallCloser"] = immediateRight.distance <= immediateLeft.distance;

        blackboard["distanceToNearestEnemy"] = enemyRelativePosition.magnitude;
        blackboard["enemyBehind"] = Mathf.Abs(enemyDirection.x) < 0.1f && enemyDirection.y < 0;
        blackboard["enemyToTheRight"] = enemyDirection.x > 0;

        blackboard["targetDistance"] = targetRelativePosition.magnitude;
        blackboard["targetInFront"] = Mathf.Abs(targetDirection.x) < 0.1f && targetDirection.y > 0;
        blackboard["targetToTheRight"] = targetDirection.x > 0;
    }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }

    public void AddTarget(GameObject target)
    {
        targets.Add(target);
    }

    private Transform ClosestObject(List<GameObject> objects)
    {
        float currentLowestDistance = Mathf.Infinity;
        Transform objectTransform = null;
        foreach (GameObject _object in objects)
        {
            if (_object == null) continue;
            float distance = this.transform.InverseTransformPoint(_object.transform.position).magnitude;
            if (distance < currentLowestDistance)
            {
                currentLowestDistance = distance;
                objectTransform = _object.transform;
            }
        }
        return objectTransform;
    }

    private Root CirclingBehaviour()
    {
        return new Root(new Sequence(
            new Action(() => Turn(1f)),
            new Action(() => Move(1f))
            ));
    }

    private Root EvadingBehaviour()
    {
        return new Root(new Service(() => UpdatePerception(),
                            new Selector(
                                Evade(),
                                new Selector(Persue(),                                
                                    EdgeSearchingBehaviour())
                                )
                            ));
    }

    private Node EdgeSearchingBehaviour()
    {
        return new Selector(
                    TurnAwayFromWall(),
                    TurnTowardsWall(),
                    new Sequence(
                        new Action(() => Turn(0f)),
                        new Action(() => Move(1f)))
                            );
    }

    private Node TurnAwayFromWall()
    {
        return new Selector(
                    new Sequence(
                        new BlackboardCondition("rightWallCloser", Operator.IS_EQUAL, true, Stops.SELF,
                            new BlackboardCondition("wallDistanceOnRight", Operator.IS_SMALLER_OR_EQUAL, wallAvoidDistance, Stops.SELF,
                                new Action(() => Turn(wallAvoidanceFactor)))),
                        new Action(() => Move(0.3f))),
                    new Sequence(
                        new BlackboardCondition("wallDistanceOnLeft", Operator.IS_SMALLER_OR_EQUAL, wallAvoidDistance, Stops.SELF,
                            new Action(() => Turn(-wallAvoidanceFactor))),
                        new Action(() => Move(0.3f))
                        )
                    );
    }

    private Node TurnTowardsWall()
    {
        return new Selector(
                    new Sequence(
                        new BlackboardCondition("rightWallCloser", Operator.IS_EQUAL, true, Stops.SELF,
                            new BlackboardCondition("wallDistanceOnRight", Operator.IS_GREATER_OR_EQUAL, wallHugDistance, Stops.SELF,
                                new Action(() => Turn(-0.3f)))),
                        new Action(() => Move(1f))),
                    new Sequence(
                            new BlackboardCondition("wallDistanceOnLeft", Operator.IS_GREATER_OR_EQUAL, wallHugDistance, Stops.SELF,
                                new Action(() => Turn(0.3f))),
                        new Action(() => Move(1f))
                        )
                    );
    }

    private Node Evade()
    {
        return new BlackboardCondition("distanceToNearestEnemy", Operator.IS_SMALLER_OR_EQUAL, visionDistance, Stops.SELF,
                new Selector(
                    TurnAwayFromWall(),
                    new Sequence(
                        PointAwayFromTarget(),
                        new Action(() => Move(1f)))
                    ));
    }

    private Node PointAwayFromTarget()
    {
        return new Selector(new BlackboardCondition("enemyBehind", Operator.IS_EQUAL, false, Stops.SELF,
                                new Selector(new BlackboardCondition("enemyToTheRight", Operator.IS_EQUAL, true, Stops.SELF,
                                                new Action(() => Turn(1f))),
                                            new Action(() => Turn(-1f)))),
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

    private Node Persue()
    {
        return new BlackboardCondition("targetDistance", Operator.IS_SMALLER_OR_EQUAL, visionDistance, Stops.SELF,
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
