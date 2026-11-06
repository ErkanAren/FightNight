using UFE3D;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadVersusMode : MonoBehaviour
{
    public GlobalInfo globalConfigFile;
    public UFE3D.CharacterInfo P1SelectedChar;
    public CharacterInputSource player1Inputs;
    public UFE3D.CharacterInfo P2SelectedChar;
    public CharacterInputSource player2Inputs;
    public int selectedStage;
    public string UFESceneName;

    public void LoadUFEScene()
    {
        globalConfigFile.deploymentOptions.deploymentType = DeploymentType.VersusMode;

        globalConfigFile.deploymentOptions.activeCharacters[0] = P1SelectedChar;
        globalConfigFile.deploymentOptions.activeCharacters[1] = P2SelectedChar;
        globalConfigFile.deploymentOptions.controllers[0] = player1Inputs;
        globalConfigFile.deploymentOptions.controllers[1] = player2Inputs;

        globalConfigFile.selectedStage = globalConfigFile.stages[selectedStage];

        SceneManager.LoadScene(UFESceneName);
    }
}