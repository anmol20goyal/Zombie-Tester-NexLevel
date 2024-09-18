using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject _canvasGO;
    [SerializeField] private GameObject _playerDeadGO;
    [SerializeField] private GameObject _enemyDeadGO;
    [SerializeField] private GameObject _enemyCrawlGO;
    [SerializeField] private Image _fadeScreen;

    [SerializeField] private float _fadeSpeed;
    private bool _canFade;

    public static UIController instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        _canFade = false;
    }

    public void OnClickNewGameBtn()
    {
        _canvasGO.SetActive(false);
    }

    public void PlayerDead()
    {
        _playerDeadGO.SetActive(true);
        _fadeScreen.gameObject.SetActive(true);
        _canFade = true;
    }

    private void StartFade()
    {
        _canFade = true;
    }

    public void EnemyDead()
    {
        _enemyDeadGO.SetActive(true);
        _enemyCrawlGO.SetActive(false);
    }    

    public void EnemyCrawl()
    {
        _enemyDeadGO.SetActive(false);
        _enemyCrawlGO.SetActive(true);
        _fadeScreen.gameObject.SetActive(true);
        Invoke(nameof(StartFade), 2f);
    }

    private void Update()
    {
        if (_canFade)
            _fadeScreen.color = Color.Lerp(_fadeScreen.color, new Color(0, 0, 0, 1), _fadeSpeed * Time.deltaTime);

        if (_fadeScreen.color.a >= 0.9f)
        {
            RestartGame();
            return;
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(0);
    }

    public void OnClickExitGame()
    {
        Application.Quit();
    }
}
