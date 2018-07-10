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

        [Tooltip("Multiplier to fix any mismatch in scale calculation of the occlusion objects")]
        public Vector3 OcclusionScalePadding = default(Vector3);

        [Tooltip("Multiplier to fix any mismatch in position calculation of the occlusion objects")]
        public Vector3 OcclusionPositionPadding = default(Vector3);

        [Range(0, 1)]
        public float DragTimeThreshold = 0.22f;

        public AnimationCurve SpeedFalloff;

        private GameObject scrollContainer;
        private ScrollBoundsGroup scrollBounds;

        private Bounds collectionBounds = new Bounds();

        private BoxCollider scrollableArea;

        private IInputSource currentInputSource;
        private uint currentInputSourceId;


        //Hand positions for scrolling calculations
        private Vector3 initialHandPos = Vector3.zero;
        private bool isPressed;
        private Vector3 currentHandPos = Vector3.zero;
        private float InitialPressTime;

        private float scrollVelocity;
        private float InitialVelocity = 0.0f;
        private float FinalVelocity = 0.25f;
        private float AccelerationRate = 0.1f;


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
                    Destroy(t.GetComponent<BoxCollider>());
                    t.gameObject.layer = 2;

                    t.parent = parent;

                    // apply the unlit shader to our primitives so we can't see them on the HoloLens
                    t.gameObject.GetComponent<Renderer>().material.shader = hidden;
                    t.gameObject.GetComponent<Renderer>().material.color = Color.black;
                }
            }
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
                //Grab the Nodes in the Object collection and feed them into the base InteractionReceiver
                foreach (CollectionNode cN in scrollTarget.NodeList)
                {
                    base.interactables.Add(cN.transform.gameObject);
                }

                //zero out our position/rotation before we do any position / parenting
                this.transform.position = Vector3.zero;
                this.transform.rotation = Quaternion.identity;

                //Make our scroll container & parent the scrollTarget to it
                scrollContainer = new GameObject
                {
                    name = "Container"
                };

                //Go get all of our bounds objects
                scrollBounds = new ScrollBoundsGroup();
                scrollBounds.InstantiateBounds(this.transform);

                //Find or create our dynamic collider
                scrollableArea = (this.gameObject.GetComponent<BoxCollider>()) ? this.gameObject.GetComponent<BoxCollider>() : this.gameObject.AddComponent<BoxCollider>() as BoxCollider;

                // Ignore Raycast Layer
                LayerMask ignoreLayers = (1 << 2);

                //Set up the bounds of our occluders and box collider
                List<Vector3> collectionBoundsPoints = new List<Vector3>();

               //Calculate bounds
                HoloToolkit.Unity.UX.BoundingBox.GetRenderBoundsPoints(scrollTarget.gameObject, collectionBoundsPoints, ignoreLayers);

                //Convert all collection points to local space
                for (int i = 0; i < collectionBoundsPoints.Count; i++)
                {
                    collectionBoundsPoints[i] = scrollTarget.transform.InverseTransformPoint(collectionBoundsPoints[i]);
                }
                
                //Turn our points list into a bounds
                collectionBounds.center = collectionBoundsPoints[0];
                collectionBounds.size = Vector3.zero;

                foreach (Vector3 point in collectionBoundsPoints)
                {
                    collectionBounds.Encapsulate(point);
                }

                Vector3 newColliderSize = Vector3.one;
                Vector3 tPos, bPos, lPos, rPos, colPos = Vector3.zero;

                //Adjust scale and position of our occluder objects
                switch (scrollDirection)
                {
                    case ScrollType.UpAndDown:
                    default:

                        scrollBounds.TopBounds.localScale = CalculateScale(scrollBounds.TopBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.BottomBounds.localScale = CalculateScale(scrollBounds.BottomBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.LeftBounds.gameObject.SetActive(false);
                        scrollBounds.RightBounds.gameObject.SetActive(false);

                        tPos = scrollContainer.transform.position;
                        tPos.y = collectionBounds.size.y;
                        scrollBounds.TopBounds.position = tPos + OcclusionPositionPadding;

                        bPos = scrollContainer.transform.position;
                        bPos.y = (scrollTarget.CellHeight * scrollAreaScrollSize) * -1f;
                        scrollBounds.BottomBounds.position = bPos + OcclusionPositionPadding;

                        colPos.Set(scrollContainer.transform.position.x, ( (scrollTarget.NodeList.Count - 1) * scrollTarget.CellHeight) * 0.5f - (scrollTarget.CellHeight * 0.5f), scrollBounds.TopBounds.position.z);
                        newColliderSize.Set(scrollBounds.TopBounds.localScale.x, scrollTarget.CellHeight * scrollAreaScrollSize, scrollBounds.TopBounds.localScale.z);

                        break;

                    case ScrollType.LeftAndRight:

                        scrollBounds.LeftBounds.localScale = CalculateScale(scrollBounds.LeftBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.RightBounds.localScale = CalculateScale(scrollBounds.RightBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.TopBounds.gameObject.SetActive(false);
                        scrollBounds.BottomBounds.gameObject.SetActive(false);

                        lPos = scrollContainer.transform.position;
                        lPos.x = collectionBounds.size.x;
                        scrollBounds.LeftBounds.position = lPos + OcclusionPositionPadding;

                        rPos = scrollContainer.transform.position;
                        rPos.x = (scrollTarget.CellWidth * scrollAreaScrollSize) * -1f;
                        scrollBounds.RightBounds.position = rPos + OcclusionPositionPadding;

                        colPos.Set(((scrollTarget.NodeList.Count - 1) * scrollTarget.CellWidth) * 0.5f - (scrollTarget.CellWidth * 0.5f), scrollContainer.transform.position.y, scrollBounds.LeftBounds.position.z);
                        newColliderSize.Set(scrollTarget.CellWidth * scrollAreaScrollSize, scrollBounds.LeftBounds.localScale.y, scrollBounds.LeftBounds.localScale.z);

                        break;

                    case ScrollType.AllDirections:

                        scrollBounds.TopBounds.localScale = CalculateScale(scrollBounds.TopBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.BottomBounds.localScale = CalculateScale(scrollBounds.BottomBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.LeftBounds.localScale = CalculateScale(scrollBounds.LeftBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);
                        scrollBounds.RightBounds.localScale = CalculateScale(scrollBounds.RightBounds.GetComponent<MeshRenderer>().bounds, collectionBounds, OcclusionScalePadding);

                        tPos = scrollContainer.transform.position;
                        tPos.y = collectionBounds.size.y;
                        scrollBounds.TopBounds.position = tPos + OcclusionPositionPadding;

                        bPos = scrollContainer.transform.position;
                        bPos.y = (scrollTarget.CellHeight * scrollAreaScrollSize) * -1f;
                        scrollBounds.BottomBounds.position = bPos + OcclusionPositionPadding;

                        lPos = scrollContainer.transform.position;
                        lPos.x = collectionBounds.size.x;
                        scrollBounds.LeftBounds.position = lPos + OcclusionPositionPadding;

                        rPos = scrollContainer.transform.position;
                        rPos.x = (scrollTarget.CellWidth * scrollAreaScrollSize) * -1f;
                        scrollBounds.RightBounds.position = rPos + OcclusionPositionPadding;

                        colPos.Set(((scrollTarget.NodeList.Count - 1) * scrollTarget.CellWidth) * 0.5f - (scrollTarget.CellWidth * 0.5f), ((scrollTarget.NodeList.Count - 1) * scrollTarget.CellHeight) * 0.5f - (scrollTarget.CellHeight * 0.5f), scrollBounds.LeftBounds.position.z);
                        newColliderSize.Set(scrollTarget.CellWidth * scrollAreaScrollSize, scrollTarget.CellHeight * scrollAreaScrollSize, scrollBounds.LeftBounds.localScale.z);

                        break;
                }

                //Adjust our collider
                scrollableArea.size = newColliderSize;
                scrollableArea.center = colPos;

                scrollContainer.transform.parent = this.transform;
                scrollContainer.transform.localPosition = Vector3.zero;
                scrollContainer.transform.rotation = Quaternion.identity;

                this.transform.position = scrollTarget.transform.position;
                this.transform.rotation = scrollTarget.transform.rotation;

                scrollTarget.transform.parent = scrollContainer.transform;
                scrollTarget.transform.localPosition = Vector3.zero;
                scrollTarget.transform.rotation = Quaternion.identity;
            }
        }


        protected override void InputDown(GameObject obj, InputEventData eventData)
        {
            currentInputSource = eventData.InputSource;
            currentInputSourceId = eventData.SourceId;

            initialHandPos = MRDL.Utility.InputHelper.GetHandPos(currentInputSource, currentInputSourceId);
            InitialPressTime = Time.time;
            isPressed = true;
            inputEvent = eventData;
        }

        private InputEventData inputEvent;

        protected override void InputUp(GameObject obj, InputEventData eventData)
        {
            isPressed = false;
            scrollVelocity = 0.0f;
        }


        void Update()
        {

            if (isPressed && DragTimeTest(InitialPressTime, Time.time, DragTimeThreshold))
            {
                currentHandPos = MRDL.Utility.InputHelper.GetHandPos(currentInputSource, currentInputSourceId);
                CalculateDragMove();
            }
        }

        private void CalculateDragMove()
        {

            Vector3 handDelta = initialHandPos - currentHandPos;

            float maxY = scrollBounds.TopBounds.position.y - (scrollAreaScrollSize * scrollTarget.CellHeight);
            float minY = scrollBounds.BottomBounds.position.y + (scrollAreaScrollSize * scrollTarget.CellHeight);
            float maxX = scrollBounds.RightBounds.position.x - (scrollAreaScrollSize * scrollTarget.CellHeight);
            float minX = scrollBounds.LeftBounds.position.x + (scrollAreaScrollSize * scrollTarget.CellHeight);

            switch (scrollDirection)
            {
                case ScrollType.UpAndDown:
                default:

                    if (handDelta.y > 0.01f)
                    {
                        scrollVelocity += AccelerationRate * Time.deltaTime;
                        scrollContainer.transform.position = Vector3.MoveTowards(this.transform.position, new Vector3(0f, maxY, 0f), scrollVelocity);

                    }
                    else if (handDelta.y < -0.01f)
                    {
                        Debug.Log("hand delta is negative");
                        scrollVelocity += AccelerationRate * Time.deltaTime;
                        scrollContainer.transform.position = Vector3.MoveTowards(this.transform.position, new Vector3(0f, minY, 0f), scrollVelocity);
                    }

                    //scrollVelocity = Mathf.Clamp(scrollVelocity, FinalVelocity * -1, FinalVelocity);
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

        private Vector3 CalculateScale(Bounds objBounds, Bounds otherBounds, Vector3 padding = default(Vector3))
        {
            //Optional padding (especially z)
            Vector3 szA = otherBounds.size + new Vector3(otherBounds.size.x * padding.x, otherBounds.size.y * padding.y, otherBounds.size.z * padding.z);
            Vector3 szB = objBounds.size;
            return new Vector3(szA.x / szB.x, szA.y / szB.y, szA.z / szB.z);
        }


    }
}
