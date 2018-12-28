using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//самостоятельная реализация расчета столкновений вместо компонента RigidBody2d
//для оценки столкновений используются лучи с начальными точками в объекте

public class Controller2D : RaycastController {//странный синтаксис наследования

	float maxClimbAngle = 80;
    float maxDescendAngle = 80;

    public 

	public CollisionInfo collisions;

    public override void Start()// полиформизм методов в C# отличается от Java
    {
        base.Start();
    }

    public void Move(Vector3 velocity, bool standingOnPlatform = false) {
		UpdateRaycastOrigins ();
		collisions.Reset ();

        if (velocity.y < 0) {
            DescendSlope(ref velocity);
        }
		if (velocity.x != 0) {
			HorizontalCollisions (ref velocity);
		}
		if (velocity.y != 0) {
			VerticalCollisions (ref velocity);
		}

		transform.Translate (velocity);

        if (standingOnPlatform) collisions.below = true;//эту информацию получаем из контроллера платформ.
	}

	void HorizontalCollisions(ref Vector3 velocity) {
		float directionX = Mathf.Sign (velocity.x);
		float rayLength = Mathf.Abs (velocity.x) + skinWidth;
		
		for (int i = 0; i < horizontalRayCount; i ++) {
			Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight;
            //если мы движемся влево, то рисуем лучи столкновений из левой стороны, иначе из правой
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);//лучи рисуются с промежутками
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            //collisionMask - переменная, которая в инспекторе объекта привязывается к слоям

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength,Color.red);
            
            //проверка столкновения луча hit с препятствием:  
            if (hit) {

                if (hit.distance == 0) continue; //пропуск проверки в случае, если опускающаяся платформа проходит через игрока

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);//RaycastHit2D.normal is the normal vector of the surface hit by the ray.
                //угол между нормалом луча (перпендикуляр к встреченной наклонной) и вектором вверх (такой угол равен углу наклонной к горизонту)
                //для вертикального препятствия такой угол будет равен 90
                if (i == 0 && slopeAngle <= maxClimbAngle) { //если для первого(нижнего) луча наклон <= максимальному
					float distanceToSlopeStart = 0;
					if (slopeAngle != collisions.slopeAngleOld) { //без этой поправки объект будет подниматься параллельно склону
						distanceToSlopeStart = hit.distance-skinWidth;
						velocity.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope(ref velocity, slopeAngle);//метод подъёма по наклонной
					velocity.x += distanceToSlopeStart * directionX;
				}

				if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) { //продолжение движения по горизонтали
					//velocity.x = (hit.distance - skinWidth) * directionX;
					//rayLength = hit.distance;
                    velocity.x = Mathf.Min(Mathf.Abs(velocity.x), (hit.distance - skinWidth)) * directionX;
                    rayLength = Mathf.Min(Mathf.Abs(velocity.x) + skinWidth, hit.distance);

                    if (collisions.climbingSlope) {//на случай вертикального препятствия при движении по склону (снижение скорости по Y)
						velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
					}

					collisions.left = directionX == -1; //препятствия
					collisions.right = directionX == 1;
				}
			}
		}
	}
	
	void VerticalCollisions(ref Vector3 velocity) {
		float directionY = Mathf.Sign (velocity.y);
		float rayLength = Mathf.Abs (velocity.y) + skinWidth;

		for (int i = 0; i < verticalRayCount; i ++) {
			Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft:raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength,Color.red);

			if (hit) {
				//velocity.y = (hit.distance - skinWidth) * directionY;
				//rayLength = hit.distance;
                velocity.y = Mathf.Min(Mathf.Abs(velocity.y), (hit.distance - skinWidth)) * directionY;
                rayLength = Mathf.Min(Mathf.Abs(velocity.y) + skinWidth, hit.distance);

                if (collisions.climbingSlope) {//на случай препятствия сверху при движении по склону (снижение скорости по X)
					velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
				}

				collisions.below = directionY == -1; //препятствия
                collisions.above = directionY == 1;
			}
		}

        
        if (collisions.climbingSlope)//переход с одной наклонной на другую
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);//можно вынести построение хитов в отдельный метод?
                if (slopeAngle != collisions.slopeAngle)//на пути новая наклонная
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }

        }
	}

	void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
		float moveDistance = Mathf.Abs (velocity.x);//модуль первоначальной скорости по горизонатали
		float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance; //уменьшение скорости по Y

		if (velocity.y <= climbVelocityY) {//velocity.y будет больше climbVelocity, если мы прыгаем. В прыжке снижение скорости для наклонных нам не нужно
			velocity.y = climbVelocityY;
			velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
			collisions.below = true;//для возможности прыжка
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;//на случай изменения наклона
		}
	}

	void DescendSlope(ref Vector3 velocity) {
		float directionX = Mathf.Sign (velocity.x);
		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

		if (hit) {
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
				if (Mathf.Sign(hit.normal.x) == directionX) {
					if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) {
						float moveDistance = Mathf.Abs(velocity.x);
						float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
						velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (velocity.x);
						velocity.y -= descendVelocityY;

						collisions.slopeAngle = slopeAngle;
						collisions.descendingSlope = true;
						collisions.below = true;
					}
				}
			}
		}
}

	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld; //для более точной реализации движения по наклонной

        public void Reset() {
			above = below = false;
			left = right = false;
			climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
