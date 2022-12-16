using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum DragPointLocation
{
    UpperLeft,
    UpperRight,
    BottomLeft,
    BottomRight,
}

public class Sticker : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] Canvas _canvas;
    [SerializeField] float _hightlightOffset = 4f;
    RectTransform _canvasTransform;
    Vector2 _pressMousePosition;
    Material _material;
    RectTransform _rectTransform;
    Image _foldImage;
    RectTransform _hightlightTransform;
    Vector2 _localDragPoint;
    Vector2 _mouseQuadrant;

    private void Awake()
    {
        _canvasTransform = _canvas.transform as RectTransform;
        _material = GetComponent<Image>().material;
        _rectTransform = transform as RectTransform;
        _foldImage = transform.Find("Fold").GetComponent<Image>();
        _hightlightTransform = _foldImage.transform.Find("Mask/Hightlight") as RectTransform;
    }

    private void OnEnable()
    {
        ResetClip();
    }

    private void OnDisable()
    {
        ResetClip();
    }

    void ResetClip()
    {
        transform.localScale = new Vector3(1f, 1f, 1f);
        _foldImage.gameObject.SetActive(false);
        _foldImage.transform.localScale = new Vector3(-1f, 1f, 1f);
        _material.SetVector("_ClipNormal", Vector4.zero);
        _material.SetFloat("_ClipValue", 0f);
        _foldImage.material.SetVector("_ClipNormal", Vector4.zero);
        _foldImage.material.SetFloat("_ClipValue", 0f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressMousePosition = GetCanvasMousePosition(eventData.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, _canvas.worldCamera, out Vector2 localMousePosition);
        if (localMousePosition.y < 0f)
        {
            if (localMousePosition.x < 0f)
            {
                UpdateDragPoint(DragPointLocation.BottomLeft);
            }
            else
            {
                UpdateDragPoint(DragPointLocation.BottomRight);
            }
        }
        else
        {
            if (localMousePosition.x < 0f)
            {
                UpdateDragPoint(DragPointLocation.UpperLeft);
            }
            else
            {
                UpdateDragPoint(DragPointLocation.UpperRight);
            }
        }
    }

    void UpdateDragPoint(DragPointLocation dragPointLocation)
    {
        var halfExtents = _rectTransform.sizeDelta / 2f;
        switch (dragPointLocation)
        {
            case DragPointLocation.UpperLeft:
                _localDragPoint = new Vector2(-halfExtents.x, halfExtents.y);
                _mouseQuadrant = new Vector2(1, -1);
                break;
            case DragPointLocation.UpperRight:
                _localDragPoint = new Vector2(halfExtents.x, halfExtents.y);
                _mouseQuadrant = new Vector2(-1, -1);
                break;
            case DragPointLocation.BottomLeft:
                _localDragPoint = new Vector2(-halfExtents.x, -halfExtents.y);
                _mouseQuadrant = new Vector2(1, 1);
                break;
            case DragPointLocation.BottomRight:
                _localDragPoint = new Vector2(halfExtents.x, -halfExtents.y);
                _mouseQuadrant = new Vector2(-1, 1);
                break;
            default:
                break;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_foldImage.gameObject.activeSelf)
        {
            _foldImage.gameObject.SetActive(true);
        }

        // Clamp mouse quadrant
        var currentMousePosition = GetCanvasMousePosition(eventData.position);
        var diff = currentMousePosition - _pressMousePosition;
        diff.x = _mouseQuadrant.x < 0 ? Mathf.Min(0, diff.x) : Mathf.Max(0, diff.x);
        diff.y = _mouseQuadrant.y < 0 ? Mathf.Min(0, diff.y) : Mathf.Max(0, diff.y);

        // Clip
        var clipNormal = diff.normalized;
        Vector2 dragPoint = _canvasTransform.InverseTransformPoint(transform.TransformPoint(_localDragPoint));
        var clipPoint = dragPoint + diff / 2f;
        SetClip(_material, clipNormal, clipPoint);

        // Mirror position
        var position = (Vector2)_canvasTransform.InverseTransformPoint(transform.position);
        var unitX = clipNormal;
        var unitY = Vector2.Perpendicular(clipNormal);
        var positionCenter = unitX * Vector2.Dot(clipPoint, unitX) + unitY * Vector2.Dot(position, unitY);
        var mirrorPosition = positionCenter + (positionCenter - position);
        _foldImage.transform.position = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _canvasTransform.TransformPoint(mirrorPosition));

        // Mirror direction
        var direction = _rectTransform.rotation * Vector3.right;
        var mirrorDirection = unitX * -Vector2.Dot(direction, unitX) + unitY * Vector2.Dot(direction, unitY);
        _foldImage.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(mirrorDirection.y, -mirrorDirection.x, 0f));

        // Clip fold
        if (clipNormal != Vector2.zero)
        {
            Vector2 foldDragPoint = _canvasTransform.InverseTransformPoint(_foldImage.transform.TransformPoint(_localDragPoint));
            SetClip(_foldImage.material, clipNormal, foldDragPoint - diff / 2f);
        }
        else
        {
            _foldImage.gameObject.SetActive(false);
        }

        // Hightlight
        _hightlightTransform.position = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _canvasTransform.TransformPoint(clipPoint + clipNormal * (_hightlightOffset + _hightlightTransform.sizeDelta.x / 2f)));
        _hightlightTransform.rotation = Quaternion.LookRotation(Vector3.forward, unitY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetClip();
    }

    void SetClip(Material material, Vector2 clipNormal, Vector2 clipPoint)
    {
        float clipValue = Vector2.Dot(clipPoint, clipNormal);
        material.SetVector("_ClipNormal", clipNormal);
        material.SetFloat("_ClipValue", clipValue);
    }

    Vector2 GetCanvasMousePosition(Vector2 mousePosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasTransform, mousePosition, _canvas.worldCamera, out Vector2 canvasMousePosition);
        return canvasMousePosition;
    }
}
