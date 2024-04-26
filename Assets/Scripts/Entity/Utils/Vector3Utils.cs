using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Utils {
    // Retrieved from: https://forum.unity.com/threads/quaternion-smoothdamp.793533/
    public static Vector3 SmoothDampEuler(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity) {
        if (Time.deltaTime == 0) return current;
        if (smoothTime == 0) return current;

        return new Vector3(
            Mathf.SmoothDampAngle(current.x, target.x, ref currentVelocity.x, smoothTime, maxSpeed: maxSpeed),
            Mathf.SmoothDampAngle(current.y, target.y, ref currentVelocity.y, smoothTime, maxSpeed: maxSpeed),
            Mathf.SmoothDampAngle(current.z, target.z, ref currentVelocity.z, smoothTime, maxSpeed: maxSpeed)
        );
    }
}

