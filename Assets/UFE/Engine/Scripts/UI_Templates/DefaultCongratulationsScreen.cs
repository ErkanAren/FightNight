using UFE3D;
using UnityEngine;

public class DefaultCongratulationsScreen : StoryModeScreen
{
    public AudioClip congratulationsSound;
    public float delayBeforeLoadingNextScreen = 6f;

    #region public override methods
    public override void OnShow()
    {
        base.OnShow();

        UFE.DelayLocalAction(this.GoToNextScreen, this.delayBeforeLoadingNextScreen);
    }
    #endregion
}
