using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.Collections;

namespace MRDL.Controls
{

    public class ScrollContainer : InteractionReceiver
    {
        public enum ScrollType
        {
            UpAndDown,
            LeftAndRight,
            AllDirections
        }

        [Tooltip("The direction in which content should scroll.")]
        public ScrollType scrollDirection;

        [Tooltip("The object collection used for scrolling")]
        public ObjectCollection scrollTarget;

        [Tooltip("This value is based on the number of cells in the object collection. Note: If larger than the number of cells in the object collection, it will clamp to the max")]
        public int scrollAreaScrollSize = 3;

        [Range(0, 1)]
        public float DragTimeThreshold = 0.22f;

        private Transform scrollContainer;
        private ScrollBoundsGroup scrollBounds;

        private Bounds collectionBounds = new Bounds();

        private BoxCollider scrollableArea = new BoxCollider();

        private IInputSource currentInputSource;
        private uint currentInputSourceId;

        //Hand positions for scrolling calculations
        private Vector3 initialHandPos = Vector3.zero;
        private bool isPressed;
        private Vector3 currentHandPos = Vector3.zero;

        private float InitialPressTime;
        private float CurrentPressTime;

        private struct ScrollBoundsGroup
        {
            public Transform TopBounds;
            public Transform BottomBounds;
            public Transform LeftBounds;
            public Transform RightBounds;
            public MeshFilter[] BoundsMeshFilter
            {
                get
                {
                    MeshFilter[] meshes = new MeshFilter[3];
                    meshes[0] = TopBounds.GetComponent<MeshFilter>();
                    meshes[1] = BottomBounds.GetComponent<MeshFilter>();
                    meshes[2] = LeftBounds.GetComponent<MeshFilter>();
                    meshes[3] = RightBounds.GetComponent<MeshFilter>();
                    return meshes;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            //Go get all of our bounds objects
            scrollBounds = GetBoundsObjects();
        }

        // Use this for initialization
        private void Start()
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

            LayerMask ignoreLayers = (1 << 2); // Ignore Raycast Layer
            List<Vector3> boundsPoints = new List<Vector3>();

            GetRenderBoundsPoints(scrollTarget.gameObject, boundsPoints, ignoreLayers);

            //Convert all collection points to local space
            for (int i = 0; i < boundsPoints.Count; i++)
            {
                boundsPoints[i] = scrollTarget.transform.InverseTransformPoint(boundsPoints[i]);
            }

            //Turn our points list into a bounds
            collectionBounds.center = boundsPoints[0];
            collectionBounds.size = Vector3.zero;

            foreach (Vector3 point in boundsPoints)
            {
                collectionBounds.Encapsulate(point);
            }

            //Adjust scale and position of our occluder objects
            switch (scrollDirection)
            {
                case ScrollType.UpAndDown:
                default:

                    scrollBounds.TopBounds.localScale = CalculateScale(scrollBounds.TopBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.BottomBounds.localScale = CalculateScale(scrollBounds.BottomBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);

                    break;

                case ScrollType.LeftAndRight:

                    scrollBounds.LeftBounds.localScale = CalculateScale(scrollBounds.LeftBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.RightBounds.localScale = CalculateScale(scrollBounds.RightBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);

                    break;

                case ScrollType.AllDirections:

                    scrollBounds.TopBounds.localScale = CalculateScale(scrollBounds.TopBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.BottomBounds.localScale = CalculateScale(scrollBounds.BottomBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.LeftBounds.localScale = CalculateScale(scrollBounds.LeftBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.RightBounds.localScale = CalculateScale(scrollBounds.RightBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);

                    break;
            }


            //Debug.Log("this is the base class position: " + this.transform.position.ToString("F5"));
            //Debug.Log("this is the object collection's position: " + scrollTarget.transform.position.ToString("F5"));
            //Debug.Log("this is the scroll container's position: " + scrollContainer.transform.position.ToString("F5"));

            //TODO: Position scroll object collection in correct place for scrolling
            Vector3 tPos = scrollContainer.transform.position;
            tPos.y = collectionBounds.size.y;
            scrollBounds.TopBounds.position = tPos;

            Vector3 bPos = scrollContainer.transform.position;
            bPos.y = (scrollTarget.CellHeight * scrollAreaScrollSize) * -1f;
            scrollBounds.BottomBounds.position = bPos;


        }

        //TODO: SET UP INPUT EVENTS
        protected override void InputDown(GameObject obj, InputEventData eventData)
        {
            Debug.Log(eventData.selectedObject);

            currentInputSource = eventData.InputSource;
            currentInputSourceId = eventData.SourceId;

            initialHandPos = MRDL.Utility.InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

            InitialPressTime = Time.time;
            isPressed = true;
        }

        protected override void InputUp(GameObject obj, InputEventData eventData)
        {
            isPressed = false;
        }


        // Update is called once per frame
        void Update()
        {
            //currentHandPos = MRDL.Utility.InputHelper.GetHandPos(currentInputSource, currentInputSourceId);

            if(isPressed && DragTimeTest(InitialPressTime, Time.time, DragTimeThreshold))
            {
                Debug.Log("This was a drag");
            }
        }

        private bool DragTimeTest(float initTime, float currTime, float pressMargin)
        {
            if (currTime - initTime > pressMargin)
            {
                return true;
            }
            return false;
        }

        private ScrollBoundsGroup GetBoundsObjects()
        {
            ScrollBoundsGroup bounds = new ScrollBoundsGroup();

            foreach (Transform child in this.transform)
            {
                //I'm sure there is a more efficient way to do this...
                if (child.name == "TopBounds")
                {
                    bounds.TopBounds = child;
                }
                if (child.name == "BottomBounds")
                {
                    bounds.BottomBounds = child;
                }
                if (child.name == "LeftBounds")
                {
                    bounds.LeftBounds = child;
                }
                if (child.name == "RightBounds")
                {
                    bounds.RightBounds = child;
                }
            }
            return bounds;
        }


        public static void GetRenderBoundsPoints(GameObject target, List<Vector3> boundsPoints, LayerMask ignoreLayers)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
            {
                var rendererObj = renderers[i];
                if (ignoreLayers == (1 << rendererObj.gameObject.layer | ignoreLayers))
                {
                    continue;
                }

                rendererObj.bounds.GetCornerPositionsFromRendererBounds(ref corners);
                boundsPoints.AddRange(corners);
            }
        }


        private static Vector3[] corners = null;
        private static Vector3[] rectTransformCorners = new Vector3[4];

        private Vector3 CalculateScale(Bounds objBounds, Bounds otherBounds)
        {
            //TODO: Add optional margins (especially z)
            Vector3 szA = otherBounds.size;
            Vector3 szB = objBounds.size;
            return new Vector3(szA.x / szB.x, szA.y / szB.y, szA.z / szB.z);
        }


    }
}
