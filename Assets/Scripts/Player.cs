﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent (typeof (Controller2D))] 
//хорошая практика - требовать наличия компонента у объекта, с которым соотнесен данный скрипт
//более того, требуемые скрипты нельзя удалить у объекта, пока не удалён требующий эти компоненты компонент
public class Player : MonoBehaviour {

    Controller2D controller;

    Vector3 velocity;
    float moveSpeed = 6;

    public float jumpHeight = 3.5f;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;

    float gravity;
    float jumpVelocity;


    float velocityXSmoothing;

    void Start()
    {
        controller = GetComponent<Controller2D>();
        gravity = -(jumpHeight * 2) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print("Gravity: " + gravity + " Jump Velocity: " + jumpVelocity);
    }

    void Update()
    {
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;//пока объект игрока на горизонтальной повехрности, ускорение падения не увеличивается
        }
        //вектор от управления игроком:
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below) //прыжок возможен только с горизонтальной поверхности
        {
            velocity.y = jumpVelocity;
        }
        //добавление притяжения:
        //velocity.x = input.x * moveSpeed;
        float targetVelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)? accelerationTimeGrounded : accelerationTimeAirborne);//Gradually changes a value towards a desired goal over time.
        //эта строчка делает изменение скорости постепенным, причем в воздухе и на земле скорость по оси x меняется немного по-разному
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }


}