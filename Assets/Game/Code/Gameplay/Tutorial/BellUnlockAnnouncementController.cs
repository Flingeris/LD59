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

        IsRunning = true;
        var currentAnnouncement = data;

        while (currentAnnouncement != null)
        {
            if (!main.TryGetBellWorldObject(currentAnnouncement.BellId, out var bellWorldObject) || bellWorldObject == null)
            {
                break;
            }

            overlay.ShowWorldMarker(
                bellWorldObject.GetTutorialMarkerTarget(),
                currentAnnouncement.Title,
                currentAnnouncement.MarkerColor,
                TutorialWorldMarkerAnchor.BellPosition);

            yield return overlay.PlayTypedMessage(currentAnnouncement.Message, false);
            currentAnnouncement = currentAnnouncement.NextAnnouncement;
        }

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
