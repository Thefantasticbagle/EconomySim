using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cl_connector : MonoBehaviour
{
    public GameObject connectedTo;
    private float rotationSpeed = 0.5f;
    private float offset = 2f;

    void FixedUpdate()
    {
        if (connectedTo != null && transform.parent != null)
        {
            //transform.position = connectedTo.transform.position;
            //RotateCart();
        }
    }

    void RotateCart()
    {
        transform.parent.position = Vector3.MoveTowards(transform.parent.position, connectedTo.transform.position, 10f);
        transform.parent.rotation = Quaternion.RotateTowards(transform.parent.rotation, connectedTo.transform.rotation, rotationSpeed);
    }
}
