//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using MRDL.Utility;

namespace MRDL.Interaction
{
    /// <summary>
    /// ArcBall Interactible is a simple interaction that uses a sphere collider for rotation.
    /// A sphere collider is used for the initial interaction point and hand offset relative 
    /// rotates that objec.
    /// </summary>
    public class ArcBallInteractable : MonoBehaviour, ToggleInteractable.IToggleable, IHoldHandler, IFocusable
    {
        [Tooltip("Dampening on movement displacement")]
        public float damping = 0.9f;

        [Tooltip("Speed for rotation")]
        public float speed = 1.0f;

        [Tooltip("Sensitivity for offset and displacement")]
        public float sensitivity = 4.0f;

        [Tooltip("Filter relative directions by setting to 0.0")]
        public bool magnetism = true;

        private Vector3 vDown;
        private Vector3 vDrag;

        private bool bDragging;
        private bool bSelected;

        private float angularVelocity;
        private Vector3 rotationAxis;
        private Vector3 handPivot;
        private Vector3 handPos;

        private Vector3[] directions = 
                    {
                        Vector3.up,
                        Vector3.forward,
                        Vector3.back,
                        Vector3.right,
                        Vector3.left,
                        Vector3.down
                    };

        private IInputSource currentInputSource;
        private uint currentInputSourceId;


        void Start()
        {
            bDragging = false;
            angularVelocity = 0;
            rotationAxis = Vector3.zero;
        }

        void Update()
        {

            if (bSelected)
            {
                handPos = InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

                if (!bDragging)
                {
                    // extract vDown from the RaycastHit
                    vDown = handPos - handPivot;

                    // start dragging
                    bDragging = true;
                }
                else
                {
                    // extract vDrag from the RaycastHit
                    vDrag = handPos - handPivot;

                    // compute the rotation axis and angular velocity from vDown and vDrag
                    rotationAxis = Vector3.Cross(vDrag, vDown);
                    angularVelocity = Vector3.Angle(vDrag, vDown) * speed;
                }
            }

            // Support magnetism if 
            if (magnetism && !bSelected)
            {
                Vector3 angle = transform.rotation.eulerAngles;
                for (int i = 0; i < directions.Length; i++)
                {
                    if (Vector3.Angle(directions[i], angle) < 30.0f)
                    {
                        rotationAxis = Vector3.Cross(directions[i], angle);
                        angularVelocity = Vector3.Angle(directions[i], angle) * speed;
                    }
                }
            }

            // on mouse up stop dragging
            if (!bSelected)
                bDragging = false;

            // apply the angular velocity
            if (angularVelocity > 0)
            {
                transform.Rotate(rotationAxis, angularVelocity * Time.deltaTime, UnityEngine.Space.World);
                angularVelocity = (angularVelocity > 0.01f) ? angularVelocity * damping : 0;
            }
        }

        public void OnHoldStarted(HoldEventData e)
        {
            // Get the location of the cursor on the sphere collider

            currentInputSource = e.InputSource;
            currentInputSourceId = e.SourceId;

            IPointingSource pointing;
            if (FocusManager.Instance.TryGetSinglePointer(out pointing))
            {
                RaycastHit hitInfo;
                Ray gazeRay = new Ray(pointing.Rays[0].Origin, pointing.Rays[0].Direction);

                if (Physics.Raycast(gazeRay, out hitInfo))
                {
                    handPos = InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

                    Vector3 hitPos = hitInfo.point;
                    Vector3 handOffset = Vector3.Normalize(hitPos - transform.position);
                    float val = 1.0f / sensitivity;

                    handOffset.Scale(new Vector3(val, val, val));
                    handPivot = handPos + handOffset;
                }

                bSelected = true;
            }
		}

        public void OnHoldCompleted(HoldEventData e)
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            bSelected = false;
		}

        public void OnHoldCanceled(HoldEventData e)
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            bSelected = false;
		}

        public void OnFocusExit()
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            bSelected = false;
        }

        public void OnFocusEnter() { /*Unused*/ }

    }
}
