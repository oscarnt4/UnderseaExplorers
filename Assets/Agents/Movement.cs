using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 10f;
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
        Vector2 movement = transform.up * movementAmount * moveSpeed * Time.deltaTime;
        Debug.Log("move amt" + movement);
        _rigidbody.MovePosition(_rigidbody.position + movement);
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
