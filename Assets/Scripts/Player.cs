using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent (typeof (Controller2D))] 
//required component is mandatory
public class Player : MonoBehaviour {

    Controller2D controller;
    Vector3 velocity;
    Vector2 directionalInput;
    
    public float maxJumpHeight = 3.5f;
    public float minJumpHeight = 1;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 6;

    public Vector2 wallJumpClimb; 
    // recomended x = 7.5 y = 16
    public Vector2 wallJumpOff;
    // recomended x = 8.5 y = 7
    public Vector2 wallLeap;
    // recomended x = 18 y = 17

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;
    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    float velocityXSmoothing;

    bool wallSliding;
    int wallDirX;

    void Start()
    {
        controller = GetComponent<Controller2D>();
        // distance = vel.init * time * (acceleration * time^2)/2
        // gravity = (jump height * 2)/time^2
        // velocity = vel.init (zero) + gravity* time
        gravity = -(maxJumpHeight * 2) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        // vel.final^2 = vel.init^2 + 2 * acceleration * distance
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
    }

    void Update()
    {
        CalculateVelocity();
        HandleWallSliding();
        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y = controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
                // controller.collisions.slopeNormal.y gives us direction. Sliding down is being accelerated.
            }
            else
            {
                velocity.y = 0;
                // Regular obstacle above or below.
            }
        }
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        // Walljumping
        if (wallSliding)
        {
            if (wallDirX == directionalInput.x)
            // Jumping up the wall
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0)
            // Jumping down from the wall
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else
            {
                // Horizontal jumping from the wall
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.x;
            }
        }

        // Regular jumping
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
                // Jumping from unclimbable slope
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                // Jumping not against unclimbable slope
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
                // Jumping from horizontal platform or climbable slope
            }
        }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;
            if (velocity.y < -wallSlideSpeedMax) velocity.y = -wallSlideSpeedMax;
            // Player is falling next to the wall

            if (timeToWallUnstick > 0)
            {
                velocity.x = 0;
                velocityXSmoothing = 0;
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                // Player holds the wall
                {

                    timeToWallUnstick -= Time.deltaTime;
                    // Countdown to slide down
                }
                else timeToWallUnstick = wallStickTime;
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }
    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);//Gradually changes a value towards a desired goal over time.
        velocity.y += gravity * Time.deltaTime;
    }


}
