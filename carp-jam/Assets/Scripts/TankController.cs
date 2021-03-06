using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController))]
public partial class TankController : MonoBehaviour
{
   public enum MoveDirection { Forward, Reverse, Stop }
 
    [Header ("Motion")]
 
    [SerializeField] private float topForwardSpeed = 3.0f;
    [SerializeField] private float topReverseSpeed = 1.0f;
 
    [Tooltip ("Rate at which top speed is reached.")]
    [SerializeField] private float acceleration = 3.0f;
 
    [Tooltip ("Rate at which speed is lost when not accelerating.")]
    [SerializeField] private float deceleration = 2.0f;
 
    [Tooltip ("Rate at which speed is lost when braking (input opposite of current direction).")]
    [SerializeField] private float brakingDeceleration = 4.0f;
 
    [Tooltip ("Maintain speed when no input is provided.")]
    [SerializeField] private bool stickyThrottle = false;
 
    [Tooltip ("Delay between change of direction when sticky throttle is enabled.")]
    [SerializeField] private float stickyThrottleDelay = 0.35f;
 
    [SerializeField] private float gravity = 10.0f;
 
    [Header ("Rotation")]
 
    [SerializeField] private float topForwardTurnRate = 1.0f;
    [SerializeField] private float topReverseTurnRate = 2.0f;
    [SerializeField] private float stoppedTurnRate = 2.0f;
 
    private float currentSpeed;
    private float currentTopSpeed;
    private MoveDirection currentDirection = MoveDirection.Stop;
    private bool isBraking = false;
    private bool isAccelerating = false;
    private float stickyDelayCount = 9999f;
    private CharacterController m_Controller;
    private Transform m_Transform;
 
 
    private void Awake ()
    {
        // Performance optimization - cache transform reference.
        m_Transform = GetComponent<Transform> ();
 
        m_Controller = GetComponent<CharacterController> ();
        currentTopSpeed = topForwardSpeed;
    }
 
 
    private void FixedUpdate ()
    {
        // Direction to move this update.
        Vector3 moveDirection = Vector3.zero;
 
        // Direction requested this update.
        MoveDirection requestedDirection = MoveDirection.Stop;
 
        if (m_Controller.isGrounded)
        {
            // Simulate loss of turn rate at speed.
            float currentTurnRate = Mathf.Lerp (currentDirection == MoveDirection.Forward ?
                topForwardTurnRate : topReverseTurnRate, stoppedTurnRate, 1 - (currentSpeed / currentTopSpeed));
 
            Vector3 angles = m_Transform.eulerAngles;
            angles.y += (Input.GetAxis ("Horizontal") * currentTurnRate);
            m_Transform.eulerAngles = angles;
 
            // Based on input, determine requested action.
            if (Input.GetAxis ("Vertical") > 0) // Requesting forward.
            {
                requestedDirection = MoveDirection.Forward;
                isAccelerating = true;
            }
            else
            {
                if (Input.GetAxis ("Vertical") < 0) // Requesting reverse.
                {
                    requestedDirection = MoveDirection.Reverse;
                    isAccelerating = true;
                }
                else
                {
                    requestedDirection = currentDirection;
                    isAccelerating = false;
                }
            }
 
            isBraking = false;
 
            if (currentDirection == MoveDirection.Stop)
            {
                stickyDelayCount = stickyDelayCount + Time.deltaTime;
 
                // If we are not sticky throttle or if we have hit the delay then change direction.
                if (!stickyThrottle || (stickyDelayCount > stickyThrottleDelay))
                {
                    // Make sure we can go in the requested direction.
                    if (((requestedDirection == MoveDirection.Reverse) && (topReverseSpeed > 0))
                        || ((requestedDirection == MoveDirection.Forward) && (topForwardSpeed > 0)))
                    {
                        currentDirection = requestedDirection;
                    }
                }
            }
            else
            {
                // Requesting a change of direction, but not stopped so we are braking.
                if (currentDirection != requestedDirection)
                {
                    isBraking = true;
                    isAccelerating = false;
                }
            }
 
            // Setup top speeds and move direction.
            if (currentDirection == MoveDirection.Forward)
            {
                moveDirection = Vector3.forward;
                currentTopSpeed = topForwardSpeed;
            }
            else
            {
                if (currentDirection == MoveDirection.Reverse)
                {
                    moveDirection = -1 * Vector3.forward;
                    currentTopSpeed = topReverseSpeed;
                }
                else
                {
                    if (currentDirection == MoveDirection.Stop)
                    {
                        moveDirection = Vector3.zero;
                    }
                }
            }
 
            if (isAccelerating)
            {
                // If we haven't hit top speed yet, accelerate.
                if (currentSpeed < currentTopSpeed)
                {
                    currentSpeed = currentSpeed + (acceleration * Time.deltaTime);
                }
            }
            else
            {
                // If we are not accelerating and still have some speed, decelerate.
                if (currentSpeed > 0)
                {
                    // Adjust deceleration for braking and implement sticky throttle.
                    float currentDecelerationRate = isBraking ? brakingDeceleration : (!stickyThrottle ? deceleration : 0);
                    currentSpeed = currentSpeed - (currentDecelerationRate * Time.deltaTime);
                }
            }
 
            // If our speed is below zero, stop and initialize.
            if ((currentSpeed < 0) || ((currentSpeed == 0) && (currentDirection != MoveDirection.Stop)))
            {
                SetStopped ();
            }
            else
            {
                // Limit the speed to the current top speed.
                if (currentSpeed > currentTopSpeed)
                {
                    currentSpeed = currentTopSpeed;
                }
            }
 
            moveDirection = m_Transform.TransformDirection (moveDirection);
        }
 
        // Implement gravity so we can stay grounded.
        moveDirection.y = moveDirection.y - (gravity * Time.deltaTime);
 
        moveDirection.z = moveDirection.z * (Time.deltaTime * currentSpeed);
        moveDirection.x = moveDirection.x * (Time.deltaTime * currentSpeed);
        m_Controller.Move (moveDirection);
    }
 
 
    private void SetStopped ()
    {
        currentSpeed = 0;
        currentDirection = MoveDirection.Stop;
        isAccelerating = false;
        isBraking = false;
        stickyDelayCount = 0;
    }
}
