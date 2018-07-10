using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
public class AccelerateAndMove : MonoBehaviour, IInputHandler {

    //this is our target velocity while decelerating
    private float initialVelocity = 0.0f;

    //this is our target velocity while accelerating
    float finalVelocity = 0.25f;

    //this is our current velocity
    float currentVelocity = 0.0f;

    //this is the velocity we add each second while accelerating
    float accelerationRate = 0.1f;

    //this is the velocity we subtract each second while decelerating  
    float decelerationRate = 0.1f;
    bool ispressed = false;

    float maxNum = 30f;
    float minNum = 0f;
    public void OnInputDown(InputEventData eventData)
    {
        ispressed = true;
    }

    public void OnInputUp(InputEventData eventData)
    {
        ispressed = false;
    }


    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (ispressed)
        {
            currentVelocity += accelerationRate * Time.deltaTime;
            //Vector3 newPos = Vector3.zero;
            //newPos.y = currentVelocity;
            //transform.position += newPos;

            transform.position = Vector3.MoveTowards(this.transform.position, new Vector3(0f, maxNum, 0f), currentVelocity);


        }
        /*else
        {
            //subtract from the current velocity while decelerating
            currentVelocity -= decelerationRate * Time.deltaTime;

            if(currentVelocity > 0)
            {
                Vector3 newPos = Vector3.zero;
                newPos.y = currentVelocity;
                transform.position -= newPos;
            }
            else
            {
                transform.Translate(0, 0, 0);
            }
        }*/

        //ensure the velocity never goes out of the initial/final boundaries
        currentVelocity = Mathf.Clamp(currentVelocity, initialVelocity, finalVelocity);
    }
}