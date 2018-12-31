using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Controller2D target;
    //параметры камеры задаются в инспекторе
    public Vector2 focusAreaSize;//размер зоны фокуса, перемещение игрока в пределах которой не вызывает перемещения камеры
    public float verticalOffset;//вертикальный отступ (чуть выше центра объекта Игрока)
    public float lookAheadDstX;//расстояние упреждения
    public float lookSmoothTime;//сглаживание движения камеры
    public float verticalSmoothTime;//сглаживание движения камеры, для длинных падений/прыжков брать равным 0

    FocusArea focusArea;

    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;


    void Start()
    {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);//отображние зоны фокуса
    }

    void LateUpdate() //метод, вызываемый последним в текущем кадре
    {
        focusArea.Update(target.collider.bounds);
        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
        }
        targetLookAheadX = lookAheadDirX * lookAheadDstX;
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTime); 
        //Gradually changes a value towards a desired goal over time.

        transform.position = (Vector3)focusPosition + Vector3.forward * -10;
    }

    struct FocusArea
    {
        public Vector2 centre;
        public Vector2 velocity;
        float left, right;
        float top, bottom;


        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        public void Update(Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }
            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }
            top += shiftY;
            bottom += shiftY;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(shiftX, shiftY);//скорость перемещения зоны фокуса
        }
    }
}
