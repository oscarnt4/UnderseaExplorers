using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DiverMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 180f;
    public int stationaryDuration = 3;
    public int movementDuration = 3;

    private float movementAmount;
    private float turnAmount;
    private Rigidbody2D _rigidbody;
    private float storedMovement;
    private int updateCount;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        updateCount = new System.Random().Next(0, stationaryDuration + movementDuration);
    }

    void Start()
    {

    }

    void FixedUpdate()
    {
        Move();
        Turn();
    }

    private void Move()
    {
        updateCount++;
        if (updateCount > stationaryDuration && updateCount <= stationaryDuration + movementDuration)
        {
            _rigidbody.velocity = transform.up * (storedMovement / movementDuration);
        }
        else
        {
            storedMovement += movementAmount * moveSpeed;
            _rigidbody.velocity = Vector2.zero;
        }
        if (updateCount >= stationaryDuration + movementDuration)
        {
            updateCount = 0;
            storedMovement = 0f;
        }
    }

    private void Turn()
    {
        float rotation = _rigidbody.rotation + turnAmount * turnSpeed * Time.deltaTime;
        _rigidbody.MoveRotation(rotation);
    }

    public void MoveBehaviour(float amount)
    {
        movementAmount = (amount > 1) ? 1 : (amount < -1) ? -1 : amount;
    }

    public void TurnBehaviour(float amount)
    {
        turnAmount = (amount > 1) ? 1 : (amount < -1) ? -1 : amount;
    }
}
