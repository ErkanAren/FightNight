using System.Collections;
using UFE3D;
using UnityEngine;

public class StoryModeTextureScreen : StoryModeScreen
{
    public bool skippable = true;
    public float delayBeforeGoingToNextScreen = 3f;
    public float minDelayBeforeSkipping = 0.1f;

    #region public override methods
    public override void OnShow()
    {
        base.OnShow();

        this.StartCoroutine(this.ShowScreenDelayed());
    }
    #endregion

    public IEnumerator ShowScreenDelayed()
    {
        float startTime = Time.realtimeSinceStartup;
        float time = 0f;

        while (
            time < this.delayBeforeGoingToNextScreen &&
            !(skippable && Input.anyKeyDown && time > this.minDelayBeforeSkipping)
        )
        {
            yield return null;
            time = Time.realtimeSinceStartup - startTime;
        }

        this.GoToNextScreen();
    }
}
