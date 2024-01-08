using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Color = UnityEngine.Color;

//[RequireComponent(typeof(Selectable))]
public class GizmoHandler : MonoBehaviour
{
    private ObjectTransformGizmo _translateGizmo;
    private ObjectTransformGizmo _rotateGizmo;
    private ObjectTransformGizmo _scaleGizmo;
    private ObjectTransformGizmo _universalGizmo;
    private bool _gizmosInitialized;
    public static bool GizmoBeingUsed { get; private set; }
    public bool GizmoUsedLastFrame { get; private set; }
    public bool IsBeingUsed { get; private set; }
    private Vector3 _positionBeforeStartDrag;
    private Vector3 _localScaleBeforeStartDrag;
    public Vector3 CurrentScaleDrag { get; private set; }
    public UnityEvent GizmoDragEnded { get; } = new UnityEvent();
    public UnityEvent GizmoDragPostUpdate { get; } = new UnityEvent();
    private Vector3 _lastCircleIntersectPoint;
    //private static readonly Color _colorTransparent = new(0, 0, 0, 0);

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        GizmoSelector.GizmoModeChanged += GizmoModeChanged;
        UI_ToggleSnapping.SnappingToggled.AddListener(EnableGizmo);
        CameraManager.CameraChanged.AddListener(EnableGizmo);
    }

    private void GizmoModeChanged(object sender = null, EventArgs e = null)
    {
        EnableGizmo();
    }

    private void OnDestroy()
    {
        GizmoSelector.GizmoModeChanged -= GizmoModeChanged;
        UI_ToggleSnapping.SnappingToggled.RemoveListener(EnableGizmo);
        CameraManager.CameraChanged.RemoveListener(EnableGizmo);
    }

    public void SelectableSelected()
    {
        CreateGizmos();
        EnableGizmo();
    }

    public void SelectableDeselected()
    {
        EnableGizmo();
    }

    private void EnableGizmo()
    {
        if (!_gizmosInitialized) return;

        _translateGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Translate && _selectable.IsSelected);
        _rotateGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Rotate && _selectable.IsSelected);
        _scaleGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Scale && _selectable.IsSelected);

        if (_selectable.IsSelected)
        {
            if (_translateGizmo.Gizmo.IsEnabled)
            {
                _translateGizmo.Gizmo.Transform.Position3D = transform.position;
                //_translateGizmo.Gizmo.Transform.Rotation3D = transform.rotation;
                _translateGizmo.Gizmo.MoveGizmo.SetSnapEnabled(UI_ToggleSnapping.SnappingEnabled);
                //RTGizmosEngine.Get.MoveGizmoSettings3D.SetXSnapStep

                bool xMovementAllowed = _selectable.IsGizmoSettingAllowed(GizmoType.Move, Axis.X);
                bool yMovementAllowed = _selectable.IsGizmoSettingAllowed(GizmoType.Move, Axis.Y);
                bool zMovementAllowed = _selectable.IsGizmoSettingAllowed(GizmoType.Move, Axis.Z);

                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderVisible(0, AxisSign.Positive, xMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderVisible(1, AxisSign.Positive, yMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderVisible(2, AxisSign.Positive, zMovementAllowed);

                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderCapVisible(0, AxisSign.Positive, xMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderCapVisible(1, AxisSign.Positive, yMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetSliderCapVisible(2, AxisSign.Positive, zMovementAllowed);

                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetDblSliderVisible(PlaneId.XY, xMovementAllowed && yMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetDblSliderVisible(PlaneId.YZ, yMovementAllowed && zMovementAllowed);
                RTGizmosEngine.Get.MoveGizmoLookAndFeel3D.SetDblSliderVisible(PlaneId.ZX, zMovementAllowed && xMovementAllowed);
            }
            else if (_rotateGizmo.Gizmo.IsEnabled)
            {
                _rotateGizmo.Gizmo.Transform.Position3D = transform.position;
                _rotateGizmo.Gizmo.Transform.Rotation3D = transform.rotation;
                _rotateGizmo.Gizmo.RotationGizmo.SetSnapEnabled(UI_ToggleSnapping.SnappingEnabled);

                Debug.Log(CameraManager.ActiveCamera.name);

                bool allowHorizontal = CameraManager.ActiveCamera.GetComponent<OperatingRoomCamera>().CameraType != OperatingRoomCameraType.OrthoSide ? true : false;
                bool allowVertical = CameraManager.ActiveCamera.GetComponent<OperatingRoomCamera>().CameraType != OperatingRoomCameraType.OrthoCeiling ? true : false;

                RTGizmosEngine.Get.RotationGizmoLookAndFeel3D.SetAxisVisible(0, _selectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.X));
                RTGizmosEngine.Get.RotationGizmoLookAndFeel3D.SetAxisVisible(1, allowVertical ? _selectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Y) : false);
                RTGizmosEngine.Get.RotationGizmoLookAndFeel3D.SetAxisVisible(2, allowHorizontal ? _selectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z) : false);
            }
            else if (_scaleGizmo.Gizmo.IsEnabled)
            {
                _scaleGizmo.Gizmo.Transform.LocalPosition3D = transform.position;
                _scaleGizmo.Gizmo.Transform.Rotation3D = transform.rotation;
                _scaleGizmo.Gizmo.ScaleGizmo.SetSnapEnabled(UI_ToggleSnapping.SnappingEnabled);

                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderVisible(0, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.X));
                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderVisible(1, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Y));
                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderVisible(2, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z));

                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderCapVisible(0, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.X));
                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderCapVisible(1, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Y));
                RTGizmosEngine.Get.ScaleGizmoLookAndFeel3D.SetSliderCapVisible(2, AxisSign.Positive, _selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z));
            }
        }

        //_universalGizmo.Gizmo.SetEnabled(GizmoSelector.CurrentGizmoMode == GizmoMode.Universal && _selectable.IsSelected);
        //_universalGizmo.Gizmo.Transform.Position3D = transform.position;
    }

    private void CreateGizmos()
    {
        if (_gizmosInitialized) return;

        _translateGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
        _translateGizmo.SetTargetObject(gameObject);
        //_translateGizmo.Gizmo.MoveGizmo.SetVertexSnapTargetObjects(new List<GameObject> { gameObject });
        _translateGizmo.SetTransformSpace(GizmoSpace.Local);
        _translateGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _translateGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _translateGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;
        _translateGizmo.Gizmo.PreDragUpdate += OnGizmoPreDragUpdate;
        _translateGizmo.Gizmo.PreDragBegin += OnGizmoPreDragBegin;
        _translateGizmo.SetTransformPivot(GizmoObjectTransformPivot.ObjectMeshPivot);

        _rotateGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
        _rotateGizmo.SetTargetObject(gameObject);
        _rotateGizmo.SetTransformSpace(GizmoSpace.Local);
        _rotateGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _rotateGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _rotateGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;
        _rotateGizmo.SetTransformPivot(GizmoObjectTransformPivot.ObjectMeshPivot);

        _scaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        //_scaleGizmo.SetTargetObject(gameObject);
        _rotateGizmo.SetTransformSpace(GizmoSpace.Local);
        _scaleGizmo.Gizmo.PostDragEnd += OnGizmoPostDragEnd;
        _scaleGizmo.Gizmo.PostDragBegin += OnGizmoPostDragBegin;
        _scaleGizmo.Gizmo.PreDragUpdate += OnGizmoPreDragUpdate;
        _scaleGizmo.Gizmo.PostDragUpdate += OnGizmoPostDragUpdate;
        _scaleGizmo.Gizmo.PreDragBegin += OnGizmoPreDragBegin;
        _scaleGizmo.SetTransformPivot(GizmoObjectTransformPivot.ObjectMeshPivot);
        _gizmosInitialized = true;
    }

    private void OnGizmoPreDragBegin(Gizmo gizmo, int handleId)
    {
        _localScaleBeforeStartDrag = transform.localScale;
        _positionBeforeStartDrag = transform.position;
        CurrentScaleDrag = transform.localScale;
        //_lastCircleIntersectPoint = default;
        IsBeingUsed = true;
    }

    private void OnGizmoPreDragUpdate(Gizmo gizmo, int handleId)
    {
        //_positionBeforeStartDrag = transform.localPosition;
    }

    private async void OnGizmoPostDragEnd(Gizmo gizmo, int handleId)
    {
        if(TryGetComponent(out KeepRelativePosition k))
        {
            k.SelectablePositionChanged();
        }
        GizmoBeingUsed = false;
        IsBeingUsed = false;
        GizmoDragEnded?.Invoke();
        await Task.Yield();
        GizmoUsedLastFrame = false;
    }

    private void OnGizmoPostDragBegin(Gizmo gizmo, int handleId)
    {
        GizmoBeingUsed = true;
    }

    // Find the points where the two circles intersect.
    private int FindCircleCircleIntersections(
        float cx0, float cy0, float radius0,
        float cx1, float cy1, float radius1,
        out PointF intersection1, out PointF intersection2)
    {
        // Find the distance between the centers.
        float dx = cx0 - cx1;
        float dy = cy0 - cy1;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // See how many solutions there are.
        if (dist > radius0 + radius1)
        {
            // No solutions, the circles are too far apart.
            intersection1 = new PointF(float.NaN, float.NaN);
            intersection2 = new PointF(float.NaN, float.NaN);
            return 0;
        }
        else if (dist < Math.Abs(radius0 - radius1))
        {
            // No solutions, one circle contains the other.
            intersection1 = new PointF(float.NaN, float.NaN);
            intersection2 = new PointF(float.NaN, float.NaN);
            return 0;
        }
        else if ((dist == 0) && (radius0 == radius1))
        {
            // No solutions, the circles coincide.
            intersection1 = new PointF(float.NaN, float.NaN);
            intersection2 = new PointF(float.NaN, float.NaN);
            return 0;
        }
        else
        {
            // Find a and h.
            double a = (radius0 * radius0 -
                radius1 * radius1 + dist * dist) / (2 * dist);
            double h = Math.Sqrt(radius0 * radius0 - a * a);

            // Find P2.
            double cx2 = cx0 + a * (cx1 - cx0) / dist;
            double cy2 = cy0 + a * (cy1 - cy0) / dist;

            // Get the points P3.
            intersection1 = new PointF(
                (float)(cx2 + h * (cy1 - cy0) / dist),
                (float)(cy2 - h * (cx1 - cx0) / dist));
            intersection2 = new PointF(
                (float)(cx2 - h * (cy1 - cy0) / dist),
                (float)(cy2 + h * (cx1 - cx0) / dist));

            // See if we have 1 or 2 solutions.
            if (dist == radius0 + radius1) return 1;
            return 2;
        }
    }

    private void OnGizmoPostDragUpdate(Gizmo gizmo, int handleId)
    {
        GizmoUsedLastFrame = true;

        if (gizmo.ObjectTransformGizmo == _translateGizmo && _selectable.ExeedsMaxTranslation(out Vector3 totalExcess))
        {
            Debug.Log("Entered DragUpdate IF");
            transform.localPosition -= totalExcess;
            Transform parent = transform.parent;

            //do vertical component first
            if (gizmo.RelativeDragOffset.y != 0)
            {
                Selectable verticalComponent = null;

                while (parent != null && verticalComponent == null && _selectable.AllowInverseControl)
                {
                    var parentSelectable = parent.GetComponent<Selectable>();
                    if (parentSelectable != null && parentSelectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Y))
                    {
                        verticalComponent = parentSelectable;
                        break;
                    }
                    parent = parent.parent;
                }

                if (verticalComponent != null)
                {
                    Vector3 desiredPosition = new Vector3(_positionBeforeStartDrag.x, _positionBeforeStartDrag.y + gizmo.TotalDragOffset.y, _positionBeforeStartDrag.z);
                    Vector3 currentPos = new Vector3(_positionBeforeStartDrag.x, transform.position.y, _positionBeforeStartDrag.z);
                    float angle = Vector3.Angle(desiredPosition, currentPos);
                    float distance = Vector3.Distance(desiredPosition, currentPos);

                    verticalComponent.transform.Rotate(0, angle, 0);
                    Debug.Log($"Found Vertical Component {angle}");
                    currentPos = new Vector3(_positionBeforeStartDrag.x, transform.position.y, _positionBeforeStartDrag.z);
                    float distance2 = Vector3.Distance(desiredPosition, currentPos);
                    if (distance2 > distance)
                    { // we went the wrong way, flip the angle
                        verticalComponent.transform.Rotate(0, -angle * 2f, 0);
                    }

                    if (verticalComponent.ExceedsMaxRotation(out Vector3 totalExcess1))
                    {
                        // Debug.Log("Doin stuffs");
                        verticalComponent.transform.localRotation *= Quaternion.Euler(-totalExcess1.x, -totalExcess1.y, -totalExcess1.z);
                    }
                }
            }

            //try a two-bone triangle movement
            Transform closestBone = null;
            Transform farthestBone = null;
            parent = transform.parent;
            while (parent != null && (closestBone == null || farthestBone == null))
            {
                var parentSelectable = parent.GetComponent<Selectable>();
                if (parentSelectable != null && parentSelectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z))
                {
                    if (closestBone == null)
                    {
                        closestBone = parent;
                    }
                    else
                    {
                        farthestBone = parent;
                        break;
                    }
                }
                parent = parent.parent;
            }

            if (closestBone != null && farthestBone != null && _selectable.AllowInverseControl)
            {
                Vector3 farthestBoneXZ = new Vector3(farthestBone.transform.position.x, 0, farthestBone.transform.position.z);
                Vector3 closestBoneXZ = new Vector3(closestBone.transform.position.x, 0, closestBone.transform.position.z);
                Vector3 thisTransformXZ = new Vector3(transform.position.x + gizmo.RelativeDragOffset.x, 0, transform.position.z + gizmo.RelativeDragOffset.z);
                //Debug.Log(thisTransformXZ);
                float circle1Radius = Vector3.Distance(farthestBoneXZ, closestBoneXZ);
                float circle2Radius = Vector3.Distance(closestBoneXZ, new Vector3(transform.position.x, 0, transform.position.z));
                int intersects = FindCircleCircleIntersections(farthestBoneXZ.x, farthestBoneXZ.z, circle1Radius, thisTransformXZ.x, thisTransformXZ.z, circle2Radius, out PointF intersection1, out PointF intersection2);

                if (intersects > 0)
                {

                    Vector3 intersect1 = new Vector3(intersection1.X, 0, intersection1.Y);
                    Vector3 intersect = intersect1;
                    if (intersects > 1)
                    {
                        Vector3 intersect2 = new Vector3(intersection2.X, 0, intersection2.Y);
                        float distance1, distance2;
                        if (_lastCircleIntersectPoint == default)
                        {
                            // use intersect closest to destination
                            distance1 = Vector3.Distance(intersect1, thisTransformXZ);
                            distance2 = Vector3.Distance(intersect2, thisTransformXZ);
                        }
                        else
                        {
                            // get point closest to last intersect
                            distance1 = Vector3.Distance(intersect1, _lastCircleIntersectPoint);
                            distance2 = Vector3.Distance(intersect2, _lastCircleIntersectPoint);
                        }

                        intersect = distance1 < distance2 ? intersect1 : intersect2;
                        _lastCircleIntersectPoint = intersect;
                    }

                    //Quaternion originalFacing = _translateGizmo.Gizmo.Transform.Rotation3D;
                    float angleBetween = -Vector3.SignedAngle(farthestBone.right, intersect - farthestBoneXZ, Vector3.up);
                    farthestBone.transform.Rotate(new Vector3(0, 0, angleBetween));
                    closestBoneXZ = new Vector3(closestBone.transform.position.x, 0, closestBone.transform.position.z);
                    angleBetween = -Vector3.SignedAngle(closestBone.right, thisTransformXZ - closestBoneXZ, Vector3.up);
                    closestBone.transform.Rotate(new Vector3(0, 0, angleBetween));
                    //transform.rotation = originalFacing;
                    //_translateGizmo.Gizmo.Transform.Rotation3D = originalFacing;
                }
            }

            _translateGizmo.Gizmo.Transform.Position3D = transform.position;
            //Debug.Log(gizmo.TotalDragOffset);
        }

        if (gizmo.ObjectTransformGizmo == _rotateGizmo && _selectable.ExceedsMaxRotation(out totalExcess))
        {
            transform.localRotation *= Quaternion.Euler(-totalExcess.x, -totalExcess.y, -totalExcess.z);
            _rotateGizmo.Gizmo.Transform.Rotation3D = transform.rotation;
        }

        if (gizmo.ObjectTransformGizmo == _scaleGizmo)
        {
            CurrentScaleDrag = new Vector3(_localScaleBeforeStartDrag.x * gizmo.TotalDragScale.x, _localScaleBeforeStartDrag.y * gizmo.TotalDragScale.y, _localScaleBeforeStartDrag.z * gizmo.TotalDragScale.z);

            float xScale = _localScaleBeforeStartDrag.x * (_selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.X) ? gizmo.TotalDragScale.x : 1);
            float yScale = _localScaleBeforeStartDrag.y * (_selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Y) ? gizmo.TotalDragScale.y : 1);
            float zScale = _selectable.ScaleLevels.Count == 0 ? _localScaleBeforeStartDrag.z * (_selectable.IsGizmoSettingAllowed(GizmoType.Scale, Axis.Z) ? gizmo.TotalDragScale.z : 1) : _selectable.transform.localScale.z;

            if (UI_ToggleSnapping.SnappingEnabled)
            {
                if (xScale != _localScaleBeforeStartDrag.x)
                {
                    xScale = Selectable.RoundToNearestHalfInch(xScale);
                }

                if (yScale != _localScaleBeforeStartDrag.y)
                {
                    yScale = Selectable.RoundToNearestHalfInch(yScale);
                }

                if (_selectable.ScaleLevels.Count == 0 && zScale != _localScaleBeforeStartDrag.z)
                {
                    zScale = Selectable.RoundToNearestHalfInch(zScale);
                }
            }

            _selectable.transform.localScale = new Vector3(xScale, yScale, zScale);
        }

        GizmoDragPostUpdate?.Invoke();
    }
}

public enum GizmoMode
{
    Translate,
    Rotate,
    Scale,
    Universal
}
