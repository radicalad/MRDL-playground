using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.Receivers;

public class InputRoutingTest : InteractionReceiver {

    public bool UseInput = false;
    public float DragTimeThreshold = 0.22f;
    private float InitialPressTime;


    protected override void InputDown(GameObject go, InputEventData eventData)
    {
        InitialPressTime = Time.time;
        if (UseInput)
        {
            eventData.Use();
            Debug.Log("Box collider using input");
        }

        Debug.Log(eventData.used);
    }

    protected override void InputUp(GameObject go, InputEventData eventData)
    {
        if ( DragTimeTest(InitialPressTime, Time.time, DragTimeThreshold) )
        {
            eventData.Use();
            Debug.Log("Receiver caught up event");
        }
        else
        {
            eventData.selectedObject.GetComponent<IInputHandler>().OnInputUp(eventData);

            IInputHandler[] handlers = eventData.selectedObject.GetComponentsInChildren<IInputHandler>();
            foreach (IInputHandler iH in handlers)
            {
                iH.OnInputUp(eventData);
            }
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}



    private bool DragTimeTest(float initTime, float currTime, float pressMargin)
    {
        if (currTime - initTime > pressMargin)
        {
            return true;
        }
        return false;
    }

}
