using Masot.Standard.Input;
using Masot.Standard.Utility;
using UnityEngine;

namespace Masot.CameraControl
{
    public class PerspectiveCameraZoom : MonoBehaviour
    {
        private IMathFunction _zoomFunction;
        private float totalDst = 0;

        public BindableProperty<Vector2Int> dstToplaneLimit = new BindableProperty<Vector2Int>(new Vector2Int(1, 10));
        public BindableProperty<MathFunction> zoomFunction = new BindableProperty<MathFunction>(MathFunction.Linear);
        public BindableProperty<int> steps = new BindableProperty<int>(1);
        public BindableProperty<int> step = new BindableProperty<int>(0);

        private void OnEnable()
        {
            dstToplaneLimit.OnChange += OnDstToPlaneLimitChange;
            zoomFunction.OnChange += OnMathFunctionChange;
            steps.OnChange += OnStepsChange;
            step.CanChange += CanStepChange;
            step.OnChange += OnStepChange;

            _zoomFunction = MathMethodFactory.CreateMathFunction(zoomFunction);

            InputController.Instance.MouseScrollEventHandler += OnMouseScroll;

            totalDst = dstToplaneLimit.Value.y - dstToplaneLimit.Value.x;
            SetPositionDependingOnStep(step.Value, steps.Value, dstToplaneLimit.Value);
        }

        private void OnDisable()
        {
            InputController.Instance.MouseScrollEventHandler -= OnMouseScroll;
        }

        private void OnDstToPlaneLimitChange(BindableProperty<Vector2Int> property)
        {
            totalDst = property.Value.y - property.Value.x;
            ChangeSteps(property.Value, steps);
        }

        private void OnStepsChange(BindableProperty<int> property)
        {
            ChangeSteps(dstToplaneLimit, property.Value);
        }

        private void ChangeSteps(Vector2Int limit, int steps)
        {
            SetPositionDependingOnStep(step.Value, steps, limit);
        }

        private void OnMathFunctionChange(BindableProperty<MathFunction> property)
        {
            _zoomFunction = MathMethodFactory.CreateMathFunction(property.Value);
        }

        private bool CanStepChange(BindableProperty<int> property, int value)
        {
            if (value < 0 || value > steps)
            {
                return false;
            }

            return true;
        }

        private void OnStepChange(BindableProperty<int> property)
        {
            SetPositionDependingOnStep(property, steps, dstToplaneLimit);
        }

        private void OnMouseScroll(MouseScrollEventArgs e)
        {
            if (e.ScrollValue < 0)
            {
                step.Value--;
            }
            else
            {
                step.Value++;
            }
        }

        private void SetPositionDependingOnStep(int step, int steps, Vector2Int limit)
        {
            var pos = Vector3.ProjectOnPlane(transform.position, Vector3.forward);
            var t = _zoomFunction.Evaluate(Mathf.InverseLerp(0, steps, step));
            transform.position = pos - transform.forward * ((t * totalDst) + limit.x);
        }
    }
}