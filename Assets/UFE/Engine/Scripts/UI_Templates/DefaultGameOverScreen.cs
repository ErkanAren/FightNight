using UFE3D;

public class DefaultGameOverScreen : StoryModeScreen
{
    public float delayBeforeLoadingNextScreen = 3f;

    #region public override methods
    public override void OnShow()
    {
        base.OnShow();

        UFE.DelayLocalAction(this.GoToNextScreen, delayBeforeLoadingNextScreen);
    }
    #endregion
}
