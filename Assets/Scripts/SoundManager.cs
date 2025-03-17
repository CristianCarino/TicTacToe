using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Transform placeSfxPrefab;
    [SerializeField] private Transform winSfxPrefab;
    [SerializeField] private Transform loseSfxPrefab;
    [SerializeField] private Transform tieSfxPrefab;

    private void Start()
    {
        GameManager.Instance.OnPlacedObject += GameManager_OnPlacedObject;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;
    } 


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e) {
        if(GameManager.Instance.GetLocalPlayerType() == e.winPlayerType) {
            Transform sfxTransform = Instantiate(winSfxPrefab);
            Destroy(sfxTransform.gameObject, 3f);
        } else {
            Transform sfxTransform = Instantiate(loseSfxPrefab);
            Destroy(sfxTransform.gameObject, 3f);
        }
    }

    private void GameManager_OnGameTied(object sender, System.EventArgs e){
        Transform sfxTransform = Instantiate(tieSfxPrefab);
        Destroy(sfxTransform.gameObject, 3f);
    }

    //Destroying locally
    private void GameManager_OnPlacedObject(object sender, System.EventArgs e) {
        Transform sfxTransform = Instantiate(placeSfxPrefab);
        Destroy(sfxTransform.gameObject, 3f);
    }
}
