//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using System;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using MRDL.Utility;

namespace MRDL.Receivers
{
    /// <summary>
    /// Receiver that will start an object rotating or rotate by a set amount
    /// </summary>
    public class RotateReceiver : InteractionReceiver, ISourceStateHandler
    {
        [Tooltip("Vector for which relative axis to rotate on")]
        public Vector3 RotateAxis = Vector3.up;

        [Tooltip("Base sensitivity for rotation amount")]
        public float Sensitivity = 5.0f;

        [Tooltip("Curve applied to the rotation input")]
        public AnimationCurve SensitivityCurve;

        private Vector3 m_handOrigin;
        private Vector3 m_curHandPos;
        private float rate;
        private bool m_rotating;
        private IInputSource handInputSource;
        IPointingSource pointing;
        private uint handInputSourceId;

        private FocusDetails m_Focuser;

        protected override void NavigationStarted(GameObject obj, NavigationEventData eventData)
        {
                m_Focuser = (FocusDetails)FocusManager.Instance.TryGetFocusDetails(eventData);
                m_handOrigin = InputHelper.GetHandPos(handInputSource, handInputSourceId);

                FocusManager.Instance.TryGetPointingSource(eventData, out pointing);
                pointing.FocusLocked = true;

                StartCoroutine("RotateObject");
        }

        protected override void NavigationUpdated(GameObject obj, NavigationEventData eventData)
        {
            FocusDetails eventFocus = (FocusDetails)FocusManager.Instance.TryGetFocusDetails(eventData);
            if (m_Focuser.Object == eventFocus.Object )
            {
                m_curHandPos = InputHelper.GetHandPos(handInputSource, handInputSourceId);
            }
        }

        protected override void NavigationCompleted(GameObject obj, NavigationEventData eventData)
        {
            FocusDetails eventFocus = (FocusDetails)FocusManager.Instance.TryGetFocusDetails(eventData);
            if (m_Focuser.Object == eventFocus.Object)
            {
                StopCoroutine("RotateObject");
                pointing.FocusLocked = false;
                m_Focuser = new FocusDetails();
            }
        }

        protected override void NavigationCanceled(GameObject obj, NavigationEventData eventData)
        {
            FocusDetails eventFocus = (FocusDetails)FocusManager.Instance.TryGetFocusDetails(eventData);
            if (m_Focuser.Object == eventFocus.Object)
            {
                StopCoroutine("RotateObject");
                FocusManager.Instance.TryGetPointingSource(eventData, out pointing);
                pointing.FocusLocked = false;
                m_Focuser = new FocusDetails();
            }
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            if (eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.GripPosition))
            {
                handInputSource = eventData.InputSource;
                handInputSourceId = eventData.SourceId;
            }
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            if (eventData.InputSource == handInputSource)
            {
                handInputSource = null;
                handInputSourceId = new uint();
            }
        }

        // Start rotating target object.
        IEnumerator RotateObject()
        {
            while (true)
            {
                Vector3 handDirection = m_handOrigin - m_curHandPos;

                float rateMod = SensitivityCurve != null ? SensitivityCurve.Evaluate(Vector3.Distance(m_curHandPos, m_handOrigin)) : 1;

                rate = Vector3.Dot(handDirection, CameraCache.Main.transform.right);
                rate *= (Sensitivity * rateMod);

                foreach(GameObject targetObject in Targets)
                {
                    targetObject.transform.Rotate(RotateAxis, rate * Time.deltaTime, UnityEngine.Space.World);
                }

                yield return new WaitForEndOfFrame();
            }
        }


    }
}
