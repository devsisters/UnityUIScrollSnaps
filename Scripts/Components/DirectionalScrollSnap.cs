//Dependencies:
// - Scroller: Source > Scripts > HelperClasses
// - DirectionalScrollSnapEditor: Source > Editor (optional)

//Contributors:
//BeksOmega

using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEditor;

namespace UnityEngine.UI.ScrollSnaps
{
    [AddComponentMenu("UI/Scroll Snaps/Directional Scroll Snap")]
    [ExecuteInEditMode]
    public class DirectionalScrollSnap : UIBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, ICanvasElement, IScrollHandler, ILayoutGroup
    {

        #region Variables
        public enum MovementDirection
        {
            Horizontal,
            Vertical
        }

        public enum MovementType
        {
            Clamped,
            Elastic
        }

        public enum SnapType
        {
            SnapToNearest,
            SnapToLastPassed,
            SnapToNext
        }

        private enum Direction
        {
            TowardsStart,
            TowardsEnd
        }

        public enum InterpolatorType
        {
            AccelerateDecelerate,
            Accelerate,
            Anticipate,
            AnticipateOvershoot,
            Decelerate,
            Linear,
            Overshoot,
            ViscousFluid,
        }

        public enum FilterMode
        {
            BlackList,
            WhiteList
        }

        public enum StartMovementEventType
        {
            OnBeginDrag,
            ScrollBar,
            OnScroll,
            ButtonPress,
            Programmatic
        }

        [Serializable]
        public class Vector2Event : UnityEvent<Vector2> { }
        [Serializable]
        public class StartMovementEvent : UnityEvent<StartMovementEventType> { }
        [Serializable]
        public class IntEvent : UnityEvent<int> { }

        [SerializeField]
        private RectTransform m_Content;
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }

        [SerializeField]
        private MovementDirection m_MovementDirection;
        public MovementDirection movementDirection
        {
            get
            {
                return m_MovementDirection;
            }
            set
            {
                if (contentIsHorizonalLayoutGroup)
                    m_MovementDirection = MovementDirection.Horizontal;
                else if (contentIsVerticalLayoutGroup)
                    m_MovementDirection = MovementDirection.Vertical;
                else
                    m_MovementDirection = value;
            }
        }

        [SerializeField]
        private bool m_LockOtherDirection = true;
        public bool lockNonScrollingDirection { get { return m_LockOtherDirection; } set { m_LockOtherDirection = value; } }

        [SerializeField]
        private MovementType m_MovementType;
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private SnapType m_SnapType = SnapType.SnapToNearest;
        public SnapType snapType { get { return m_SnapType; } set { m_SnapType = value; } }

        [SerializeField]
        private bool m_UseVelocity = true;
        public bool useVelocity { get { return m_UseVelocity; } set { m_UseVelocity = value; } }

        [SerializeField]
        private float m_Friction = .25f;

        [SerializeField]
        private InterpolatorType m_InterpolatorType = InterpolatorType.ViscousFluid;

        [SerializeField]
        private float m_Tension = 2f;

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

        [SerializeField]
        private float m_ScrollDelay = .02f;
        public float scrollDelay
        {
            get
            {
                return m_ScrollDelay;
            }
            set
            {
                m_ScrollDelay = Mathf.Max(value, 0);
            }
        }

        [SerializeField]
        private int m_MinDurationMillis = 200;

        [SerializeField]
        private int m_MaxDurationMillis = 2000;

        [SerializeField]
        private int m_ScrollDurationMillis = 250;
        public int scrollDurationMillis
        {
            get
            {
                return m_ScrollDurationMillis;
            }
            set
            {
                m_ScrollDurationMillis = Mathf.Max(value, 1);
            }
        }

        [SerializeField]
        private bool m_AddInactiveChildrenToCalculatingFilter;
        public bool addInactiveChildrenToCalculatingFilter { get { return m_AddInactiveChildrenToCalculatingFilter; } set { m_AddInactiveChildrenToCalculatingFilter = value; } }

        [SerializeField]
        private FilterMode m_FilterModeForCalculatingSize;
        public FilterMode calculateSizeFilterMode { get { return m_FilterModeForCalculatingSize; } set { m_FilterModeForCalculatingSize = value; } }

        [SerializeField]
        private List<RectTransform> m_CalculatingFilter = new List<RectTransform>();
        public List<RectTransform> calculatingFilter { get { return m_CalculatingFilter; } set { m_CalculatingFilter = value; } }

        [SerializeField]
        private bool m_AddInactiveChildrenToSnapPositionsFilter;
        public bool addInactiveChildrenToSnapPositionsFilter { get { return m_AddInactiveChildrenToSnapPositionsFilter; } set { m_AddInactiveChildrenToSnapPositionsFilter = value; } }

        [SerializeField]
        private FilterMode m_FilterModeForSnapPositions;
        public FilterMode snapPositionsFilterMode { get { return m_FilterModeForSnapPositions; } set { m_FilterModeForSnapPositions = value; } }

        [SerializeField]
        private List<RectTransform> m_SnapPositionsFilter = new List<RectTransform>();
        public List<RectTransform> snapPositionsFilter { get { return m_SnapPositionsFilter; } set { m_SnapPositionsFilter = value; } }

        [SerializeField]
        private RectTransform m_Viewport;
        public RectTransform viewport { get { return m_Viewport; } set { m_Viewport = value; SetDirtyCaching(); } }

        private ScrollBarEventsListener m_HorizontalScrollbarEventsListener;
        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        public Scrollbar horizontalScrollbar
        {
            get
            {
                return m_HorizontalScrollbar;
            }
            set
            {
                if (m_HorizontalScrollbar)
                {
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                    if (m_HorizontalScrollbarEventsListener)
                    {
                        m_HorizontalScrollbarEventsListener.onPointerDown -= ScrollBarPointerDown;
                        m_HorizontalScrollbarEventsListener.onPointerUp -= ScrollBarPointerUp;
                        DestroyImmediate(m_HorizontalScrollbarEventsListener);
                    }
                }
                m_HorizontalScrollbar = value;
                if (m_HorizontalScrollbar)
                {
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                    m_HorizontalScrollbarEventsListener = m_HorizontalScrollbar.gameObject.AddComponent<ScrollBarEventsListener>();
                    m_HorizontalScrollbarEventsListener.onPointerDown += ScrollBarPointerDown;
                    m_HorizontalScrollbarEventsListener.onPointerUp += ScrollBarPointerUp;
                }
            }
        }

        private ScrollBarEventsListener m_VerticalScrollBarEventsListener;
        [SerializeField]
        private Scrollbar m_VerticalScrollbar;
        public Scrollbar verticalScrollbar
        {
            get
            {
                return m_VerticalScrollbar;
            }
            set
            {
                if (m_VerticalScrollbar)
                {
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                    if (m_VerticalScrollBarEventsListener)
                    {
                        m_VerticalScrollBarEventsListener.onPointerDown -= ScrollBarPointerDown;
                        m_VerticalScrollBarEventsListener.onPointerUp -= ScrollBarPointerUp;
                        DestroyImmediate(m_VerticalScrollBarEventsListener);
                    }
                }
                m_VerticalScrollbar = value;
                if (m_VerticalScrollbar)
                {
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                    m_VerticalScrollBarEventsListener = m_VerticalScrollbar.gameObject.AddComponent<ScrollBarEventsListener>();
                    m_VerticalScrollBarEventsListener.onPointerDown += ScrollBarPointerDown;
                    m_VerticalScrollBarEventsListener.onPointerUp += ScrollBarPointerUp;
                }
            }
        }

        [SerializeField]
        private Button m_BackButton;
        public Button backButton
        {
            get
            {
                return m_BackButton;
            }
            set
            {
                if (m_BackButton)
                {
                    m_BackButton.onClick.RemoveListener(OnBack);
                }
                m_BackButton = value;
                if (m_BackButton)
                {
                    m_BackButton.onClick.AddListener(OnBack);
                }
            }
        }

        [SerializeField]
        private Button m_ForwardButton;
        public Button forwardButton
        {
            get
            {
                return m_ForwardButton;
            }
            set
            {
                if (m_ForwardButton)
                {
                    m_ForwardButton.onClick.RemoveListener(OnForward);
                }
                m_ForwardButton = value;
                if (m_ForwardButton)
                {
                    m_ForwardButton.onClick.AddListener(OnForward);
                }
            }
        }

        [SerializeField]
        private bool m_DrawGizmos = false;

        [SerializeField]
        private Vector2Event m_OnValueChanged = new Vector2Event();
        public Vector2Event onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        [SerializeField]
        private StartMovementEvent m_StartMovementEvent = new StartMovementEvent();
        public StartMovementEvent startMovementEvent { get { return m_StartMovementEvent; } set { m_StartMovementEvent = value; } }

        [SerializeField]
        private IntEvent m_ClosestSnapPositionChanged = new IntEvent();
        public IntEvent closestSnapPositionChanged { get { return m_ClosestSnapPositionChanged; } set { m_ClosestSnapPositionChanged = value; } }

        [SerializeField]
        private IntEvent m_SnappedToItem = new IntEvent();
        public IntEvent snappedToItem { get { return m_SnappedToItem; } set { m_SnappedToItem = value; } }

        [SerializeField]
        private IntEvent m_TargetItemSelected = new IntEvent();
        public IntEvent targetItemSelected { get { return m_TargetItemSelected; } set { m_TargetItemSelected = value; } }


        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.x <= m_ViewBounds.size.x)
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.y <= m_ViewBounds.size.y)
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;
                ;
                return 1 - (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private Vector2 m_Velocity;
        public Vector2 velocity { get { return m_Velocity; } }

        private int m_ClosestSnapPositionIndex;
        public int closestSnapPositionIndex { get { return m_ClosestSnapPositionIndex; } }

        private Scroller m_Scroller;
        public Scroller scroller
        {
            get
            {
                if (m_Scroller == null)
                    m_Scroller = new Scroller(m_MinDurationMillis, m_MaxDurationMillis, GetInterpolator());
                return m_Scroller;
            }
            set
            {
                if (m_Scroller != null)
                    m_Scroller.AbortAnimation();
                m_Scroller = value;
            }
        }


        private string filterWhitelistException = "The {0} is set to whitelist and is either empty or contains an empty object. You probably need to assign a child to the {0} or set the {0} to blacklist.";
        private string availableChildrenListEmptyException = "The Content has no children available for {0}. This is probably because they are all blacklisted. You should check what children you have blacklisted in your item filters and if you have Add Inactive Children checked.";
        private string contentHasNoChildrenException = "The Content has no children so it is unable to snap. You should assign children to the Content or choose a new RectTransform for the Content.";
        private string childOutsideValidRegionWarning = "Child: {0} is outside the valid bounds of the content. If this was unintentional move it inside region indicated by the green arrow gizmos. If you see no green arrows turn on Draw Gizmos.";

        private DrivenRectTransformTracker m_Tracker;

        private List<RectTransform> m_AvailableForCalculating = new List<RectTransform>();
        private List<RectTransform> m_AvailableForSnappingTo = new List<RectTransform>();
        
        private List<RectTransform> m_ChildrenForSizeFromStartToEnd = new List<RectTransform>();
        private List<RectTransform> m_ChildrenForSnappingFromStartToEnd = new List<RectTransform>();

        private List<Vector2> m_SnapPositions = new List<Vector2>();

        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        private Vector2 m_ContentStartPosition = Vector2.zero;

        private Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        private int m_PrevClosestSnap;
        private bool m_PrevScrolling;
        private float m_PrevTension;
        private float m_PrevFriction;
        private int m_PrevMinDuration;
        private int m_PrevMaxDuration;

        [NonSerialized]
        private bool m_HasRebuiltLayout = false;
        [NonSerialized]
        private bool m_HasUpdatedLayout = false;

        private Vector2 m_StartPosition;
        private Vector2 m_EndPosition;

        private float m_TotalScrollableLength;
        private Vector2 m_MinPos;
        private Vector2 m_MaxPos;

        private bool m_WaitingForEndScrolling;
        private float m_TimeOfLastScroll;

        [NonSerialized]
        private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private RectTransform m_ViewRect;
        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = m_Viewport;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }

        private LayoutGroup m_LayoutGroup;
        private bool contentIsLayoutGroup
        {
            get
            {
                if (m_Content == null)
                    return false;
                m_LayoutGroup = m_Content.GetComponent<LayoutGroup>();
                return m_LayoutGroup && m_LayoutGroup.enabled;
            }
        }
        
        private bool contentIsHorizonalLayoutGroup
        {
            get
            {
                if (m_Content == null)
                    return false;
                HorizontalLayoutGroup horizLayoutGroup = m_Content.GetComponent<HorizontalLayoutGroup>();
                return horizLayoutGroup && horizLayoutGroup.enabled;
            }
        }
        
        private bool contentIsVerticalLayoutGroup
        {
            get
            {
                if (m_Content == null)
                    return false;
                VerticalLayoutGroup vertLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
                return vertLayoutGroup && vertLayoutGroup.enabled;
            }
        }

        private RectTransform m_StartChild;
        private RectTransform firstCalculateChild
        {
            get
            {
                if (m_ChildrenForSizeFromStartToEnd == null)
                    GetChildrenFromStartToEnd();
                if (m_StartChild == null)
                    m_StartChild = m_ChildrenForSizeFromStartToEnd[0];
                return m_StartChild;
            }
        }

        private RectTransform m_EndChild;
        private RectTransform lastCalculateChild
        {
            get
            {
                if
                    (m_ChildrenForSizeFromStartToEnd == null)
                    GetChildrenFromStartToEnd();
                if (m_EndChild == null)
                    m_EndChild = m_ChildrenForSizeFromStartToEnd[m_ChildrenForSizeFromStartToEnd.Count - 1];
                return m_EndChild;
            }
        }

        private Vector3 contentTopLeft
        {
            get
            {
                Vector3[] contentCorners = new Vector3[4];
                m_Content.GetWorldCorners(contentCorners);
                return contentCorners[1];
            }
        }

        private int axis
        {
            get
            {
                return (int)movementDirection;
            }
        }

        private int inverseAxis
        {
            get
            {
                return (m_MovementDirection == MovementDirection.Vertical) ? 0 : 1;
            }
        }
        #endregion

        #region SetupScrollSnap
        protected override void OnEnable()
        {
            base.OnEnable();


            if (m_HorizontalScrollbar)
            {
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                if (Application.isPlaying)
                {
                    m_HorizontalScrollbarEventsListener = m_HorizontalScrollbar.gameObject.AddComponent<ScrollBarEventsListener>();
                    m_HorizontalScrollbarEventsListener.onPointerDown += ScrollBarPointerDown;
                    m_HorizontalScrollbarEventsListener.onPointerUp += ScrollBarPointerUp;
                }
            }
            if (m_VerticalScrollbar)
            {
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                if (Application.isPlaying)
                {
                    m_VerticalScrollBarEventsListener = m_VerticalScrollbar.gameObject.AddComponent<ScrollBarEventsListener>();
                    m_VerticalScrollBarEventsListener.onPointerDown += ScrollBarPointerDown;
                    m_VerticalScrollBarEventsListener.onPointerUp += ScrollBarPointerUp;
                }
            }

            if (m_BackButton)
            {
                m_BackButton.onClick.AddListener(OnBack);
            }
            if (m_ForwardButton)
            {
                m_ForwardButton.onClick.AddListener(OnForward);
            }

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

            RebuildLayoutGroups();

            m_Scroller = new Scroller(m_Friction, m_MinDurationMillis, m_MaxDurationMillis, GetInterpolator());

            UpdatePrevData();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_HorizontalScrollbar)
            {
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                if (m_HorizontalScrollbarEventsListener != null)
                {
                    m_HorizontalScrollbarEventsListener.onPointerDown -= ScrollBarPointerDown;
                    m_HorizontalScrollbarEventsListener.onPointerUp -= ScrollBarPointerUp;
                    DestroyImmediate(m_HorizontalScrollbarEventsListener);
                }
            }
            if (m_VerticalScrollbar)
            {
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                if (m_VerticalScrollBarEventsListener != null)
                {
                    m_VerticalScrollBarEventsListener.onPointerDown -= ScrollBarPointerDown;
                    m_VerticalScrollBarEventsListener.onPointerUp -= ScrollBarPointerUp;
                    DestroyImmediate(m_VerticalScrollBarEventsListener);
                }
            }
            if (m_BackButton)
            {
                m_BackButton.onClick.RemoveListener(OnBack);
            }
            if (m_ForwardButton)
            {
                m_ForwardButton.onClick.RemoveListener(OnForward);
            }

            m_HasRebuiltLayout = false;
            m_HasUpdatedLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>
        /// Updates the size and snap positions of the scroll snap. Call this whenever you change filters, add new children to the content, ect.
        /// </summary>
        public void UpdateLayout()
        {
            if (m_Content == null)
                return;

            m_HasUpdatedLayout = true;
            OnValidate();
            EnsureLayoutHasRebuilt();
            GetValidChildren();
            SetupDrivenTransforms();
            GetChildrenFromStartToEnd();

            Vector2 childOneOrigPosTransformLocalSpace = transform.InverseTransformPoint(firstCalculateChild.position);

            ResizeContent();
            GetSnapPositions();

            Vector2 childOneNewPosTransformLocalSpace = transform.InverseTransformPoint(firstCalculateChild.position);
            Vector2 offset = childOneOrigPosTransformLocalSpace - childOneNewPosTransformLocalSpace;
            m_Content.anchoredPosition = m_Content.anchoredPosition + offset;
        }

        private void SetupDrivenTransforms()
        {
            Vector2 anchorPos = new Vector2(0, 1);

            m_Tracker.Clear();

            m_Tracker.Add(this, m_Content, DrivenTransformProperties.Anchors);

            m_Content.anchorMax = anchorPos;
            m_Content.anchorMin = anchorPos;

            //So that we can calculate everything correctly
            foreach (RectTransform transform in m_AvailableForCalculating)
            {
                m_Tracker.Add(this, transform, DrivenTransformProperties.Anchors);

                transform.anchorMax = anchorPos;
                transform.anchorMin = anchorPos;
            }
        }

        private void GetValidChildren()
        {
            m_AvailableForCalculating = new List<RectTransform>();
            m_AvailableForSnappingTo = new List<RectTransform>();

            if (m_Content.childCount < 1)
            {
                throw (new MissingReferenceException(contentHasNoChildrenException));
            }

            Func<RectTransform, bool> childIsAvailableForCalculating;
            Func<RectTransform, bool> childIsAvailableForSnappingTo;

            if (m_FilterModeForCalculatingSize == FilterMode.WhiteList)
            {
                if (m_CalculatingFilter.Count < 1 || m_CalculatingFilter.Contains(null))
                {
                    throw (new UnassignedReferenceException(string.Format(filterWhitelistException, "Calculate Size Filter")));
                }
                childIsAvailableForCalculating = (RectTransform child) => m_CalculatingFilter.Contains(child) || (m_AddInactiveChildrenToCalculatingFilter && !child.gameObject.activeInHierarchy);
            }
            else
            {
                childIsAvailableForCalculating = (RectTransform child) => !m_CalculatingFilter.Contains(child) && (!m_AddInactiveChildrenToCalculatingFilter || child.gameObject.activeInHierarchy);
            }


            if (m_FilterModeForSnapPositions == FilterMode.WhiteList)
            {
                if (m_SnapPositionsFilter.Count < 1 || m_SnapPositionsFilter.Contains(null))
                {
                    throw (new UnassignedReferenceException(string.Format(filterWhitelistException, "Available Snaps Filter")));
                }
                childIsAvailableForSnappingTo = (RectTransform child) => m_SnapPositionsFilter.Contains(child) || (m_AddInactiveChildrenToSnapPositionsFilter && !child.gameObject.activeInHierarchy);
            }
            else
            {
                childIsAvailableForSnappingTo = (RectTransform child) => !m_SnapPositionsFilter.Contains(child) && (!m_AddInactiveChildrenToSnapPositionsFilter || child.gameObject.activeInHierarchy);
            }

            foreach (RectTransform child in m_Content)
            {

                if (childIsAvailableForCalculating(child))
                {
                    if (child.position.x < contentTopLeft.x || child.position.y > contentTopLeft.y)
                    {
                        Debug.LogWarningFormat(this, childOutsideValidRegionWarning, child.name);
                    }
                    m_AvailableForCalculating.Add(child);
                    if (childIsAvailableForSnappingTo(child))
                    {
                        m_AvailableForSnappingTo.Add(child);
                    }
                }
            }

            if (m_AvailableForCalculating.Count < 1)
            {
                throw (new MissingReferenceException(string.Format(availableChildrenListEmptyException, "calculating")));
            }

            if (m_AvailableForSnappingTo.Count < 1)
            {
                throw (new MissingReferenceException(string.Format(availableChildrenListEmptyException, "snapping to")));
            }
        }

        private void GetChildrenFromStartToEnd()
        {
            m_StartChild = null;
            m_EndChild = null;
            m_ChildrenForSizeFromStartToEnd = new List<RectTransform>();
            foreach (RectTransform child in m_Content)
            {
                if (m_AvailableForCalculating.Contains(child))
                {
                    int insert = m_ChildrenForSizeFromStartToEnd.Count;
                    for (int i = 0; i < m_ChildrenForSizeFromStartToEnd.Count; i++)
                    {
                        if (TransformAIsNearerStart(child, m_ChildrenForSizeFromStartToEnd[i]))
                        {
                            insert = i;
                            break;
                        }
                    }
                    m_ChildrenForSizeFromStartToEnd.Insert(insert, child);
                }
            }
        }

        private void ResizeContent()
        {
            float halfViewRect = (viewRect.sizeDelta[axis] / 2);
            int paddingStart = (int)(halfViewRect - Mathf.Abs(firstCalculateChild.anchoredPosition[axis]));
            int paddingEnd = (int)(halfViewRect - (m_Content.sizeDelta[axis] - Mathf.Abs(lastCalculateChild.anchoredPosition[axis])));

            if (contentIsLayoutGroup)
            {
                if (m_MovementDirection == MovementDirection.Horizontal)
                {
                    m_LayoutGroup.padding.left = m_LayoutGroup.padding.left + paddingStart;
                    m_LayoutGroup.padding.right = m_LayoutGroup.padding.right + paddingEnd;
                    m_Content.sizeDelta = new Vector2(m_Content.sizeDelta.x + paddingStart + paddingEnd, m_Content.sizeDelta.y);
                }
                else
                {
                    m_LayoutGroup.padding.top = m_LayoutGroup.padding.top + paddingStart;
                    m_LayoutGroup.padding.bottom = m_LayoutGroup.padding.bottom + paddingEnd;
                    m_Content.sizeDelta = new Vector2(m_Content.sizeDelta.x, m_Content.sizeDelta.y + paddingStart + paddingEnd);
                }
                RebuildLayoutGroups();
            }
            else
            {
                foreach (RectTransform child in m_ChildrenForSizeFromStartToEnd)
                {
                    if (m_MovementDirection == MovementDirection.Horizontal)
                    {
                        child.anchoredPosition = new Vector2(child.anchoredPosition.x + paddingStart, child.anchoredPosition.y);
                    }
                    else
                    {
                        child.anchoredPosition = new Vector2(child.anchoredPosition.x, child.anchoredPosition.y - paddingStart);
                    }
                }
                float totalSize = Mathf.Abs(lastCalculateChild.anchoredPosition[axis]) + halfViewRect;
                m_Content.sizeDelta = (movementDirection == MovementDirection.Horizontal) ? new Vector2(totalSize, m_Content.sizeDelta.y) : new Vector2(m_Content.sizeDelta.x, totalSize);
            }
            SetNormalizedPosition(1, 0);
            SetNormalizedPosition(0, 1);
            m_MinPos = m_Content.anchoredPosition;
            SetNormalizedPosition(0, 0);
            SetNormalizedPosition(1, 1);
            m_MaxPos = m_Content.anchoredPosition;
        }

        private void GetSnapPositions()
        {
            m_ChildrenForSnappingFromStartToEnd = new List<RectTransform>();
            m_SnapPositions = new List<Vector2>();
            m_TotalScrollableLength = m_Content.sizeDelta[axis] - viewRect.sizeDelta[axis];
            foreach (RectTransform child in m_ChildrenForSizeFromStartToEnd)
            {
                if (m_AvailableForSnappingTo.Contains(child))
                {
                    m_ChildrenForSnappingFromStartToEnd.Add(child);
                    float normalizedPosition;
                    GetNormalizedPositionOfChild(child, out normalizedPosition);
                    SetNormalizedPosition(normalizedPosition, axis);
                    m_SnapPositions.Add(RoundVector2ToInts(m_Content.anchoredPosition));
                }
            }
            SetNormalizedPosition(0, axis);
            m_StartPosition = RoundVector2ToInts(m_Content.anchoredPosition);
            SetNormalizedPosition(1, axis);
            m_EndPosition = RoundVector2ToInts(m_Content.anchoredPosition);
        }
        #endregion

        #region ProgrammaticallyScroll
        private void OnBack()
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.ButtonPress);
            scroller.ForceFinish();
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = m_SnapPositions[Mathf.Clamp(closestSnapPositionIndex - 1, 0, m_SnapPositions.Count - 1)][axis];
            scroller.StartScroll(m_Content.anchoredPosition, targetPosition, m_ScrollDurationMillis);
            m_TargetItemSelected.Invoke(closestSnapPositionIndex - 1);
        }

        private void OnForward()
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.ButtonPress);
            scroller.ForceFinish();
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = m_SnapPositions[Mathf.Clamp(closestSnapPositionIndex + 1, 0, m_SnapPositions.Count - 1)][axis];
            scroller.StartScroll(m_Content.anchoredPosition, targetPosition, m_ScrollDurationMillis);
            m_TargetItemSelected.Invoke(closestSnapPositionIndex + 1);
        }

        /// <summary>
        /// Scrolls to the supplied index in the supplied duration of time.
        /// </summary>
        /// <param name="index">The index of the snap position you want to scroll to.</param>
        /// <param name="durationMillis">The duration of the scroll in milliseconds.</param>
        public void ScrollToSnapPosition(int index, int durationMillis) //zero based index
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = m_SnapPositions[index][axis];
            m_Scroller.StartScroll(m_Content.anchoredPosition, targetPosition, durationMillis);
            m_TargetItemSelected.Invoke(index);
        }

        /// <summary>
        /// Scrolls to the nearest snap position to the end position in the supplied duration of time.
        /// </summary>
        /// <param name="endPos">The reference end position of the content, in the content's local coordinates.</param>
        /// <param name="durationMillis">The duration of the scroll in milliseconds.</param>
        public void ScrollToNearestSnapPosition(Vector2 endPos, int durationMillis)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = FindClosestSnapPositionToPosition(endPos)[axis];
            m_Scroller.StartScroll(m_Content.anchoredPosition, targetPosition, durationMillis);
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(targetPosition));
        }

        /// <summary>
        /// Scrolls to the nearest snap position to the normalized position in the supplied duration of time.
        /// </summary>
        /// <param name="normalizedPos">The reference end position of the content, normalized.</param>
        /// <param name="durationMillis">The duration of the scroll in milliseconds.</param>
        public void ScrollToNearestSnapPosition(float normalizedPos, int durationMillis)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 anchoredPos = m_Content.anchoredPosition;
            SetNormalizedPosition(normalizedPos, axis);
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = FindClosestSnapPositionToPosition(m_Content.anchoredPosition)[axis];
            m_Scroller.StartScroll(anchoredPos, targetPosition, durationMillis);
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(targetPosition));
        }

        /// <summary>
        /// Flings to the supplied index at the supplied velocity.
        /// </summary>
        /// <param name="index">The index of the snap position you want to scroll to.</param>
        /// <param name="velocity">The velocity the scroll snap will move at in units per second, in the content's local space.</param>
        public void FlingToSnapPosition(int index, float velocity)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 velocityV2 = Vector2.zero;
            velocityV2[axis] = velocity;
            Vector2 targetPosition = m_Content.anchoredPosition;
            targetPosition[axis] = m_SnapPositions[index][axis];

            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 min = new Vector2(m_MinPos.x - offset.x, m_MinPos.y - offset.y);
            Vector2 max = new Vector2(m_MaxPos.x - offset.x, m_MaxPos.y - offset.y);
            m_Scroller.Fling(m_Content.anchoredPosition, velocityV2, min, max);
            m_Scroller.SetFinalPosition(targetPosition);
            m_TargetItemSelected.Invoke(index);
        }

        /// <summary>
        /// Flings to the nearest snap position to the end position at the supplied velocity.
        /// </summary>
        /// <param name="endPos">The reference end position of the content, in the content's local coordinates.</param>
        /// <param name="velocity">The velocity the scroll snap will move at in units per second, in the content's local space.</param>
        public void FlingToNearestSnapPosition(Vector2 endPos, float velocity)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 velocityV2 = Vector2.zero;
            velocityV2[axis] = velocity;
            Vector2 finalPosition = m_Content.anchoredPosition;
            Vector2 snapPos = FindClosestSnapPositionToPosition(endPos);
            finalPosition[axis] = snapPos[axis];

            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 min = new Vector2(m_MinPos.x - offset.x, m_MinPos.y - offset.y);
            Vector2 max = new Vector2(m_MaxPos.x - offset.x, m_MaxPos.y - offset.y);
            m_Scroller.Fling(m_Content.anchoredPosition, velocityV2, min, max);
            m_Scroller.SetFinalPosition(finalPosition);
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(snapPos));
        }

        /// <summary>
        /// Flings to the nearest snap position to the normalized position at the supplied velocity.
        /// </summary>
        /// <param name="normalizedPos">The reference end position of the content, normalized.</param>
        /// <param name="velocity">The velocity the scroll snap will move at in units per second, in the content's local space.</param>
        public void FlingToNearestSnapPosition(float normalizedPos, float velocity)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();
            Vector2 velocityV2 = Vector2.zero;
            velocityV2[axis] = velocity;
            Vector2 anchoredPos = m_Content.anchoredPosition;
            SetNormalizedPosition(normalizedPos, axis);
            Vector2 finalPos = m_Content.anchoredPosition;
            Vector2 snapPos = FindClosestSnapPositionToPosition(finalPos);
            finalPos[axis] = snapPos[axis];

            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 min = new Vector2(m_MinPos.x - offset.x, m_MinPos.y - offset.y);
            Vector2 max = new Vector2(m_MaxPos.x - offset.x, m_MaxPos.y - offset.y);
            m_Scroller.Fling(m_Content.anchoredPosition, velocityV2, min, max);
            m_Scroller.SetFinalPosition(finalPos);
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(snapPos));
        }

        /// <summary>
        /// Flings to the nearest snap position to where the scroll snap would land after flinging at the supplied velocity.
        /// </summary>
        /// <param name="velocity">The velocity the scroll snap will move at in units per second, in the content's local space.</param>
        public void FlingToNearestSnapPosition(float velocity)
        {

            m_StartMovementEvent.Invoke(StartMovementEventType.Programmatic);
            m_Scroller.ForceFinish();

            Vector2 velocityV2 = Vector2.zero;
            velocityV2[axis] = velocity;

            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 min = new Vector2(m_MinPos.x - offset.x, m_MinPos.y - offset.y);
            Vector2 max = new Vector2(m_MaxPos.x - offset.x, m_MaxPos.y - offset.y);
            m_Scroller.Fling(m_Content.anchoredPosition, velocityV2, min, max);

            Vector2 finalPos = m_Scroller.finalPosition;
            Vector2 snapPos = FindClosestSnapPositionToPosition(finalPos);
            finalPos[axis] = snapPos[axis];
            m_Scroller.SetFinalPosition(finalPos);
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(snapPos));
        }
        #endregion

        #region Scrolling
        private void LateUpdate()
        {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);

            if (m_WaitingForEndScrolling && Time.time - m_TimeOfLastScroll > m_ScrollDelay)
            {
                m_WaitingForEndScrolling = false;
                DoEndManualMovement();
            }

            if (m_Scroller.ComputeScrollOffset())
            {
                m_Content.anchoredPosition = m_Scroller.currentPosition;
            }
            Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
            m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);

            m_ClosestSnapPositionIndex = m_SnapPositions.IndexOf(FindClosestSnapPositionToPosition(m_Content.anchoredPosition));

            if (m_Content.anchoredPosition != m_PrevPosition)
            {
                m_OnValueChanged.Invoke(new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition));
            }
            if (m_ClosestSnapPositionIndex != m_PrevClosestSnap)
            {
                m_ClosestSnapPositionChanged.Invoke(m_ClosestSnapPositionIndex);
            }
            if (m_Scroller.isFinished && m_PrevScrolling)
            {
                m_SnappedToItem.Invoke(m_ClosestSnapPositionIndex);
                m_Velocity = Vector2.zero;
            }


            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
            }
            UpdatePrevData();
        }

        private void ScrollBarPointerDown(PointerEventData ped)
        {
            m_StartMovementEvent.Invoke(StartMovementEventType.ScrollBar);
        }

        private void ScrollBarPointerUp(PointerEventData ped)
        {
            DoEndManualMovement();
        }

        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            if (!m_WaitingForEndScrolling)
            {
                m_StartMovementEvent.Invoke(StartMovementEventType.OnScroll);
                m_WaitingForEndScrolling = true;
                m_Scroller.ForceFinish();
            }
            m_TimeOfLastScroll = Time.time;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (m_MovementDirection == MovementDirection.Vertical)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            else
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        public virtual void OnBeginDrag(PointerEventData ped)
        {
            if (!IsActive())
            {
                return;
            }

            m_StartMovementEvent.Invoke(StartMovementEventType.OnBeginDrag);

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, ped.position, ped.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;

            m_Scroller.ForceFinish();
            m_Velocity = Vector2.zero;
        }

        public virtual void OnEndDrag(PointerEventData ped)
        {
            DoEndManualMovement();
        }

        private void DoEndManualMovement()
        {
            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 snapPos;

            if (!m_UseVelocity)
            {
                Vector2 finalPos = m_Content.anchoredPosition;
                if (m_SnapType == SnapType.SnapToNearest)
                {
                    snapPos = FindClosestSnapPositionToPosition(m_Content.anchoredPosition);
                    finalPos[axis] = snapPos[axis];
                }
                else if (m_SnapType == SnapType.SnapToLastPassed)
                {
                    snapPos = FindLastSnapPositionBeforePosition(m_Content.anchoredPosition, GetDirectionFromVelocity(m_Velocity, axis));
                    finalPos[axis] = snapPos[axis];
                }
                else
                {
                    snapPos = FindNextSnapAfterPosition(m_Content.anchoredPosition, GetDirectionFromVelocity(m_Velocity, axis));
                    finalPos[axis] = snapPos[axis];
                }
                finalPos.x = Mathf.Clamp(finalPos.x, m_MinPos.x, m_MaxPos.x);
                finalPos.y = Mathf.Clamp(finalPos.y, m_MinPos.y, m_MaxPos.y);
                m_Scroller.StartScroll(m_Content.anchoredPosition, finalPos, m_ScrollDurationMillis);
            }
            else
            {
                Vector2 min = new Vector2(m_MinPos.x - offset.x, m_MinPos.y - offset.y);
                Vector2 max = new Vector2(m_MaxPos.x - offset.x, m_MaxPos.y - offset.y);

                m_Scroller.Fling(m_Content.anchoredPosition, m_Velocity, min, max);
                Vector2 finalPos = m_Scroller.finalPosition;
                if (m_SnapType == SnapType.SnapToNearest)
                {
                    snapPos = FindClosestSnapPositionToPosition(finalPos);
                    finalPos[axis] = snapPos[axis];
                }
                else if (m_SnapType == SnapType.SnapToLastPassed)
                {
                    snapPos = FindLastSnapPositionBeforePosition(finalPos, GetDirectionFromVelocity(m_Velocity, axis));
                    finalPos[axis] = snapPos[axis];
                }
                else
                {
                    snapPos = FindNextSnapAfterPosition(finalPos, GetDirectionFromVelocity(m_Velocity, axis));
                    finalPos[axis] = snapPos[axis];
                }
                finalPos.x = Mathf.Clamp(finalPos.x, m_MinPos.x, m_MaxPos.x);
                finalPos.y = Mathf.Clamp(finalPos.y, m_MinPos.y, m_MaxPos.y);
                m_Scroller.SetFinalPosition(finalPos);
            }
            m_TargetItemSelected.Invoke(m_SnapPositions.IndexOf(snapPos));
        }

        public virtual void OnDrag(PointerEventData ped)
        {
            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, ped.position, ped.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;

            if (m_MovementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the normalized position of the scroll snap when it is snapped to the child. 
        /// </summary>
        /// <returns>Returns true if the supplied RectTransform is a child of the content.</returns>
        public bool GetNormalizedPositionOfChild(RectTransform child, out float normalizedPosition) //bool = is child of content
        {
            normalizedPosition = DistanceOnAxis(child.anchoredPosition, firstCalculateChild.anchoredPosition, axis) / m_TotalScrollableLength;
            return child.parent == m_Content;
        }

        /// <summary>
        /// Gets the position of the content in the contenet's local coordinates when it is snapped to the child. 
        /// </summary>
        /// <returns>Returns true if the supplied RectTransform is a child of the content.</returns>
        public bool GetPositionOfChild(RectTransform child, out Vector2 position) //bool = is child of content
        {
            Vector2 anchoredPos = m_Content.anchoredPosition;
            float normalizedPos;
            GetNormalizedPositionOfChild(child, out normalizedPos);
            SetNormalizedPosition(normalizedPos, axis);
            position = m_Content.anchoredPosition;
            m_Content.anchoredPosition = anchoredPos;
            return child.parent == m_Content;
        }

        /// <summary>
        /// Gets the snap position index of the child. 
        /// </summary>
        /// <returns>Returns true if the the supplied RectTransform is a child of the content.</returns>
        public bool GetIndexOfChild(RectTransform child, out int index) //bool = is it a snap position
        {
            Vector2 childPos;
            GetPositionOfChild(child, out childPos);
            index = m_SnapPositions.IndexOf(childPos);
            return m_SnapPositions.Contains(childPos);
        }

        /// <summary>
        /// Gets the normalized position of the content when snapped to the supplied snap position. 
        /// </summary>
        /// <returns>Returns true if the supplied snap position index is a valid snap position.</returns>
        public bool GetNormalizedPositionOfSnapPosition(int snapPositionIndex, out float normalizedPosition)
        {
            if (snapPositionIndex > 0 && snapPositionIndex < m_SnapPositions.Count)
            {
                RectTransform child;
                GetChildAtIndex(snapPositionIndex, out child);
                GetNormalizedPositionOfChild(child, out normalizedPosition);
                return true;
            }
            normalizedPosition = 0f;
            return false;
        }

        /// <summary>
        /// Gets the position of the content in the content's local coordinates when snapped to the snap position at the specified coordinates. 
        /// </summary>
        /// <returns>Returns true if the supplied snap position index is a valid snap position.</returns>
        public bool GetSnapPositionAtIndex(int snapPositionIndex, out Vector2 location) //bool = is it a snap position
        {
            if (snapPositionIndex > 0 && snapPositionIndex < m_SnapPositions.Count)
            {
                location = m_SnapPositions[snapPositionIndex];
                return true;
            }
            location = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Gets the child RectTransform of the content for the supplied snap position index. 
        /// </summary>
        /// <returns>Returns true if the supplied snap position index is a valid snap position.</returns>
        public bool GetChildAtIndex(int snapPositionIndex, out RectTransform child)
        {
            if (snapPositionIndex > 0 && snapPositionIndex < m_SnapPositions.Count)
            {
                child = m_ChildrenForSnappingFromStartToEnd[snapPositionIndex];
                return true;
            }
            child = null;
            return false;
        }

        #endregion

        #region Calculations
        private Vector2 FindClosestSnapPositionToPosition(Vector2 position)
        {
            EnsureLayoutHasRebuilt();
            Vector3 closest = Vector3.zero;
            float distance = Mathf.Infinity;

            foreach (Vector2 snapPosition in m_SnapPositions)
            {
                if (DistanceOnAxis(position, snapPosition, axis) < distance)
                {
                    distance = DistanceOnAxis(position, snapPosition, axis);
                    closest = snapPosition;
                }
            }
            return closest;
        }

        private Vector2 FindLastSnapPositionBeforePosition(Vector2 position, Direction direction)
        {
            EnsureLayoutHasRebuilt();
            Vector2 distanceReferencePos = m_StartPosition;
            if (direction == Direction.TowardsStart)
            {
                m_SnapPositions.Reverse();
                distanceReferencePos = m_EndPosition;
            }

            Vector2 selectedSnapPosition = Vector2.zero;
            float distanceFromPosition = Mathf.Infinity;
            float maxDistance = DistanceOnAxis(distanceReferencePos, position, axis);

            foreach (Vector2 snapPosition in m_SnapPositions)
            {
                if (DistanceOnAxis(position, snapPosition, axis) < distanceFromPosition && DistanceOnAxis(distanceReferencePos, snapPosition, axis) <= maxDistance)
                {
                    distanceFromPosition = DistanceOnAxis(position, snapPosition, axis);
                    selectedSnapPosition = snapPosition;
                }
            }

            if (direction == Direction.TowardsStart)
            {
                m_SnapPositions.Reverse();
            }
            return selectedSnapPosition;
        }

        private Vector2 FindNextSnapAfterPosition(Vector2 position, Direction direction)
        {
            EnsureLayoutHasRebuilt();
            Vector2 distanceReferencePos = m_StartPosition;
            if (direction == Direction.TowardsEnd)
            {
                m_SnapPositions.Reverse();
                distanceReferencePos = m_EndPosition;
            }

            Vector2 selectedSnapPosition = Vector2.zero;
            float distanceFromPosition = Mathf.Infinity;
            float maxDistance = DistanceOnAxis(distanceReferencePos, position, axis);
            
            foreach (Vector2 snapPosition in m_SnapPositions)
            {
                if (DistanceOnAxis(position, snapPosition, axis) < distanceFromPosition && DistanceOnAxis(distanceReferencePos, snapPosition, axis) <= maxDistance)
                {
                    distanceFromPosition = DistanceOnAxis(position, snapPosition, axis);
                    selectedSnapPosition = snapPosition;
                }
            }

            if (direction == Direction.TowardsEnd)
            {
                m_SnapPositions.Reverse();
            }
            return selectedSnapPosition;
        }

        private Direction GetDirectionFromVelocity(Vector2 velocity, int axis)
        {
            if (axis == 0)
            {
                if (Mathf.Sign(velocity[axis]) > 0)
                {
                    return Direction.TowardsStart;
                }
                else
                {
                    return Direction.TowardsEnd;
                }
            }
            else
            {
                if (Mathf.Sign(velocity[axis]) > 0)
                {
                    return Direction.TowardsEnd;
                }
                else
                {
                    return Direction.TowardsStart;
                }
            }
        }

        private float DistanceOnAxis(Vector2 posOne, Vector2 posTwo, int axis)
        {
            return Mathf.Abs(posOne[axis] - posTwo[axis]);
        }

        private bool TransformAIsNearerStart(RectTransform transformA, RectTransform transformB)
        {
            if (movementDirection == MovementDirection.Horizontal)
            {
                return transformA.anchoredPosition[axis] < transformB.anchoredPosition[axis];
            }
            else
            {
                return transformA.anchoredPosition[axis] > transformB.anchoredPosition[axis];
            }
        }

        private float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        private Vector2 RoundVector2ToInts(Vector2 vector)
        {
            return new Vector2((int)vector.x, (int)vector.y);
        }

        private Interpolator GetInterpolator()
        {
            Interpolator interpolator = new Scroller.ViscousFluidInterpolator();
            switch (m_InterpolatorType)
            {
                case InterpolatorType.Accelerate:
                    interpolator = new Scroller.AccelerateInterpolator();
                    break;
                case InterpolatorType.AccelerateDecelerate:
                    interpolator = new Scroller.AccelerateDecelerateInterpolator();
                    break;
                case InterpolatorType.Anticipate:
                    interpolator = new Scroller.AnticipateInterpolator(m_Tension);
                    break;
                case InterpolatorType.AnticipateOvershoot:
                    interpolator = new Scroller.AnticipateOvershootInterpolator(m_Tension);
                    break;
                case InterpolatorType.Decelerate:
                    interpolator = new Scroller.DecelerateInterpolator();
                    break;
                case InterpolatorType.Linear:
                    interpolator = new Scroller.LinearInterpolator();
                    break;
                case InterpolatorType.Overshoot:
                    interpolator = new Scroller.OvershootInterpolator(m_Tension);
                    break;
            }

            return interpolator;
        }
        #endregion

        #region Control
        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null && m_AvailableForSnappingTo.Count > 0;
        }

        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            UpdateLayout();
        }

        public virtual void SetLayoutVertical()
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();
        }

        private void RebuildLayoutGroups()
        {
            if (contentIsLayoutGroup)
            {
                m_LayoutGroup.CalculateLayoutInputHorizontal();
                m_LayoutGroup.CalculateLayoutInputVertical();
                m_LayoutGroup.SetLayoutHorizontal();
                m_LayoutGroup.SetLayoutVertical();
            }
        }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        private void EnsureLayoutUpdated()
        {
            if (!m_HasUpdatedLayout)
            {
                UpdateLayout();
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        private void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            EnsureLayoutUpdated();
            UpdateBounds();
            // How much the content is larger than the view.
            float scrollableLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            //the amount of content below the left corner of the viewbounds
            float amountBelowLeftCorner = (axis == 0) ? value * scrollableLength : (1 - value) * scrollableLength;
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - amountBelowLeftCorner;
            // The new content localPosition, in the space of the view.
            float newLocalPosition = m_Content.localPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 localPosition = m_Content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                m_Content.localPosition = localPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }

        private void UpdateBounds()
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            Vector3 excess = m_ViewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (m_Content.pivot.x - 0.5f);
                contentSize.x = m_ViewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (m_Content.pivot.y - 0.5f);
                contentSize.y = m_ViewBounds.size.y;
            }

            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (m_Content == null)
                return new Bounds();

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            var toLocal = viewRect.worldToLocalMatrix;
            m_Content.GetWorldCorners(m_Corners);
            for (int j = 0; j < 4; j++)
            {
                Vector3 v = toLocal.MultiplyPoint3x4(m_Corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            Vector2 offset = Vector2.zero;

            Vector2 min = m_ContentBounds.min;
            Vector2 max = m_ContentBounds.max;

            if (movementDirection == MovementDirection.Horizontal || (movementDirection == MovementDirection.Vertical && !m_LockOtherDirection))
            {
                min.x += delta.x;
                max.x += delta.x;
                if (min.x > m_ViewBounds.min.x)
                    offset.x = m_ViewBounds.min.x - min.x;
                else if (max.x < m_ViewBounds.max.x)
                    offset.x = m_ViewBounds.max.x - max.x;
            }

            if (movementDirection == MovementDirection.Vertical || (movementDirection == MovementDirection.Horizontal && !m_LockOtherDirection))
            {
                min.y += delta.y;
                max.y += delta.y;
                if (max.y < m_ViewBounds.max.y)
                    offset.y = m_ViewBounds.max.y - max.y;
                else if (min.y > m_ViewBounds.min.y)
                    offset.y = m_ViewBounds.min.y - min.y;
            }

            return offset;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                if (m_ContentBounds.size.x > 0)
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0)
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (m_LockOtherDirection)
            {
                if (movementDirection == MovementDirection.Vertical)
                    position.x = m_Content.anchoredPosition.x;
                if (movementDirection == MovementDirection.Horizontal)
                    position.y = m_Content.anchoredPosition.y;
            }

            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        private void UpdatePrevData()
        {
            if (m_Content == null)
            {
                m_PrevPosition = Vector2.zero;
            }
            else
            {
                m_PrevPosition = m_Content.anchoredPosition;
            }

            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
            m_PrevClosestSnap = m_ClosestSnapPositionIndex;
            m_PrevScrolling = !m_Scroller.isFinished;
        }

        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
        #endregion

        private void OnDrawGizmos()
        {

            if (m_DrawGizmos)
            {
                Vector3[] corners = new Vector3[4];
                m_Content.GetWorldCorners(corners);

                Vector3 topLeft = corners[1];
                Vector3 topRight = corners[2];
                Vector3 bottomRight = corners[3];
                Vector3 bottomLeft = corners[0];

                Gizmos.color = Color.white;
                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);

                Vector3 topDirection = topRight - topLeft;
                Vector3 leftDirection = bottomLeft - topLeft;
                Vector3 topEndPoint = topLeft + topDirection.normalized * GetGizmoSize(topLeft);
                Vector3 leftEndPoint = topLeft + leftDirection.normalized * GetGizmoSize(topLeft);
                Vector3 perpendicularDirection = Vector3.Cross(topDirection, leftDirection);

                Vector3 arrowDirectionOne = Quaternion.AngleAxis(-135, perpendicularDirection) * leftDirection;
                Vector3 arrowDirectionTwo = Quaternion.AngleAxis(135, perpendicularDirection) * leftDirection;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(topLeft, topEndPoint);
                Gizmos.DrawLine(topLeft, leftEndPoint);
                Gizmos.DrawRay(leftEndPoint, arrowDirectionOne.normalized * GetGizmoSize(leftEndPoint) * .25f);
                Gizmos.DrawRay(leftEndPoint, arrowDirectionTwo.normalized * GetGizmoSize(leftEndPoint) * .25f);
                Gizmos.DrawRay(topEndPoint, -(arrowDirectionOne.normalized * GetGizmoSize(leftEndPoint) * .25f));
                Gizmos.DrawRay(topEndPoint, arrowDirectionTwo.normalized * GetGizmoSize(leftEndPoint) * .25f);

                Vector3[] childCorners = new Vector3[4];
                foreach (RectTransform child in m_Content)
                {
                    child.GetWorldCorners(childCorners);
                    if (child.position.x < topLeft.x || child.position.y > topLeft.y)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(childCorners[1], childCorners[2]);
                        Gizmos.DrawLine(childCorners[2], childCorners[3]);
                        Gizmos.DrawLine(childCorners[3], childCorners[0]);
                        Gizmos.DrawLine(childCorners[0], childCorners[1]);
                    }
                    else if (m_AvailableForSnappingTo.Contains(child))
                    {
                        Gizmos.color = Color.cyan;

                        if (m_MovementDirection == MovementDirection.Horizontal)
                        {
                            Gizmos.DrawRay(child.position, leftDirection.normalized * GetGizmoSize(child.position) * .5f);
                            Gizmos.DrawRay(child.position, -(leftDirection.normalized * GetGizmoSize(child.position) * .5f));
                        }
                        else
                        {
                            Gizmos.DrawRay(child.position, topDirection.normalized * GetGizmoSize(child.position) * .5f);
                            Gizmos.DrawRay(child.position, -(topDirection.normalized * GetGizmoSize(child.position) * .5f));
                        }
                    }
                }
            }
        }

        private float GetGizmoSize(Vector3 position)
        {
            Camera current = Camera.current;
            position = Gizmos.matrix.MultiplyPoint(position);

            if (current)
            {
                Transform transform = current.transform;
                Vector3 position2 = transform.position;
                float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
                Vector3 a = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
                Vector3 b = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
                float magnitude = (a - b).magnitude;
                return 80f / Mathf.Max(magnitude, 0.0001f);
            }

            return 20f;
        }

        protected override void OnValidate()
        {
            m_Friction = Mathf.Max(m_Friction, .001f);
            m_Tension = Mathf.Max(m_Tension, 0);

            m_MinDurationMillis = Mathf.Max(m_MinDurationMillis, 1);
            m_MaxDurationMillis = Math.Max(m_MaxDurationMillis, Mathf.Max(m_MinDurationMillis, 1));
            m_ScrollDurationMillis = Mathf.Max(m_ScrollDurationMillis, 1);

            m_ScrollSensitivity = Mathf.Max(m_ScrollSensitivity, 0);
            m_ScrollDelay = Mathf.Max(m_ScrollDelay, 0);

            if (m_Scroller != null && (m_Scroller.interpolator != GetInterpolator() || m_Tension != m_PrevTension || m_Friction != m_PrevFriction || m_MinDurationMillis != m_PrevMinDuration || m_MaxDurationMillis != m_PrevMaxDuration))
            {
                m_PrevTension = m_Tension;
                m_PrevFriction = m_Friction;
                m_PrevMinDuration = m_MinDurationMillis;
                m_PrevMaxDuration = m_MaxDurationMillis;
                m_Scroller.AbortAnimation();
                m_Scroller = new Scroller(m_Friction, m_MinDurationMillis, m_MaxDurationMillis, GetInterpolator());
            }

            if (contentIsHorizonalLayoutGroup)
            {
                m_MovementDirection = MovementDirection.Horizontal;
            }

            if (contentIsVerticalLayoutGroup)
            {
                m_MovementDirection = MovementDirection.Vertical;
            }

            SetDirtyCaching();
        }

        [MenuItem("GameObject/UI/ScrollSnaps/DirectionalScrollSnap", false, 10)]
        private static void CreateDirectionalScrollSnap(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;

            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null || !canvas.gameObject.activeInHierarchy)
                {
                    parent = new GameObject("Canvas");
                    parent.layer = LayerMask.NameToLayer("UI");
                    canvas = parent.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    parent.AddComponent<CanvasScaler>();
                    parent.AddComponent<GraphicRaycaster>();
                    Undo.RegisterCreatedObjectUndo(parent, "Create " + parent.name);

                    EventSystem evsy = FindObjectOfType<EventSystem>();
                    if (evsy == null || !evsy.gameObject.activeInHierarchy)
                    {
                        GameObject eventSystem = new GameObject("EventSystem");
                        eventSystem.AddComponent<EventSystem>();
                        eventSystem.AddComponent<StandaloneInputModule>();

                        Undo.RegisterCreatedObjectUndo(eventSystem, "Create " + eventSystem.name);
                    }
                }
                else
                {
                    parent = canvas.gameObject;
                }
            }

            int numChildren = 2;

            GameObject GO = new GameObject("Directional Scroll Snap");
            RectTransform rectTransform = GO.AddComponent<RectTransform>();
            DirectionalScrollSnap scrollSnap = GO.AddComponent<DirectionalScrollSnap>();
            Image image = GO.AddComponent<Image>();

            GameObject content = new GameObject("Content");
            RectTransform contentRectTransform = content.AddComponent<RectTransform>();
            Image contentImage = content.AddComponent<Image>();


            GO.transform.SetParent(parent.transform, false);
            rectTransform.sizeDelta = new Vector2(200, 200);
            scrollSnap.content = contentRectTransform;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = Color.red;

            content.transform.SetParent(GO.transform, false);
            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(0, 1);
            contentRectTransform.sizeDelta = new Vector2(200 + (150 * (numChildren - 1)), 200);
            contentRectTransform.anchoredPosition = new Vector2(100, -100);
            contentImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            contentImage.type = Image.Type.Sliced;
            contentImage.color = new Color(0, 0, 1, .5f);

            for (int i = 0; i < numChildren; i++)
            {
                GameObject child = new GameObject("Child Item");
                RectTransform childRectTransform = child.AddComponent<RectTransform>();
                child.AddComponent<Image>();

                child.transform.SetParent(content.transform, false);
                childRectTransform.anchorMin = new Vector2(0, 1);
                childRectTransform.anchorMax = new Vector2(0, 1);
                childRectTransform.sizeDelta = new Vector2(100, 100);
                childRectTransform.anchoredPosition = new Vector2(100 + (150 * i), -100);
            }

            GameObjectUtility.SetParentAndAlign(GO, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(GO, "Create " + GO.name);
            Selection.activeObject = GO;


            // Find the best scene view
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
            {
                sceneView = SceneView.sceneViews[0] as SceneView;
            }

            // Couldn't find a SceneView. Don't set position.
            if (sceneView == null || sceneView.camera == null)
            {
                return;
            }

            // Create world space Plane from canvas position.
            RectTransform canvasRTransform = parent.GetComponent<RectTransform>();
            Vector2 localPlanePosition;
            Camera camera = sceneView.camera;
            Vector3 position = Vector3.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRTransform, new Vector2(camera.pixelWidth / 2, camera.pixelHeight / 2), camera, out localPlanePosition))
            {
                // Adjust for canvas pivot
                localPlanePosition.x = localPlanePosition.x + canvasRTransform.sizeDelta.x * canvasRTransform.pivot.x;
                localPlanePosition.y = localPlanePosition.y + canvasRTransform.sizeDelta.y * canvasRTransform.pivot.y;

                localPlanePosition.x = Mathf.Clamp(localPlanePosition.x, 0, canvasRTransform.sizeDelta.x);
                localPlanePosition.y = Mathf.Clamp(localPlanePosition.y, 0, canvasRTransform.sizeDelta.y);

                // Adjust for anchoring
                position.x = localPlanePosition.x - canvasRTransform.sizeDelta.x * rectTransform.anchorMin.x;
                position.y = localPlanePosition.y - canvasRTransform.sizeDelta.y * rectTransform.anchorMin.y;

                Vector3 minLocalPosition;
                minLocalPosition.x = canvasRTransform.sizeDelta.x * (0 - canvasRTransform.pivot.x) + rectTransform.sizeDelta.x * rectTransform.pivot.x;
                minLocalPosition.y = canvasRTransform.sizeDelta.y * (0 - canvasRTransform.pivot.y) + rectTransform.sizeDelta.y * rectTransform.pivot.y;

                Vector3 maxLocalPosition;
                maxLocalPosition.x = canvasRTransform.sizeDelta.x * (1 - canvasRTransform.pivot.x) - rectTransform.sizeDelta.x * rectTransform.pivot.x;
                maxLocalPosition.y = canvasRTransform.sizeDelta.y * (1 - canvasRTransform.pivot.y) - rectTransform.sizeDelta.y * rectTransform.pivot.y;

                position.x = Mathf.Clamp(position.x, minLocalPosition.x, maxLocalPosition.x);
                position.y = Mathf.Clamp(position.y, minLocalPosition.y, maxLocalPosition.y);
            }

            rectTransform.anchoredPosition = position;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        protected class ScrollBarEventsListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action<PointerEventData> onPointerDown;
            public Action<PointerEventData> onPointerUp;

            public virtual void OnPointerDown(PointerEventData ped)
            {
                onPointerDown(ped);
            }

            public virtual void OnPointerUp(PointerEventData ped)
            {
                onPointerUp(ped);
            }
        }
    }
}
