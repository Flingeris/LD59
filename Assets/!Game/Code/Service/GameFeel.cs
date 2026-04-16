using DG.Tweening;
using UnityEngine;

public class GameFeel
{
    public void PunchScale(Transform target, float strength = 0.25f, float duration = 0.25f, int vibrato = 8, float elasticity = 0.6f)
    {
        if (target == null) return;
        target.DOKill();

        target.DOPunchScale(
            punch: Vector3.one * strength,
            duration: duration,
            vibrato: vibrato,
            elasticity: elasticity
        ).SetEase(Ease.OutQuad);
    }

    public void Shake(Transform target, float duration = 0.3f, float strength = 0.5f, int vibrato = 10)
    {
        if (target == null) return;
        target.DOShakePosition(duration, strength, vibrato, randomness: 90f);
    }

    public void EnemyHitPunchScale(Transform t)
    {
        t.DOPunchScale(Vector3.one * 0.15f, 0.15f, 5, 0.8f);
    }

    public void PlayerHitPunch(Transform t)
    {
        t.DOPunchScale(Vector3.one * 0.1f, 0.12f, 4, 0.9f);
        Camera.main.transform.DOShakePosition(0.12f, 0.5f, 40, 90, false, true);
    }
}