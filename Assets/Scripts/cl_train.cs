using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cl_train : MonoBehaviour
{
    public GameObject upIdentifer;
    public GameObject rearConnector;
    public float forwardAcceleration = 20f;
    public float brakeForce = 30f;
    public float turnRate = 40f;
    public float maxSpeed = 200f;
    public float currentSpeed = 0f;
    public float rotation = 0f;
    Vector3 up;
    Vector3 forward;
    Vector3 horizontal;

    public int[] speedLevels = { 0, 22, 50, 100, 200 };
    int speedState = 0;

    void Start()
    {
        up = upIdentifer.transform.position - transform.position;
        forward = transform.position - rearConnector.transform.position;
        horizontal = Vector3.Cross(up, forward);
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && speedState < speedLevels.Length-1)
        {
            speedState++;
        }
        else if (Input.GetKeyDown(KeyCode.F) && 0<speedState)
        {
            speedState--;
        }

        if (Input.GetKey(KeyCode.A) && 0<currentSpeed)
        {
            transform.Rotate(up, -turnRate * Time.deltaTime, Space.Self);
        }
        else if (Input.GetKey(KeyCode.D) && 0<currentSpeed)
        {
            transform.Rotate(up, turnRate * Time.deltaTime, Space.Self);
        }

        if (Input.GetKey(KeyCode.W) && 0<currentSpeed)
        {
            transform.Rotate(horizontal, turnRate * Time.deltaTime, Space.Self);
        }
        else if (Input.GetKey(KeyCode.S) && 0<currentSpeed)
        {
            transform.Rotate(horizontal, -turnRate * Time.deltaTime, Space.Self);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(forward, turnRate * Time.deltaTime, Space.Self);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(forward, -turnRate * Time.deltaTime, Space.Self);
        }
    }

    void MovePlayer()
    {
        if (currentSpeed > speedLevels[speedState] && currentSpeed > 0f)
        {
            // Apply brake force
            currentSpeed -= brakeForce * Time.deltaTime;
            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;
            }
        }
        else if (currentSpeed < speedLevels[speedState])
        {
            // Apply acceleration
            currentSpeed += forwardAcceleration * Time.deltaTime;
        }
        transform.Translate(Vector3.right * currentSpeed * Time.deltaTime);
    }
}
