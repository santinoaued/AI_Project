using UnityEngine;

public class StateFeedback : MonoBehaviour
{
    [SerializeField] private float _labelHeight = 2f;

    private Material _bodyMaterial;
    private TextMesh _label;
    private Transform _cameraTransform;
    private string _stateName = string.Empty;
    private string _eventText = string.Empty;
    private float _eventEndTime;

    private void Awake()
    {
        Renderer bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null)
            _bodyMaterial = bodyRenderer.material;

        CreateLabel();
    }

    private void Start()
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        _label.transform.position = transform.position + Vector3.up * _labelHeight;

        if (_cameraTransform != null)
            _label.transform.rotation = _cameraTransform.rotation;

        if (_eventText.Length > 0 && Time.time >= _eventEndTime)
        {
            _eventText = string.Empty;
            RefreshText();
        }
    }

    private void OnDestroy()
    {
        if (_label != null)
            Destroy(_label.gameObject);
    }

    public void SetState(string stateName, Color color)
    {
        Debug.Log($"{name} -> {stateName}");

        _stateName = stateName;

        if (_bodyMaterial != null)
            _bodyMaterial.color = color;

        _label.color = color;
        RefreshText();
    }

    public void SetEvent(string eventText, float duration)
    {
        _eventText = eventText;
        _eventEndTime = Time.time + duration;
        RefreshText();
    }

    private void RefreshText()
    {
        _label.text = _eventText.Length > 0 ? $"{_stateName}\n{_eventText}" : _stateName;
    }

    private void CreateLabel()
    {
        GameObject labelObj = new GameObject("StateLabel");
        _label = labelObj.AddComponent<TextMesh>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelObj.GetComponent<MeshRenderer>().sharedMaterial = _label.font.material;
        _label.fontSize = 48;
        _label.characterSize = 0.1f;
        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
    }
}
