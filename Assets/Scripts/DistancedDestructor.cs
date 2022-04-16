using UnityEngine;

public class DistancedDestructor : MonoBehaviour
{

    private GameObject _player;

    private void Start()
    {
        _player = GameObject.Find("Player");
    }

    private void Update()
    {
        if(!_player)
        {
            _player = GameObject.Find("Player");
            return;
        }
        if(_player.transform.position.x - transform.position.x > 15f)
        {
            Destroy(gameObject);
        }
    }
}
