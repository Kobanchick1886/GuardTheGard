using UnityEngine;

public class Player : MonoBehaviour
{
    private Movement _movement;
    [SerializeField] private int EXP = 0;
    
    private void Awake()
    {   
        _movement = GetComponent<Movement>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EXP"))
        {
            if (collision.IsTouching(GetComponent<BoxCollider2D>()))
            {
                EXP++;
                Destroy(collision.gameObject);
            }
        }
    }
}
