using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    [SerializeField] GameObject tomato;

    void Update()
    {
        float move = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(move, 0, 0) * moveSpeed * Time.deltaTime;
        transform.localPosition += movement;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
       
    }
    private void Shoot()
    {
        Instantiate(tomato, transform.position, transform.rotation);
    }

}
