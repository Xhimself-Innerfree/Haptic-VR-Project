using UnityEngine;

// by JL April 16 2025 
public class Player_Ctrl : MonoBehaviour
{
    private CharacterController player_char_ctrler;

    public float moveSpeed = 5f;
    public float jumpSpeed = 2f;

    private float horizontalMove, VerticalMove;
    private Vector3 moveDirection;

    //There is no gravity in CharacterCtrler so I add gravity by myself

    public float gravity = 9.81f;
    private Vector3 Vel_Y;

    public Transform groundCheck;
    public float checkRadius = 0.4f;
    public LayerMask groundLayer;
    public bool isGrounded;

    private void Start()
    {
        player_char_ctrler = GetComponent<CharacterController>();   
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer, QueryTriggerInteraction.Ignore);
        if (isGrounded && Vel_Y.y < 0) { 
            Vel_Y.y = -0.1f; // Reset the vertical velocity when grounded
        }


        horizontalMove = Input.GetAxis("Horizontal") * moveSpeed;
        VerticalMove = Input.GetAxis("Vertical") * moveSpeed;
        moveDirection = transform.forward * VerticalMove + transform.right * horizontalMove;
        player_char_ctrler.Move(moveDirection * Time.deltaTime);

        //Jump
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            //Vel_Y.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            Vel_Y.y = jumpSpeed;
        }


        Vel_Y.y -= gravity * Time.deltaTime;
        player_char_ctrler.Move(Vel_Y * Time.deltaTime);
    }
}
