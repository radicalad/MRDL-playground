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

        private GameObject scrollContainer;
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

        private struct ObjectCollectionValueStorage
        {

        }

        private struct ScrollBoundsGroup
        {
            public Transform TopBounds;
            public Transform BottomBounds;
            public Transform LeftBounds;
            public Transform RightBounds;
            public List<Transform> BoundsList { get; private set; }
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

            public void InstantiateBounds(Transform parent)
            {
                BoundsList = new List<Transform>();

                TopBounds = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                BottomBounds = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                LeftBounds = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                RightBounds = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;

                TopBounds.gameObject.name = "TopBounds";
                BottomBounds.gameObject.name = "BottomBounds";
                LeftBounds.gameObject.name = "LeftBounds";
                RightBounds.gameObject.name = "RightBounds";

                BoundsList.Add(TopBounds);
                BoundsList.Add(BottomBounds);
                BoundsList.Add(LeftBounds);
                BoundsList.Add(RightBounds);
                
                //Find the unlit shader once
                Shader hidden = Shader.Find("Unlit/Color");
                
                foreach (Transform t in BoundsList)
                {
                    //Hide object, destroy the box collider, and set the layer to ignore raycast
                    Destroy( t.GetComponent<BoxCollider>() );
                    t.gameObject.layer = 2;

                    t.parent = parent;

                    // apply the unlit shader to our primitives so we can't see them
                    t.gameObject.GetComponent<Renderer>().material.shader = hidden;
                    t.gameObject.GetComponent<Renderer>().material.color = Color.black;
                }
            }
        }

        public override void OnEnable()
        {
//            Debug.Log(scrollTarget.transform.position);
            base.OnEnable();
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
                scrollContainer = new GameObject();
                scrollContainer.name = "Container";
                scrollContainer.transform.parent = this.transform;
                scrollContainer.transform.position = Vector3.zero;
                
                this.transform.position = scrollTarget.transform.position;
                this.transform.rotation = scrollTarget.transform.rotation;

                scrollTarget.transform.parent = scrollContainer.transform;

                scrollTarget.transform.position = Vector3.zero;
                scrollTarget.transform.rotation = Quaternion.identity;

                //Go get all of our bounds objects
                scrollBounds = new ScrollBoundsGroup();
                scrollBounds.InstantiateBounds(this.transform);

            }

            LayerMask ignoreLayers = (1 << 2); // Ignore Raycast Layer
            List<Vector3> boundsPoints = new List<Vector3>();

            HoloToolkit.Unity.UX.BoundingBox.GetRenderBoundsPoints(scrollTarget.gameObject, boundsPoints, ignoreLayers);

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
                    scrollBounds.LeftBounds.gameObject.SetActive(false);
                    scrollBounds.RightBounds.gameObject.SetActive(false);
                    break;

                case ScrollType.LeftAndRight:

                    scrollBounds.LeftBounds.localScale = CalculateScale(scrollBounds.LeftBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.RightBounds.localScale = CalculateScale(scrollBounds.RightBounds.GetComponent<MeshRenderer>().bounds, collectionBounds);
                    scrollBounds.TopBounds.gameObject.SetActive(false);
                    scrollBounds.BottomBounds.gameObject.SetActive(false);
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

            if(isPressed && DragTimeTest(InitialPressTime, Time.time, DragTimeThreshold))
            {
                currentHandPos = MRDL.Utility.InputHelper.GetHandPos(currentInputSource, currentInputSourceId);
                CalculateDragMove();
            }
        }


        private void CalculateDragMove()
        {

            //vars we'll need:
            //currentHandPos
            //initialHandPos
            //scrollAreaScrollSize
            //scrollTarget.CellHeight -- calculate precentage from total number of cells
            //scrollTarget.CellWidth
            //
            //scrollContainer -- object to scroll

            int totalItems = scrollTarget.NodeList.Count;
            Vector3 handDelta = initialHandPos - currentHandPos;
            float scrollAreaOffset = (totalItems * scrollTarget.CellHeight) - (scrollAreaScrollSize * scrollTarget.CellHeight);
            float maxY = scrollBounds.TopBounds.position.y - (scrollAreaScrollSize * scrollTarget.CellHeight);
            float minY = scrollBounds.BottomBounds.position.y + (scrollAreaScrollSize * scrollTarget.CellHeight);
            //float maxX = scrollBounds.RightBounds.position.y;
            //float minX = scrollBounds.LeftBounds.position.y;

            float amountToScroll = ((totalItems * scrollTarget.CellHeight) - (scrollAreaScrollSize * scrollTarget.CellHeight)) / handDelta.y;

            //Debug.Log("amount to scroll" + amountToScroll);
            Debug.Log(handDelta.y.ToString("F5"));
            switch (scrollDirection)
            {
                case ScrollType.UpAndDown:
                default:

                    Vector3 newPos = new Vector3(0f, handDelta.y, 0f);

                    if(handDelta.y > 0f)
                    {
                        if (scrollContainer.transform.position.y < maxY)
                        {
                            scrollContainer.transform.position += newPos;
                        }
                        else
                        {
                            newPos.Set(scrollContainer.transform.position.x, maxY, scrollContainer.transform.position.z);
                            scrollContainer.transform.position = newPos;
                        }
                    }
                    else if (handDelta.y < 0f)
                    {
                        if(scrollContainer.transform.position.y > minY)
                        {
                            scrollContainer.transform.position += newPos;
                        } else
                        {
                            newPos.Set(scrollContainer.transform.position.x, minY, scrollContainer.transform.position.z);
                            scrollContainer.transform.position = newPos;
                        }
                    }

                    break;
                case ScrollType.LeftAndRight:

                    break;
                case ScrollType.AllDirections:
                    break;
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

        private Vector3 CalculateScale(Bounds objBounds, Bounds otherBounds)
        {
            //TODO: Add optional margins (especially z)
            Vector3 szA = otherBounds.size;
            Vector3 szB = objBounds.size;
            return new Vector3(szA.x / szB.x, szA.y / szB.y, szA.z / szB.z);
        }


    }
}
