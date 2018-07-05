//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using UnityEngine;
using System.Collections;
using HoloToolkit.Unity.InputModule;

namespace MRDL.Interaction
{
    /// <summary>
    /// This is a interactible object for Slideshow Interactions.
    /// An array of textures is iterated through as the use interacts with the object.
    /// </summary>
    public class SlideshowInteractable : MonoBehaviour, ToggleInteractable.IToggleable, IInputClickHandler, IFocusable, IHoldHandler
    {
        [Tooltip("Transition time between slides")]
        public float TransitionDuration = 1.0f;

        [Tooltip("Color to fade to between slides")]
        public Color FadeColor = Color.black;

        [Tooltip("Array of textures as slides")]
        public Texture[] SlideTextures;

        private int slideIndex = 0;
        private float m_PassedTime;
        private Texture origTexture;
        private Renderer thisRenderer;

        private bool m_Targted;

        public void OnEnable()
        {
            thisRenderer = this.GetComponent<Renderer>();
            origTexture = thisRenderer.material.mainTexture;

            // Switch Material
            if (thisRenderer != null)
            {
                slideIndex = 0;
                thisRenderer.material.SetTexture(0, SlideTextures[slideIndex]);
            }
        }

        public void OnDisable()
        {
            // Set the texture back the original
            if (thisRenderer != null)
            {
                thisRenderer.material.SetTexture(0, origTexture);
            }
        }

        // Start the transitions
        IEnumerator StartTransition()
        {
            float f_halfDuration = TransitionDuration / 2;

            // Fade out
            m_PassedTime = 0.0f;
            while (m_PassedTime < f_halfDuration)
            {
                float fRGBval = 1.0f - (m_PassedTime / f_halfDuration);
                Color colour = new Color(fRGBval, fRGBval, fRGBval);

                thisRenderer.material.color = colour;
                m_PassedTime = m_PassedTime + Time.deltaTime;
            }

            // Switch Material
            if (thisRenderer != null)
            {
                slideIndex = (slideIndex >= (SlideTextures.Length - 1)) ? 0 : slideIndex + 1;
                thisRenderer.material.SetTexture(0, SlideTextures[slideIndex]);
            }

            // Fade in
            m_PassedTime = 0.0f;
            while (m_PassedTime < f_halfDuration)
            {
                float fRGBval = (m_PassedTime / f_halfDuration);
                Color colour = new Color(fRGBval, fRGBval, fRGBval);

                thisRenderer.material.color = colour;
                m_PassedTime = m_PassedTime + Time.deltaTime;
            }
            yield return (null);
        }

		public void OnInputClicked(InputClickedEventData eventData)
        {
			StartCoroutine(StartTransition());
		}

        public void OnHoldStarted(HoldEventData eventData)
        {
            m_Targted = true;
        }

        public void OnHoldCompleted(HoldEventData eventData)
        {
            if (m_Targted)
            {
                StartCoroutine(StartTransition());
                m_Targted = false;
            }
        }

        public void OnHoldCanceled(HoldEventData eventData)
        {
            m_Targted = false;
        }

        public void OnFocusExit()
        {
            m_Targted = false;
        }

        public void OnFocusEnter() { /*Unused*/ }

    }
}