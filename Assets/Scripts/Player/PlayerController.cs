using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

    Vector3 velocity;
    Rigidbody myRigidbody;

    void Start() {
        myRigidbody = GetComponent<Rigidbody>();
    }

    public void Move(Vector3 _velocity) {
        velocity = _velocity;
    }

    // player rotate to look at some point
    public void LookAt(Vector3 lookPoint) {
        // for the right height to look
        Vector3 heightCorrectedPoint = new Vector3(lookPoint.x, transform.position.y, lookPoint.z);
        transform.LookAt(heightCorrectedPoint);
    }

    void FixedUpdate() {
        // move with a certain velocity
        myRigidbody.MovePosition(myRigidbody.position + velocity * Time.fixedDeltaTime);
    }
}
