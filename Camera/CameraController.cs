using Masot.Standard.Input;
using Masot.Standard.Movement;
using Masot.Standard.Utility;
using System.Collections;
using UnityEngine;

namespace Masot.CameraControl
{
    internal class CameraController : DirectInputMovement
    {
        [SerializeField]
        private Transform snappedTransform = null;
        [SerializeField]
        private Transform selected = null;
        [SerializeField]
        private new Camera camera = null;
        private Color originalColor = Color.white;
        private TransformInfo original = null;

        public InputDefine rotateAroundInput = new InputDefine(KeyCode.Mouse1, GetKeyType.Hold);
        public InputDefine selectInput = new InputDefine(KeyCode.Mouse0, GetKeyType.Press);
        public InputDefine UnsnapInput = new InputDefine(KeyCode.Escape, GetKeyType.Press);
        public InputDefine centerToObject = new InputDefine(KeyCode.Space, GetKeyType.Hold);

        public bool snapBack = true;
        public bool snapOnZoom = false;
        public bool ZoomToCursor = false;
        public float doublePressDelay = 0.5f;

        public bool dragInertia = true;
        public float inertiaTime = 1f;
        public float inertiaCutoff = 0.001f;
        [Range(0, 1)]
        public float inertiaDamper = 0.5f;
        public MathFunction functionType;
        private bool inertia = false;
        private Vector2 inertiaValue = Vector2.zero;
        private Vector2 inertiaMax = Vector2.zero;

        [Range(0, 1)]
        public float PercentageOfSnappedObjectOnScreenOnSnap = 0.5f;

        private void Moved()
        {
            Unsnap();
            //StartMovingCoroutine(Vector3.zero);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            OnMove.AddListener(Moved);
            InputController.Instance.Register<MouseAxisDragEventArgs>(rotateAroundInput, OnDrag);
            InputController.Instance.Register<MousePositionEventArgs>(selectInput, OnSelect);
            InputController.Instance.Register(UnsnapInput, OnUnsnap);

            if (camera == null)
            {
                camera = GetComponent<Camera>();
                Debug.Assert(camera != null, "No Camera found");
            }

            if (snappedTransform != null)
            {
                SnapTo(snappedTransform);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            OnMove.RemoveListener(Moved);
            InputController.Instance.Remove<MouseAxisDragEventArgs>(rotateAroundInput, OnDrag);
            InputController.Instance.Remove<MousePositionEventArgs>(selectInput, OnSelect);
            InputController.Instance.Remove(UnsnapInput, OnUnsnap);
        }

        public void SnapTo(Transform snapTo)
        {
            if (snapTo == null)
            {
                return;
            }

            if (original is null)
                original = new TransformInfo(transform.parent, transform.position, transform.rotation);

            //change into orbital body later
            //var orbitalObject = snapTo.GetComponent<Assets.Scripts.Game.Structures.MineralMapping.OrbitalBodyGenerator>();

            var localPos = transform.localPosition;

            snappedTransform = snapTo;
            transform.parent = snapTo;

            rigidbody2D.velocity = Vector2.zero;

            var renderer = snappedTransform.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                originalColor = renderer.material.color;
                renderer.material.color = Color.red;
            }

            //transform.localPosition = localPos;

            InputController.Instance.Register(centerToObject, OnCenterToObject);
        }

        private void OnCenterToObject(EventArgsBase _)
        {
            var offset = Vector3.Project(snappedTransform.position - transform.position, transform.forward);
            transform.position = snappedTransform.position + offset;
        }

        private void OnUnsnap(EventArgsBase _)
        {
            Unsnap();
        }

        public void Unsnap()
        {
            if (selected == null)
            {
                return;
            }

            ChangeSelectedBack(selected);

            if (snappedTransform == null)
            {
                return;
            }

            if (snapBack && original != null)
            {
                transform.parent = original.parent;
                transform.position = original.position;
                transform.rotation = original.rotation;
            }

            rigidbody2D.velocity = Vector2.zero;

            var renderer = snappedTransform.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }

            original = null;
            snappedTransform = null;

            InputController.Instance.Remove(centerToObject, OnCenterToObject);
        }

        private Vector2 screenStart = Vector2.zero;
        private void OnDragStart(MousePositionEventArgs e)
        {
            screenStart = e.ScreenPosition;
        }

        //if not snapped move on screen axis
        //if snapped move on 3d object axis around the snapped object
        //scale by something
        //by snapped object size or something on screen
        //rotate around object
        private void OnDrag(MouseAxisDragEventArgs e)
        {
            if (dragInertia)
            {
                MoveBy(e.Drag);
                return;
            }

            MoveByInertia(e.Drag);
        }

        public Vector3 Velocity = Vector3.zero;
        public float dampTime = 0;

        private void MoveBy(Vector2 dragDelta)
        {
            dragDelta *= movementScale;

            if (snappedTransform == null)
            {
                //move on 2d axis
                //transform.position += Vector3.SmoothDamp(transform.position, transform.right * -dragDelta.x + transform.up * -dragDelta.y, ref Velocity, dampTime);
                transform.position += transform.right * -dragDelta.x + transform.up * -dragDelta.y;
                return;
            }

            //move around a snapped target
            transform.RotateAround(snappedTransform.position, transform.up, dragDelta.x);
            transform.RotateAround(snappedTransform.position, transform.right, -dragDelta.y);
        }

        private void MoveByInertia(Vector2 dragDelta)
        {
            inertiaValue += dragDelta;
            inertiaMax = inertiaValue;

            if (inertia)
            {
                return;
            }
            StartCoroutine("InertiaProcess");
        }

        private IEnumerator InertiaProcess()
        {
            inertia = true;
            var fnc = MathMethodFactory.CreateMathFunction(functionType);
            fnc.Offset = new Vector2(1, 0);

            while (inertiaValue.sqrMagnitude > inertiaCutoff * inertiaCutoff)
            {
                yield return new WaitForFixedUpdate();
                var t = fnc.Evaluate(1 - inertiaValue.sqrMagnitude / inertiaMax.sqrMagnitude);
                inertiaValue -= inertiaMax * t * inertiaDamper;
                MoveBy(inertiaValue);
            }

            inertia = false;
        }

        public float movementScale = 1;

        protected override void OnUp(MovementWrapper2DSettings settings)
        {
        }

        protected override void OnLeft(MovementWrapper2DSettings settings)
        {
        }

        protected override void OnDown(MovementWrapper2DSettings settings)
        {
        }

        protected override void OnRight(MovementWrapper2DSettings settings)
        {
        }

        private float lastSelectTime = 0;
        private Color originalMeshColor = Color.white;
        //select object double click
        private void OnSelect(MousePositionEventArgs e)
        {
            var ray = new MouseRaycast3DEventArgs(camera, e.ScreenPosition, e.WorldPosition, e.Input);
            if (!ray.DidHit)
            {
                ChangeSelectedBack(selected);
                selected = null;
                lastSelectTime = Time.realtimeSinceStartup;
                return;
            }

            var newSelected = ray.Hit.transform;

            if (selected == newSelected)
            {
                //selecting the same shit while snapped
                if (selected == snappedTransform)
                {
                    lastSelectTime = Time.realtimeSinceStartup;
                    return;
                }

                //selecting the same shit while not snapped
                if (lastSelectTime + doublePressDelay > Time.realtimeSinceStartup)
                {
                    //double click on the same object
                    //snap
                    SnapTo(selected);
                }
            }
            else
            {
                //change color and shit

                //change old
                ChangeSelectedBack(selected);
                //first selected object event
                //todo call like Ui or shit
                //change new
                MeshRenderer meshRenderer;
                if (newSelected.TryGetComponent(out meshRenderer))
                {
                    originalMeshColor = meshRenderer.material.color;
                    meshRenderer.material.color = Color.green;
                }
            }

            selected = newSelected;
            lastSelectTime = Time.realtimeSinceStartup;
        }

        private void ChangeSelectedBack(Transform transform)
        {
            if (transform == null)
            {
                return;
            }

            MeshRenderer meshRenderer;
            if (!transform.TryGetComponent(out meshRenderer))
            {
                return;
            }

            meshRenderer.material.color = originalMeshColor;
        }
    }
}