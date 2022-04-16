using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum GameState
{
    START,INPUT,GROWING,NONE
}

public class GameManager : MonoBehaviour
{

    [SerializeField] private Vector3 _startPosition;

    [SerializeField] private Vector2 _minMaxRange, _spawnRange;

    [SerializeField] private GameObject _platformPrefab, _playerPrefab, _stickPrefab, _coinPrefab, _currentCamera;

    [SerializeField] private Transform _rotateTransform, _endRotateTransform;

    [SerializeField] private GameObject _scorePanel, _startPanel, _endPanel;

    [SerializeField] private Text _scoreText, _diamondsText;

    private GameObject _currentPlatform, _nextPlatform, _currentStick, _player;

    private int _score, _coins, _highScore;

    private float _cameraOffsetX;

    private GameState _currentState;

    [SerializeField] private float _stickIncreaseSpeed, _maxStickSize;

    public static GameManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _currentState = GameState.START;

        _endPanel.SetActive(false);
        _scorePanel.SetActive(false);
        _startPanel.SetActive(true);

        _score = 0;
        _coins = PlayerPrefs.HasKey("Coins") ? PlayerPrefs.GetInt("Coins") : 0;
        _highScore = PlayerPrefs.HasKey("HighScore") ? PlayerPrefs.GetInt("HighScore") : 0;

        _scoreText.text = _score.ToString();
        _diamondsText.text = _coins.ToString();

        CreateStartObjects();
        _cameraOffsetX = _currentCamera.transform.position.x - _player.transform.position.x;

        if(StateManager.instance.hasSceneStarted)
        {
            GameStart();
        }
    }

    private void Update()
    {
        if(_currentState == GameState.INPUT)
        {
            if(Input.GetMouseButton(0))
            {
                _currentState = GameState.GROWING;
                ScaleStick();
            }
        }

        if(_currentState == GameState.GROWING)
        {
            if(Input.GetMouseButton(0))
            {
                ScaleStick();
            }
            else
            {
                StartCoroutine(FallStick());
            }
        }
    }

    void ScaleStick()
    {
        Vector3 tempScale = _currentStick.transform.localScale;
        tempScale.y += Time.deltaTime * _stickIncreaseSpeed;
        if (tempScale.y > _maxStickSize)
            tempScale.y = _maxStickSize;
        _currentStick.transform.localScale = tempScale;
    }

    IEnumerator FallStick()
    {
        _currentState = GameState.NONE;
        var x = Rotate(_currentStick.transform, _rotateTransform, 0.4f);
        yield return x;

        Vector3 movePosition = _currentStick.transform.position + new Vector3(_currentStick.transform.localScale.y,0,0);
        movePosition.y = _player.transform.position.y;
        x = Move(_player.transform,movePosition,0.5f);
        yield return x;

        var results = Physics2D.RaycastAll(_player.transform.position,Vector2.down);
        var result = Physics2D.Raycast(_player.transform.position, Vector2.down);
        foreach (var temp in results)
        {
            if(temp.collider.CompareTag("Platform"))
            {
                result = temp;
            }
        }

        if(!result || !result.collider.CompareTag("Platform"))
        {
            _player.GetComponent<Rigidbody2D>().gravityScale = 1f;
            x = Rotate(_currentStick.transform, _endRotateTransform, 0.5f);
            yield return x;
            GameOver();
        }
        else
        {
            UpdateScore();

            movePosition = _player.transform.position;
            movePosition.x = _nextPlatform.transform.position.x + _nextPlatform.transform.localScale.x * 0.5f - 0.35f;
            x = Move(_player.transform, movePosition, 0.2f);
            yield return x;

            movePosition = _currentCamera.transform.position;
            movePosition.x = _player.transform.position.x + _cameraOffsetX;
            x = Move(_currentCamera.transform, movePosition, 0.5f);
            yield return x;

            CreatePlatform();
            SetRandomSize(_nextPlatform);
            _currentState = GameState.INPUT;
            Vector3 stickPosition = _currentPlatform.transform.position;
            stickPosition.x += _currentPlatform.transform.localScale.x * 0.5f - 0.05f;
            stickPosition.y = _currentStick.transform.position.y;
            stickPosition.z = _currentStick.transform.position.z;
            _currentStick = Instantiate(_stickPrefab, stickPosition, Quaternion.identity);
        }
    }


    void CreateStartObjects()
    {
        CreatePlatform();

        Vector3 playerPosition = _playerPrefab.transform.position;
        playerPosition.x += (_currentPlatform.transform.localScale.x * 0.5f - 0.35f);
        _player = Instantiate(_playerPrefab,playerPosition,Quaternion.identity);
        _player.name = "Player";

        Vector3 stickPosition = _stickPrefab.transform.position;
        stickPosition.x += (_currentPlatform.transform.localScale.x*0.5f - 0.05f);
        _currentStick = Instantiate(_stickPrefab, stickPosition, Quaternion.identity);
    }

    void CreatePlatform()
    {
        var currentPlatform = Instantiate(_platformPrefab);
        _currentPlatform = _nextPlatform == null ? currentPlatform : _nextPlatform;
        _nextPlatform = currentPlatform;
        currentPlatform.transform.position = _platformPrefab.transform.position + _startPosition;
        Vector3 tempDistance = new Vector3(Random.Range(_spawnRange.x,_spawnRange.y) + _currentPlatform.transform.localScale.x*0.5f,0,0);
        _startPosition += tempDistance;

        if(Random.Range(0,10) == 0)
        {
            var tempCoins = Instantiate(_coinPrefab);
            Vector3 tempPosition = currentPlatform.transform.position;
            tempPosition.y = _coinPrefab.transform.position.y;
            tempCoins.transform.position = tempPosition;
        }
    }

    void SetRandomSize(GameObject platform)
    {
        var newScale = platform.transform.localScale;
        var allowedScale = _nextPlatform.transform.position.x - _currentPlatform.transform.position.x
            - _currentPlatform.transform.localScale.x * 0.5f - 0.4f;
        newScale.x = Mathf.Max(_minMaxRange.x,Random.Range(_minMaxRange.x,Mathf.Min(allowedScale,_minMaxRange.y)));
        platform.transform.localScale = newScale;
    }

    void UpdateScore()
    {
        _score++;
        _scoreText.text = _score.ToString();
    }

    void GameOver()
    {
        _endPanel.SetActive(true);
        _scorePanel.SetActive(false);

        if(_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt("HighScore", _highScore);
        }
    }

    public void UpdateCoins()
    {
        _coins++;
        PlayerPrefs.SetInt("Coins", _coins);
        _diamondsText.text = _coins.ToString();
    }

    public void GameStart()
    {
        _startPanel.SetActive(false);
        _scorePanel.SetActive(true);

        CreatePlatform();
        SetRandomSize(_nextPlatform);
        _currentState = GameState.INPUT;
        
    }

    public void GameRestart()
    {
        StateManager.instance.hasSceneStarted = false;
        SceneManager.LoadScene(0);
    }

    public void SceneRestart()
    {
        StateManager.instance.hasSceneStarted = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    IEnumerator Move(Transform currentTransform,Vector3 target,float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.position;
        while(passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Vector3.Lerp(init, target, normalized);
            currentTransform.position = current;
            yield return null;
        }
    }

    IEnumerator Rotate(Transform currentTransform, Transform target, float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.rotation;
        while (passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Quaternion.Slerp(init, target.rotation, normalized);
            currentTransform.rotation = current;
            yield return null;
        }
    }
}
