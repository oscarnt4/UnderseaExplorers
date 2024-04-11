using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MermaidMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    private float movementAmount;
    private float turnAmount;
    private Rigidbody2D _rigidbody;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
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
        _rigidbody.velocity = transform.up * movementAmount * moveSpeed;
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
