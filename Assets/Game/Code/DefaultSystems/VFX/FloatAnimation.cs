using DG.Tweening;
using UnityEngine;

public class FloatUpDownAnimation : MonoBehaviour
{
    [Header("Move Y")]
    [SerializeField] private float moveYAmplitude = 0.15f;

    [SerializeField] private float moveYDuration = 0.8f;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;

    [SerializeField] private float maxRandomStartDelay = 0.5f;
    [SerializeField] private bool ignoreTimeScale = true;

    private Sequence _sequence;
    private Vector3 _startLocalPosition;

    private void Awake()
    {
        _startLocalPosition = transform.localPosition;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        Stop();

        transform.localPosition = _startLocalPosition;

        _sequence = DOTween.Sequence();

        _sequence.Append(
            transform
                .DOLocalMoveY(_startLocalPosition.y + moveYAmplitude, moveYDuration)
                .SetEase(Ease.InOutSine)
        );

        _sequence
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);

        float delay = Random.Range(0f, maxRandomStartDelay);
        _sequence.SetDelay(delay);
    }

    public void Stop()
    {
        if (_sequence != null && _sequence.IsActive())
            _sequence.Kill();

        transform.localPosition = _startLocalPosition;
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }
}