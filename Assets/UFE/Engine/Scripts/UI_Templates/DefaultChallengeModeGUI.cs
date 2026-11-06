using UFE3D;
using UnityEngine;

public class DefaultChallengeModeGUI : ChallengeMode
{
    public void OnGUI()
    {

        if (!complete && !UFE.MatchManager.lockInputs && !UFE.MatchManager.lockMovements)
        {
            GUI.Box(new Rect(20, 100, 250, 400), UFE.GetChallenge(currentChallenge).challengeName);
            GUI.BeginGroup(new Rect(30, 130, 300, 400));
            {
                if (UFE.GetChallenge(currentChallenge).description == "%list%")
                {
                    string newDesc = "";
                    int currAction = 0;
                    foreach (ActionSequence actionSeq in sequentialActions)
                    {
                        string moveName = actionSeq.specialMove.moveName;
                        if (currentAction > currAction) moveName += " (DONE)";

                        newDesc += moveName + "\n";
                        currAction++;
                    }
                    GUILayout.Label(newDesc);
                }
                else
                {
                    GUILayout.Label(UFE.GetChallenge(currentChallenge).description);
                }
            }
            GUI.EndGroup();

            if (GUI.Button(new Rect(Screen.width - 120, 50, 70, 30), "Skip"))
            {
                currentAction = sequentialActions.Count;
                TestChallenge();
            }
        }
    }

    /*
    protected override void startNextChallenge()
    {
        int selectedStage = 0;
        if (currentChallenge == 0) selectedStage = 1; // First challenge = 0
        UFE.SetStage(UFE.config.stages[selectedStage]);
        base.startNextChallenge();
    }*/
}