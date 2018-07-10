using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAndSlow : MonoBehaviour {

    public Transform Target;
    public AnimationCurve DistanceVersusSpeed;

    private float _initialDistanceToTarget;
    private float _speed;
    private float _distanceToTarget;

    public void Start()
    {
        _initialDistanceToTarget = (Target.position - transform.position).magnitude;
        _speed = 0;
    }

    public void Update()
    {
        // Calculate our distance from target
        Vector3 deltaPosition = Target.position - transform.position;
        _distanceToTarget = deltaPosition.magnitude;

        // Update our speed based on our distance from the target
        _speed = DistanceVersusSpeed.Evaluate((_initialDistanceToTarget - _distanceToTarget) / _initialDistanceToTarget);

        // If we need to move father than we can in this update, then limit how much we move
        if (_distanceToTarget > _speed)
            deltaPosition = deltaPosition.normalized * _speed;

        // Set our position
        transform.position += deltaPosition * _speed * Time.deltaTime;
    }
}