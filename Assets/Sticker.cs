using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Sticker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] Canvas _canvas;
    RectTransform _canvasTransform;
    Vector2 _pressMousePosition;
    Material _material;
    RectTransform _rectTransform;
    Image _fold;

    private void Awake()
    {
        _canvasTransform = _canvas.transform as RectTransform;
        _material = GetComponent<Image>().material;
        _rectTransform = transform as RectTransform;
        _fold = transform.Find("Fold").GetComponent<Image>();
    }

    private void OnDisable()
    {
        _material.SetVector("_ClipNormal", Vector4.zero);
        _material.SetFloat("_ClipValue", 0f);
    }

    void Start()
    {

        Debug.Log(new Vector2(1, 1).normalized);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressMousePosition = GetCanvasMousePosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var halfExtents = _rectTransform.sizeDelta / 2f;
        var localDragPoint = new Vector3(-halfExtents.x, -halfExtents.y);

        var currentMousePosition = GetCanvasMousePosition(eventData.position);
        var diff = currentMousePosition - _pressMousePosition;
        var clipNormal = diff.normalized;
        Vector2 dragPoint = _canvasTransform.InverseTransformPoint(_rectTransform.TransformPoint(localDragPoint));
        var clipCenter = dragPoint + diff / 2f;
        float clipValue = Vector2.Dot(clipCenter, clipNormal);
        _material.SetVector("_ClipNormal", clipNormal);
        _material.SetFloat("_ClipValue", clipValue);

        var position = (Vector2)_canvasTransform.InverseTransformPoint(_rectTransform.position);
        var unitX = clipNormal;
        var unitY = Vector2.Perpendicular(clipNormal);
        var center = unitX * Vector2.Dot(clipCenter, unitX) + unitY * Vector2.Dot(position, unitY);
        var reflectedPosition = center + (center - position);
        _fold.transform.position = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _canvasTransform.TransformPoint(reflectedPosition));

        var direction = _rectTransform.rotation * Vector3.right;
        var reflectedDirection = unitX * -Vector2.Dot(direction, unitX) + unitY * Vector2.Dot(direction, unitY);
        _fold.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(reflectedDirection.y, -reflectedDirection.x, 0f));

        Vector2 foldDragPoint = _canvasTransform.InverseTransformPoint(_fold.rectTransform.TransformPoint(new Vector3(-localDragPoint.x, localDragPoint.y)));
        SetClip(_fold.material, clipNormal, foldDragPoint - diff / 2f);
    }

    void SetClip(Material material, Vector2 clipNormal, Vector2 clipCenter)
    {
        float clipValue = Vector2.Dot(clipCenter, clipNormal);
        material.SetVector("_ClipNormal", clipNormal);
        material.SetFloat("_ClipValue", clipValue);
    }

    Vector2 GetCanvasMousePosition(Vector2 mousePosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasTransform, mousePosition, _canvas.worldCamera, out Vector2 canvasMousePosition);
        return canvasMousePosition;
    }
}
