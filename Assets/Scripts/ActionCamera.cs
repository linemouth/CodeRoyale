using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ActionCamera : MonoBehaviour
{
    public Boat Subject { get; set; }
    [Range(0, 1)]
    public float focus = 0.5f; // 0 = focus on the turret, 1 = focus on the hull 
    [Range(0.1f, 10)]
    public float snappiness = 1;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void FixedUpdate()
    {
        if(Subject != null)
        {
            // Get a good position from which to watch the turret.
            Vector3 turretTargetPosition = Subject.turret.position - 25 * Subject.turret.forward + 15 * Vector3.up;
            Quaternion turretTargetRotation = Subject.turret.rotation * Quaternion.Euler(20, 0, 0);

            // Get a good position from which to watch the hull.
            Vector3 hullTargetPosition = Subject.transform.position - 25 * Subject.transform.forward + 15 * Vector3.up;
            Quaternion hullTargetRotation = Subject.transform.rotation * Quaternion.Euler(20, 0, 0);

            // Focus on a view somewhere between the turret and hull.
            targetPosition = Vector3.Lerp(turretTargetPosition, hullTargetPosition, focus);
            targetRotation = Quaternion.Slerp(turretTargetRotation, hullTargetRotation, focus);
        }

        // Smoothly move to the target orientation.
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * snappiness);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * snappiness);
    }
}
