using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrowGameObject;
    [SerializeField] private GameObject circleArrowGameObject;
    [SerializeField] private GameObject crossYouTextGameObject;
    [SerializeField] private GameObject circleYouTextGameObject;

    private void Awake(){
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        crossYouTextGameObject.SetActive(false);
        circleYouTextGameObject.SetActive(false);
    }

    private void Start(){
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
    }

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, System.EventArgs e){
        UpdateCurrentArrow();
    }

    //When the game starts we can check which is the local player
    private void GameManager_OnGameStarted(object sender, System.EventArgs e){
        if(GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross) {
            crossYouTextGameObject.SetActive(true);
        } else {
            circleYouTextGameObject.SetActive(true);
        }
        UpdateCurrentArrow();
    }

        private void UpdateCurrentArrow() {
            if(GameManager.Instance.GetcurrentPlayablePlayerType() == GameManager.PlayerType.Cross) {
                crossArrowGameObject.SetActive(true);
                circleArrowGameObject.SetActive(false);
            } else {
                crossArrowGameObject.SetActive(false);
                circleArrowGameObject.SetActive(true);
            }

        }
}

