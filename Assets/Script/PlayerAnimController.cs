using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    public Animator animator;
    public float speed = 5f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);
        transform.Translate(move * speed * Time.deltaTime, Space.World);

        bool isWalking = move.magnitude > 0.1f;
        animator.SetBool("isWalking", isWalking);
    }
}
