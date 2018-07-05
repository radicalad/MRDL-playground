using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.Collections;

namespace MRDL.Controls
{

    public class ScollContainer : InteractionReceiver
    {
        public enum ScrollType
        {
            UpAndDown,
            LeftAndRight,
            AllDirections
        }

        public ScrollType scrollDirection;
        public ObjectCollection scrollTarget;

        private Transform scrollContainer;
        private List<GameObject> scollBounds;

        public override void OnEnable()
        {
            base.OnEnable();

            //Go get all of our bounds objects
            scollBounds = GetBoundsObjects();

        }
        // Use this for initialization
        void Start()
        {
            if (!scrollTarget)
            {
                Debug.LogError("ScrollContainer needs a valid object collection in order to work. Deactivating.");
                this.gameObject.SetActive(false);
            }
            else
            {
                //Find our scroll container & parent the scrollTarget to it
                scrollContainer = this.transform.Find("ScrollContainer");
                scrollTarget.transform.parent = scrollContainer;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }


        private List<GameObject> GetBoundsObjects()
        {
            List<GameObject> bounds = new List<GameObject>();

            foreach (Transform child in this.transform)
            {
                if (child.name == "TopBounds" || child.name == "BottomBounds" || child.name == "LeftBounds" || child.name == "RightBounds")
                {
                    bounds.Add(child.gameObject);
                }
            }
            return bounds;
        }

        private Vector3 CalculateScale(Bounds objBounds, Bounds otherBounds)
        {
            Vector3 szA = otherBounds.size;
            Vector3 szB = objBounds.size;
            return new Vector3(szA.x / szB.x, szA.y / szB.y, szA.z / szB.z);
        }
    }
}
