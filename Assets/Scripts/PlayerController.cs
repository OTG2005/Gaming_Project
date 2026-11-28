using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
 public float moveSpeed;
 public float jumpHeight;
 public KeyCode jump;
 public KeyCode left;
 public KeyCode right;
 public KeyCode jump2;
 public KeyCode left2;
 public KeyCode right2;
 public Transform groundCheck;
 public float groundCheckRadius;
 public LayerMask WhatIsGround;
 private bool isGrounded;
 private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
         
    }

    // Update is called once per frame
    void Update()
    {
      
      if((Input.GetKeyDown(jump) || Input.GetKeyDown(jump2)) && isGrounded)
        {
          Jump();  
        }
        if(Input.GetKey(left) || Input.GetKey(left2))
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(-moveSpeed, GetComponent<Rigidbody2D>().velocity.y);
       if(GetComponent<SpriteRenderer>()!= null)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }
        }
        if(Input.GetKey(right) || Input.GetKey(right2))
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(moveSpeed, GetComponent<Rigidbody2D>().velocity.y);
            if(GetComponent<SpriteRenderer>()!= null)
            {
                GetComponent<SpriteRenderer>().flipX = false;
            }



        }
void Jump()
    {
        GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, jumpHeight);
    }

anim.SetFloat("speed",Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x));
         anim.SetFloat("Height", (GetComponent<Rigidbody2D>().velocity.y));
         anim.SetBool("grounded", isGrounded);

    }
    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, WhatIsGround);
    }
}
