//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using MRDL.Utility;

namespace MRDL.Interaction
{
    /// <summary>
    /// The displacement interactible is an interactible that is 
    /// triggered when a threshold is hit.  The threshold is an 
    /// absolute value in one or more axis within a given period of time
    /// </summary>

    public class DisplacementInteractable : MonoBehaviour, ToggleInteractable.IToggleable, IHoldHandler, IFocusable
    {
        #region public accessors
        public enum DisplacementEnum
        {
            Any,
            Push,
            Pull,
            PushPull,
            Up,
            Down,
            UpDown,
            Right,
            Left,
            RightLeft
        }

        [Tooltip("Spatial type of displacement")]
        public DisplacementEnum DisplacementType = DisplacementEnum.Any;

        [Tooltip("Required displacement amount within the displacement time")]
        public float DisplacementAmount = 0.2f;

        [Tooltip("Time required for displacement")]
        public float DisplacementTime = 1.5f;

        public delegate void OnDisplacement(GameObject go);
        public OnDisplacement OnDisplaced;
        #endregion

        #region private vars
        private Vector3 m_HandStartPos;
        private Vector3 m_HandEndPos;
        private bool m_IsDisplaced;
        private IInputSource currentInputSource;
        private uint currentInputSourceId;

        #endregion

        private Coroutine m_DisplacementCheckCoroutine = null;

        public void OnHoldStarted(HoldEventData eventData)
		{
            currentInputSource = eventData.InputSource;
            currentInputSourceId = eventData.SourceId;
            m_DisplacementCheckCoroutine = StartCoroutine(DisplacementCheck());
		}

		public void OnHoldCompleted(HoldEventData eventData)
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            StopCoroutine(m_DisplacementCheckCoroutine);
		}

		public void OnHoldCanceled(HoldEventData eventData)
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            StopCoroutine(m_DisplacementCheckCoroutine);
		}

        public void OnFocusEnter() { /* Unused */ }

        public void OnFocusExit()
        {
            currentInputSource = null;
            currentInputSourceId = 0;

            StopCoroutine(m_DisplacementCheckCoroutine);
        }

        // Checking for displacement
        private IEnumerator DisplacementCheck()
        {
            float startTime = Time.fixedTime;
            m_HandStartPos = InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

            while (DisplacementTime > (Time.fixedTime - startTime) && !m_IsDisplaced)
            {
                m_HandEndPos = InputHelper.GetHandPos(currentInputSource, currentInputSourceId);
                CheckDisplacement(m_HandStartPos, m_HandEndPos);
                yield return new WaitForEndOfFrame();
            }

            if (m_IsDisplaced)
            {
                if (OnDisplaced != null)
                    OnDisplaced(gameObject);
            }

            yield return null;
        }

        // Checking for displacement hit
        private void CheckDisplacement(Vector3 origPos, Vector3 newPos)
        {
            Vector3 dispVect = newPos - origPos;

            switch (DisplacementType)
            {
                case DisplacementEnum.Any:
                    Debug.Log("Displaced");
                    m_IsDisplaced = dispVect.magnitude > DisplacementAmount;
                    break;
                case DisplacementEnum.Down:
                    Debug.Log("Displaced Down");
                    m_IsDisplaced = -dispVect.y > DisplacementAmount;
                    break;
                case DisplacementEnum.Left:
                    Debug.Log("Displaced Left");
                    m_IsDisplaced = -dispVect.x > DisplacementAmount;
                    break;
                case DisplacementEnum.Pull:
                    Debug.Log("Displaced Pull");
                    m_IsDisplaced = -dispVect.z > DisplacementAmount;
                    break;
                case DisplacementEnum.Push:
                    Debug.Log("Displaced Push");
                    m_IsDisplaced = dispVect.z > DisplacementAmount;
                    break;
                case DisplacementEnum.PushPull:
                    Debug.Log("Displaced Push/Pull");
                    m_IsDisplaced = Mathf.Abs(dispVect.z) > DisplacementAmount;
                    break;
                case DisplacementEnum.Right:
                    Debug.Log("Displaced Right");
                    m_IsDisplaced = dispVect.x > DisplacementAmount;
                    break;
                case DisplacementEnum.RightLeft:
                    Debug.Log("Displaced Right/Left");
                    m_IsDisplaced = Mathf.Abs(dispVect.x) > DisplacementAmount;
                    break;
                case DisplacementEnum.Up:
                    Debug.Log("Displaced Up");
                    m_IsDisplaced = dispVect.y > DisplacementAmount;
                    break;
                case DisplacementEnum.UpDown:
                    Debug.Log("Displaced Up/Down");
                    m_IsDisplaced = Mathf.Abs(dispVect.y) > DisplacementAmount;
                    break;
            }
        }
    }
}
