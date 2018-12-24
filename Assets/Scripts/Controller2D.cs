using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//самостоятельная реализация расчета столкновений вместо компонента RigidBody2d
//для оценки столкновений используются лучи с начальными точками в объекте

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour {

    public LayerMask collisionMask;

    const float skinWidth = .015f;//ширина поверхности объекта
    public int horizontalRayCount = 4;//количество лучей
    public int verticalRayCount = 4;
    float horizontalRaySpacing;//расстояние между лучами
    float verticalRaySpacing;

    BoxCollider2D collider;
    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing(); //расчитываем расстояния между лучами (деление стороны объекта на количество лучей)
    }

    void CalculateRaySpacing() //расчитываем расстояния между лучами
    {
        Bounds bounds = collider.bounds;//получаем границы
        bounds.Expand(skinWidth * -2);//сужение границ на ширину оболочки - т.е. начальные точки лучей лежат немного внутри объекта

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);//ограничиваем возможное количество лучшей между 2 и максимальным int
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1); // интервалы = сторона объекта / количество промежутков (количество лучей - 1)
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1); 
    }

    public void Move(Vector3 velocity) //движение и обработка столкновений
    {
        UpdateRaycastOrigins();//обновление начальных точек лучей
        collisions.Reset();//сброс фиксирования столкновений
        if (velocity.x != 0) HorizontalCollisions(ref velocity);
        if (velocity.y != 0) VerticalCollisions(ref velocity);


        transform.Translate(velocity);
    }

    void VerticalCollisions(ref Vector3 velocity)//ref - передача значения по ссылке, а не по значению
    {
        //направление вверх - положительное значение (sign 1), вниз - отрицательное (sign 2)
        float directionY = Mathf.Sign(velocity.y);//Return value is 1 when f is positive or zero, -1 when f is negative.
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;//модуль вектора вертикальной скорости + ширина поверхности
        //отрисовка лучей, исходящих из стороны объекта:
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            //если мы движемся вниз, то рисуем лучи столкновений из нижней стороны объекта, иначе лучи столкновений из верхней
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);//лучи рисуются с промежутками = длине стороны объекта, разделенной на количество лучей
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            
            //отрисовка лучей для сцены:
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
            
            //проверка столкновения луча hit с препятствием:            
            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;//это ограничивает перемещение лишь на расстояние до столкновения
                rayLength = hit.distance - skinWidth; // это нужно для ситуации, в которой первый луч столкнулся с препятствием-уступом, а следующий луч пройдет мимо уступа
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    void HorizontalCollisions(ref Vector3 velocity)//ref - передача значения по ссылке, а не по значению
    {
        //направление вправо - положительное значение (sign 1), влево - отрицательное (sign 2)
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;//модуль вектора горизонтальной скорости + ширина поверхности
        //отрисовка лучей, исходящих из стороны объекта:
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            //если мы движемся влево, то рисуем лучи столкновений из левой стороны, иначе из правой
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);//лучи рисуются с промежутками
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            //проверка столкновения луча hit с препятствием:            
            if (hit)
            {
                velocity.x = (hit.distance - skinWidth) * directionX;//это ограничивает перемещение лишь на расстояние до столкновения
                rayLength = hit.distance - skinWidth; // это нужно для ситуации, в которой первый луч столкнулся с препятствием-уступом, а следующий луч пройдет мимо уступа
                collisions.left = directionX == -1;
                collisions.right = directionX == 1;
            }
        }
    }

    void UpdateRaycastOrigins() //обновление начальных точек лучей - привязка лучей в объекту
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);//аналогично методу CaltulateRaySpacing
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    struct RaycastOrigins //начальные точки лучей
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }

}
