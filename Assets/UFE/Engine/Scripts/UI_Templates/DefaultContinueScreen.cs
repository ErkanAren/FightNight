using UFE3D;
using UnityEngine;
using UnityEngine.UI;

public class DefaultContinueScreen : StoryModeContinueScreen
{
    public float countDownTime = 10f;
    public Text countDownDisplay;

    private bool _isGameOver = false;

    public virtual void Update()
    {
        if (countDownDisplay == null) return;

        countDownTime -= Time.deltaTime;
        if (countDownTime < 0) countDownTime = 0;
        countDownDisplay.text = Mathf.Round(countDownTime).ToString();

        if (countDownTime == 0 && !_isGameOver)
        {
            _isGameOver = true;
            UFE.StartStoryModeGameOverScreen();
        }
    }

    public void GoToMainMenu()
    {
        UFE.StartMainMenuScreen();
    }

    public void GoToGameOverScreen()
    {
        UFE.StartStoryModeGameOverScreen();
    }

    public void ContinueStoryMode()
    {
        UFE.StartStoryModeBattle();
    }
}
