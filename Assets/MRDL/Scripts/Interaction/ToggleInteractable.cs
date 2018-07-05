//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using HoloToolkit.Unity.InputModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MRDL.Interaction
{
    /// <summary>
    /// This is a simple interactible for toggling other interactible components.
    /// </summary>
    public class ToggleInteractable : MonoBehaviour, IInputHandler, IFocusable, IHoldHandler
    {
        public interface IToggleable
        {
            bool enabled { get; set; }
        }

        [Tooltip("Target object to enable or disable components on")]
        public GameObject TargetObject;

        [Tooltip("Transition time between slides")]
        public Component[] EnableComponents;

        [Tooltip("Transition time between slides")]
        public Component[] DisableComponents;

        [Tooltip("Reset from prefab works in editor to revert back to prefab settings")]
        public bool ResetFromPrefab;

        private bool bToggled;

        private bool m_Targted;

        public void OnInputUp(InputEventData eventData)
        {
            this.Triggered();
        }

        public void OnHoldStarted(HoldEventData eventData)
        {
            m_Targted = true;
        }

        public void OnHoldCompleted(HoldEventData eventData)
        {
            if (m_Targted)
            {
                this.Triggered();
                m_Targted = false;
            }
        }

        public void OnHoldCanceled(HoldEventData eventData)
        {
            m_Targted = false;
        }


        public void OnInputDown(InputEventData eventData) { /* Unused */ }


        public void OnFocusEnter() { /* Unused */ }

        public void OnFocusExit()
        {
            m_Targted = false;
        }

        protected void Triggered()
        {
            if (TargetObject != null)
            {
#if UNITY_EDITOR
            	    if (ResetFromPrefab) { PrefabUtility.ResetToPrefabState(TargetObject); }
#endif

                bToggled = !bToggled;

                if (EnableComponents.Length > 0 || DisableComponents.Length > 0)
                {
                    // Enable the components in the enable array
                    foreach (var component in EnableComponents)
                    {
                        MonoBehaviour toggleComp = (MonoBehaviour)component;
                        if (toggleComp != null)
                        {
                            toggleComp.enabled = bToggled;
                        }
                    }

                    // Disable the components in the disable array
                    foreach (var component in DisableComponents)
                    {
                        MonoBehaviour toggleComp = (MonoBehaviour)component;
                        if (toggleComp != null)
                        {
                            toggleComp.enabled = !bToggled;
                        }
                    }
                }
                else
                {
                    var intComponents = TargetObject.GetComponents<IToggleable>();
                    foreach (var component in intComponents)
                    {
                        component.enabled = bToggled;
                    }
                }
            }
        }
    }
}