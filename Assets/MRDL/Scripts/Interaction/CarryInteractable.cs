//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using MRDL.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MRDL.Interaction
{
    /// <summary>
    /// This is a simple interactible for toggling other interactible components.
    /// </summary>
    public class CarryInteractiable : MonoBehaviour, IInputClickHandler, IHoldHandler, ISourcePositionHandler
    {
        public enum AttachTargetEnum
        {
            Cursor,
            Hand
        }

        public enum AttachInteractionEnum
        {
            PressAndHold,
            Tapped,
            //DoubleTapped
        }

        [Tooltip("Attachment target type")]
        public AttachTargetEnum m_AttachType = AttachTargetEnum.Cursor;

        [Tooltip("Interaction type to init and stop carry")]
        public AttachInteractionEnum m_InteractionType = AttachInteractionEnum.PressAndHold;

        [Tooltip("Time to lerp to cursor position")]
        public float m_PositionLerpTime = 0.2f;

        [Tooltip("Time to lerp to cursor rotation")]
        public float m_RotationLerpTime = 0.2f;

        [Tooltip("Create parent for all targets and reparent after")]
        public bool m_ParentObjects = false;

        private bool m_bAttached = false;

        private Vector3 m_CurrentHandPos = Vector3.zero;
        private Vector3 m_HandLastPosition = Vector3.zero;
        private float m_HandDeadzone = 0.00001f;

        private FocusDetails m_InteractingFocus;
        private IInputSource currentInputSource;
        private uint currentInputSourceId;

        public void OnEnable()
        {
            if (m_AttachType == AttachTargetEnum.Hand)
            {
                //InputSources.Instance.hands.OnHandMoved += OnHandMoved;
            }
        }

        public void OnDisable()
        {
            if (m_AttachType == AttachTargetEnum.Hand)
            {
                //InputSources.Instance.hands.OnHandMoved -= OnHandMoved;
            }
        }

        public void OnPositionChanged(SourcePositionEventData eventData)
        {
            m_CurrentHandPos = InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

            if (m_AttachType == AttachTargetEnum.Hand && Vector3.Distance(m_HandLastPosition, m_CurrentHandPos) >= m_HandDeadzone)
            {
                m_HandLastPosition = m_CurrentHandPos;
            }
        }

        /// <summary>
        /// On Hold start event for attaching objects if it's the correct option
        /// </summary>
        /// <param name="eventArgs"></param>
        public void OnHoldStarted(HoldEventData eventArgs)
        {
            if (m_InteractionType == AttachInteractionEnum.PressAndHold && m_InteractingFocus.Object == null)
            {

                currentInputSource = eventArgs.InputSource;
                currentInputSourceId = eventArgs.SourceId;

                IPointingSource pointing;
                if (FocusManager.Instance.TryGetSinglePointer(out pointing))
                {
                    AttachObject(pointing);
                }
            }
        }

        /// <summary>
        /// On Completed hold 
        /// </summary>
        /// <param name="eventArgs"></param>
        public void OnHoldCompleted(HoldEventData eventArgs)
        {
            if (m_InteractionType == AttachInteractionEnum.PressAndHold && m_bAttached && m_InteractingFocus.Object == eventArgs.selectedObject)
            {
                DetachObject();
            }
        }

        /// <summary>
        /// On hold cancel detach objects
        /// </summary>
        /// <param name="eventArgs"></param>
        public void OnHoldCanceled(HoldEventData eventArgs)
        {
            if (m_InteractionType == AttachInteractionEnum.PressAndHold && m_bAttached && m_InteractingFocus.Object == eventArgs.selectedObject)
            {
                DetachObject();
            }
        }

        /// <summary>
        /// On tapped if tapped interaction type then attach or detach
        /// </summary>
        /// <param name="eventArgs"></param>
        public void OnInputClicked(InputClickedEventData eventArgs)
        {
            if (m_InteractionType == AttachInteractionEnum.Tapped)
            {
                eventArgs.Use();

                currentInputSource = eventArgs.InputSource;
                currentInputSourceId = eventArgs.SourceId;


                IPointingSource pointing;
                if (FocusManager.Instance.TryGetPointingSource(eventArgs, out pointing))
                {
                    if (m_bAttached && m_InteractingFocus.Object == eventArgs.selectedObject)
                    {
                        DetachObject();
                    }
                    else if (m_InteractingFocus.Object == null)
                    {                        
                        AttachObject(pointing);
                    }
                }
            }
        }

        /*
        /// <summary>
        /// On double tapped if double tapped interaction type then attach or detach
        /// </summary>
         /// <param name="eventArgs"></param>
        protected void OnDoubleTapped(InputEventData eventArgs)
        {
            if (m_InteractionType == AttachInteractionEnum.DoubleTapped)
            {
                if (m_bAttached && m_InteractingFocus == eventArgs.Focuser)
                {
					m_InteractingFocus.ReleaseFocus();
                    DetachObject();
                }
                else if (m_InteractingFocus == null)
                {
					eventArgs.Focuser.LockFocus();
                    AttachObject(eventArgs.Focuser);
                }
            }
        }
        */
        private void AttachObject(IPointingSource pointer)
        {
            m_InteractingFocus = FocusManager.Instance.GetFocusDetails(pointer);
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            StartCoroutine("CarryObject");
            m_bAttached = true;
        }

        private void DetachObject()
        {
            StopCoroutine("CarryObject");
            gameObject.layer = LayerMask.NameToLayer("Default");
            m_bAttached = false;

            currentInputSource = null;
            currentInputSourceId = 0;
            m_InteractingFocus = new FocusDetails();
        }

        // Start rotating target object.
        public IEnumerator CarryObject()
        {
            while (true)
            {
                Vector3 curPos = m_AttachType == AttachTargetEnum.Cursor ? m_InteractingFocus.Point : m_HandLastPosition;
                Quaternion curRot = m_AttachType == AttachTargetEnum.Cursor ? Quaternion.LookRotation(m_InteractingFocus.Normal) : CameraCache.Main.transform.rotation;

                this.transform.position = Vector3.Lerp(this.transform.position, curPos, Time.deltaTime / m_PositionLerpTime);
                this.transform.rotation = Quaternion.Lerp(this.transform.rotation, curRot, Time.deltaTime / m_RotationLerpTime);

                yield return new WaitForEndOfFrame();
            }
        }

    }
}