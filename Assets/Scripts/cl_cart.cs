using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public class cl_cart : MonoBehaviour
{
    public Transform frontConnector;
    public Transform backConnector; 
    public Transform targetCartRearPoint;
    public cl_train driveTrain;
    public float lerpSpeed = 10f;
    public float rotationSpeed = 10f;
    public float offset = 6f;
    public float minimumDistance = 0.5f;
    public float maxDistance = 1f;
    void Start()
    {
        if (targetCartRearPoint == null)
        {
            Debug.LogWarning("Target is not assigned!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (targetCartRearPoint != null && 
            minimumDistance < Vector3.Magnitude(frontConnector.transform.position - targetCartRearPoint.transform.position))
        {
            RotateTowardsTarget();
            Vector3 directionToFrontPoint = (targetCartRearPoint.position - frontConnector.position);
            if (maxDistance < directionToFrontPoint.magnitude)
            {
                Vector3 targetPosition = transform.position + (directionToFrontPoint.normalized * offset);
                transform.position = Vector3.Slerp(transform.position, targetPosition, driveTrain.currentSpeed*Time.deltaTime);
            }
        }
    }
    void RotateTowardsTarget()
    {
        Vector3 forward = frontConnector.position - transform.position;
        Vector3 directionToTarget = (targetCartRearPoint.position - backConnector.position).normalized;
        Vector3 axis = Vector3.Cross(forward, directionToTarget);

        float radians = Mathf.Acos(Vector3.Dot(forward, directionToTarget) /
                    forward.magnitude / directionToTarget.magnitude);
        Quaternion rotationAmmount = 
            Quaternion.AngleAxis(Mathf.Rad2Deg*radians+90, axis);

        if (0.005f < radians)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationAmmount * transform.rotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 v = targetCartRearPoint.transform.position - transform.position;
        Vector3 r = Quaternion.LookRotation(v, targetCartRearPoint.transform.up).eulerAngles;
        Vector3 s = Quaternion.LookRotation(v, transform.up).eulerAngles;
        float d = r.z - s.z;
        if (d < 0)
        {
            d += 360f;
        }
        if (180f < d)
        {
            d -= 360f;
        }
        transform.Rotate(v, d * Time.deltaTime * rotationSpeed, Space.World);
    }
}
