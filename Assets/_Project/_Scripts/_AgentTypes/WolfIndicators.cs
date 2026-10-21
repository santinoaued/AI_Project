using System.Collections.Generic;
using UnityEngine;

public class WolfIndicators : MonoBehaviour
{
    private const int RingSegments = 32;
    private const int ConeSegments = 16;
    private const float IndicatorHeight = 0.4f;

    [SerializeField] private float _ringDuration = 1f;
    [SerializeField] private float _ringMaxRadius = 6f;
    [SerializeField] private float _alertLineDuration = 1.5f;

    private static Material _lineMaterial;

    private Wolf _wolf;
    private LineRenderer _pathLine;
    private LineRenderer _sightLine;
    private LineRenderer _alertLine;
    private LineRenderer _ringLine;
    private LineRenderer _coneLine;
    private Transform _marker;

    private float _ringStartTime = float.NegativeInfinity;
    private Color _ringColor;
    private float _alertStartTime = float.NegativeInfinity;
    private Vector3 _alertSource;

    private void Awake()
    {
        _wolf = GetComponent<Wolf>();

        if (_lineMaterial == null)
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

        _pathLine = CreateLine("PathLine", new Color(1f, 0.9f, 0.1f), 0.25f, false);
        _sightLine = CreateLine("SightLine", Color.red, 0.1f, false);
        _alertLine = CreateLine("AlertLine", new Color(1f, 0.55f, 0.1f), 0.15f, false);
        _ringLine = CreateLine("HowlRing", Color.white, 0.15f, true);

        _coneLine = CreateLine("ViewCone", Color.white, 0.06f, true);
        _coneLine.positionCount = ConeSegments + 2;

        _sightLine.positionCount = 2;
        _alertLine.positionCount = 2;
        _ringLine.positionCount = RingSegments;

        _marker = CreateMarker();
    }

    private void LateUpdate()
    {
        UpdateCone();
        UpdatePath();
        UpdateSight();
        UpdateMarker();
        UpdateRing();
        UpdateAlertLine();
    }

    private void OnDestroy()
    {
        if (_marker != null)
            Destroy(_marker.gameObject);
    }

    public void PlayHowl()
    {
        _ringStartTime = Time.time;
        _ringColor = Color.white;
    }

    public void PlayAlertReceived(Vector3 sourcePosition)
    {
        _ringStartTime = Time.time;
        _ringColor = new Color(1f, 0.55f, 0.1f);
        _alertStartTime = Time.time;
        _alertSource = sourcePosition;
    }

    private void UpdateCone()
    {
        Vector3 origin = transform.position;
        Color color = _wolf.CanSeeHunter ? new Color(1f, 0.2f, 0.2f, 0.6f) : new Color(1f, 1f, 1f, 0.35f);

        _coneLine.enabled = true;
        _coneLine.startColor = color;
        _coneLine.endColor = color;
        _coneLine.SetPosition(0, Raised(origin));

        float halfAngle = _wolf.ViewAngle * 0.5f;

        for (int i = 0; i <= ConeSegments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)ConeSegments);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, _wolf.ViewDistance, _wolf.ObstacleMask);
            float length = hit ? hitInfo.distance : _wolf.ViewDistance;

            _coneLine.SetPosition(i + 1, Raised(origin + direction * length));
        }
    }

    private void UpdatePath()
    {
        Navigator navigator = _wolf.Navigator;
        bool visible = navigator != null && navigator.IsUsingPathfinding;
        _pathLine.enabled = visible;

        if (!visible)
            return;

        IReadOnlyList<Vector3> path = navigator.Path;
        int first = navigator.Index;

        _pathLine.positionCount = path.Count - first + 1;
        _pathLine.SetPosition(0, Raised(transform.position));

        for (int i = first; i < path.Count; i++)
            _pathLine.SetPosition(i - first + 1, Raised(path[i]));
    }

    private void UpdateSight()
    {
        _sightLine.enabled = _wolf.CanSeeHunter;

        if (!_sightLine.enabled)
            return;

        _sightLine.SetPosition(0, Raised(transform.position));
        _sightLine.SetPosition(1, Raised(_wolf.HunterPosition));
    }

    private void UpdateMarker()
    {
        bool visible = _wolf.HasKnownPosition;
        _marker.gameObject.SetActive(visible);

        if (!visible)
            return;

        _marker.position = Raised(_wolf.LastKnownPosition);
        _marker.localScale = Vector3.one * (0.6f + 0.2f * Mathf.PingPong(Time.time * 2f, 1f));
    }

    private void UpdateRing()
    {
        float t = (Time.time - _ringStartTime) / _ringDuration;
        _ringLine.enabled = t >= 0f && t <= 1f;

        if (!_ringLine.enabled)
            return;

        Color color = _ringColor;
        color.a = 1f - t;
        _ringLine.startColor = color;
        _ringLine.endColor = color;

        float radius = Mathf.Lerp(0.5f, _ringMaxRadius, t);
        Vector3 center = Raised(transform.position);

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            _ringLine.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
    }

    private void UpdateAlertLine()
    {
        float t = (Time.time - _alertStartTime) / _alertLineDuration;
        _alertLine.enabled = t >= 0f && t <= 1f;

        if (!_alertLine.enabled)
            return;

        Color color = new Color(1f, 0.55f, 0.1f, 1f - t);
        _alertLine.startColor = color;
        _alertLine.endColor = color;
        _alertLine.SetPosition(0, Raised(_alertSource));
        _alertLine.SetPosition(1, Raised(transform.position));
    }

    private LineRenderer CreateLine(string lineName, Color color, float width, bool loop)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = _lineMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.enabled = false;
        return line;
    }

    private Transform CreateMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"{name} KnownPosition";
        Destroy(marker.GetComponent<Collider>());

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.material = new Material(_lineMaterial) { color = new Color(1f, 0.2f, 0.2f, 0.85f) };
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        marker.SetActive(false);
        return marker.transform;
    }

    private static Vector3 Raised(Vector3 position)
    {
        position.y = IndicatorHeight;
        return position;
    }
}
