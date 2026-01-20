using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 _originalScale;
    public float scaleFactor = 1.1f;
    public float speed = 10f;

    private Vector3 _targetScale;

    void Start()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = _originalScale * scaleFactor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }
}