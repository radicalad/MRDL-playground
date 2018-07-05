//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using MRDL.Utility;

namespace MRDL.Receivers
{
    /// <summary>
    /// Triggers and event based on the proximity of an object to a location
    /// </summary>
    public class ProximityReceiver : InteractionReceiver, ISourceStateHandler
    {
        [Tooltip("Target object to check proximity against and affect")]
        public GameObject TargetObject;

        #region public members
        public enum SourcePositionEnum
        {
            Head,
            Hand
        }

        [Tooltip("Check for hand or head proximity")]
        public SourcePositionEnum SourceLocation = SourcePositionEnum.Head;

        [Tooltip("Proximity distance to trigger")]
        public float Distance = 2.5f;

        [Tooltip("When true toggle the active state of the target object")]
        public bool ToggleActive = true;
        #endregion

        #region private members
        private bool bInProximity;
        private Vector3 SourcePos;
        private IInputSource handInputSource;
        private uint handInputSourceId;

        #endregion

        public void LateUpdate()
        {
            switch (SourceLocation)
            {
                case SourcePositionEnum.Hand:
                    SourcePos = (handInputSource != null) ? InputHelper.GetHandPos(handInputSource, handInputSourceId) : Vector3.zero;
                    //InputManager.Instance.DetectedInputSources.

                    break;
                case SourcePositionEnum.Head:
                    SourcePos = CameraCache.Main.transform.position;
                    break;
            }
            ProximityCheck();
        }

        // Here we check the proximity to a location
        protected virtual void ProximityCheck()
        {
            if (TargetObject != null)
            {
                float curDistance = Vector3.Distance(SourcePos, TargetObject.transform.position);

                if (!bInProximity && curDistance < Distance)
                {
                    bInProximity = true;
                    OnProximityEnter();
                }
                else if (bInProximity && curDistance > Distance)
                {
                    bInProximity = false;
                    OnProximityExit();
                }
            }
        }

        protected virtual void OnProximityEnter()
        {
            if (ToggleActive)
            {
                foreach (GameObject _bio in interactables)
                {
                    _bio.SetActive(true);
                }
            }
        }

        protected virtual void OnProximityExit()
        {
            if (ToggleActive)
            {
                foreach (GameObject _bio in interactables)
                {
                    _bio.SetActive(false);
				}
            }
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            if( eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.GripPosition) )
            {
                handInputSource = eventData.InputSource;
                handInputSourceId = eventData.SourceId;
            }
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            if(eventData.InputSource == handInputSource)
            {
                handInputSource = null;
                handInputSourceId = new uint();
            }
        }
    }
}