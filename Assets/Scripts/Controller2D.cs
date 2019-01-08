using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//custom collisions controller instead of RigidBody2d

public class Controller2D : RaycastController {

	public float maxSlopeAngle = 80;

	public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;
    // It is public for camera adjustment.

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
        //overloading method Move() for getting Player's input
    {
		UpdateRaycastOrigins ();
		collisions.Reset ();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
            // Processing slope descension.
        }

        if (moveAmount.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        HorizontalCollisions(ref moveAmount);

        if (moveAmount.y != 0)
        {
			VerticalCollisions (ref moveAmount);
		}

		transform.Translate (moveAmount);

        if (standingOnPlatform)
        {
            collisions.below = true;
            // This way a jump is possible from a slope.
        }
            
	}

	void HorizontalCollisions(ref Vector2 moveAmount) {
		float directionX = collisions.faceDir;
		float rayLength = Mathf.Abs (moveAmount.x) + skinWidth;

        if (Mathf.Abs(moveAmount.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
            //Jumping directrly up next to the wall makes Player slide down
        }
		
		for (int i = 0; i < horizontalRayCount; i ++) {
			Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
            // Sending collision rays.
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            // collisionMask variable is being set in the Inspector window.

            Debug.DrawRay(rayOrigin, Vector2.right * directionX,Color.red);
            
            if (hit) {

                if (hit.distance == 0) continue; 
                // Skiping the check if the descending platform moves through Player.

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                // RaycastHit2D.normal is the vector perpendicular to the surface hit by the ray.
                // slopeAngle is the angle between the normal to the slope and a vector being pointed directly up.
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    // Checking this only for the first collision ray.
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }

                    float distanceToSlopeStart = 0;
					if (slopeAngle != collisions.slopeAngleOld) { 
                        // A minor fix to avoid ascending prematurely.
						distanceToSlopeStart = hit.distance-skinWidth;
						moveAmount.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
					moveAmount.x += distanceToSlopeStart * directionX;
				}

				if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                { 
                    moveAmount.x = Mathf.Min(Mathf.Abs(moveAmount.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(moveAmount.x) + skinWidth, hit.distance);

                    if (collisions.climbingSlope)
                        // There is an obstacle on the slope.
                    {
						moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
					}
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}
	
	void VerticalCollisions(ref Vector2 moveAmount) {
		float directionY = Mathf.Sign (moveAmount.y);
		float rayLength = Mathf.Abs (moveAmount.y) + skinWidth;

		for (int i = 0; i < verticalRayCount; i ++) {
			Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY,Color.red);

			if (hit) {
                // Ability to move vertically through specific obtacles marked with "Through" tag (NES "Contra" style).
                if (hit.collider.tag == "Through")
                {
                    // Jumping through the platform above.
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    // Jumping down through the platform.
                    if (collisions.fallingThroughPlatfom)
                    {
                        continue;
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatfom = true;
                        Invoke("ResetFallingThroughPlatform", .5f);
                        continue;
                    }

                }
                
                moveAmount.y = Mathf.Min(Mathf.Abs(moveAmount.y), (hit.distance - skinWidth)) * directionY;
                rayLength = Mathf.Min(Mathf.Abs(moveAmount.y) + skinWidth, hit.distance);

                if (collisions.climbingSlope)
                    //there is an obstacle above while Player is on the slope
                {
					moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
				}

				collisions.below = directionY == -1;
                collisions.above = directionY == 1;
			}
		}

        
        if (collisions.climbingSlope)
            //clibming from one slope to another
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                    //a new slope after current one
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }

        }
	}

	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
		float moveDistance = Mathf.Abs (moveAmount.x);
		float climbmoveAmountY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (moveAmount.y <= climbmoveAmountY) {
            //moveAmount.y > climbmoveAmount if Player jumps.
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (moveAmount.x);
			collisions.below = true;
            //making jumping from the slope possible
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
	}

	void DescendSlope(ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        if (maxSlopeHitLeft ^ maxSlopeHitRight)
            // If only one condition is met.
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }


        if (!collisions.slidingDownMaxSlope)
            // Checking for a normal descending.
        {
            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                        {
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendmoveAmountY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
                // Tan(angle) = (movement.y - hitDst) / movement.x -> movement.x = (movement.y - hitDst) / Tan(angle).
                // Mathf.Sign(hit.normal.x) for determining the direction.

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }

    }

    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatfom = false;
    }

	public struct CollisionInfo
    {
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
        public bool descendingSlope;
        public bool slidingDownMaxSlope;

        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;
        public Vector2 moveAmountOld;
        public bool fallingThroughPlatfom;
        public int faceDir;

        public void Reset()
        {
			above = below = false;
			left = right = false;
			climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero;

            slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
