using System.Collections;

public sealed class BellUnlockAnnouncementController
{
    private readonly Main main;
    private TutorialOverlayView overlayView;

    public BellUnlockAnnouncementController(Main main)
    {
        this.main = main;
    }

    public bool IsRunning { get; private set; }

    public void TryPlay(BellUnlockAnnouncementData data)
    {
        if (IsRunning || data == null || string.IsNullOrWhiteSpace(data.BellId))
        {
            return;
        }

        if (main == null || main.RunState == null || main.RunState.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        main.StartCoroutine(PlayRoutine(data));
    }

    private IEnumerator PlayRoutine(BellUnlockAnnouncementData data)
    {
        var overlay = ResolveOverlay();
        if (overlay == null)
        {
            yield break;
        }

        if (!main.TryGetBellWorldObject(data.BellId, out var bellWorldObject) || bellWorldObject == null)
        {
            yield break;
        }

        IsRunning = true;

        overlay.ShowWorldMarker(
            bellWorldObject.GetTutorialMarkerTarget(),
            data.Title,
            data.MarkerColor,
            TutorialWorldMarkerAnchor.BellPosition);

        yield return overlay.PlayTypedMessage(data.Message, false);

        overlay.HideWorldMarker();
        IsRunning = false;
    }

    private TutorialOverlayView ResolveOverlay()
    {
        if (overlayView != null)
        {
            return overlayView;
        }

        overlayView = G.ui != null ? G.ui.EnsureTutorialOverlay() : null;
        return overlayView;
    }
}