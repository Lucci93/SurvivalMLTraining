using UnityEngine;

public class Projectile : MonoBehaviour {

    public LayerMask collisionMask;
    public float speed = 10;
    public float damage = 1;

    float lifetime = 3f;
    float skinWidth = .1f;

    void Start() {
        Destroy(gameObject, lifetime);

        Collider[] initialCollisions = Physics.OverlapSphere(transform.position, .1f, collisionMask);
        if (initialCollisions.Length > 0) {
            OnHitObject(initialCollisions[0], transform.position);
        }
    }

    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
    }

    void Update() {
        float moveDistance = speed * Time.deltaTime;
        // check collision with elements
        CheckCollisions(moveDistance);
        // move bullet
        transform.Translate(Vector3.forward * moveDistance);
    }

    void CheckCollisions(float moveDistance) {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        // check collision with enemies
        if (Physics.Raycast(ray, out hit, moveDistance + skinWidth, collisionMask, QueryTriggerInteraction.Collide)) {
            OnHitObject(hit.collider, hit.point);
        }
    }

    void OnHitObject(Collider c, Vector3 hitPoint) {
        // check if the object have a damageble interface to use to damage it
        IDamageable damageableObject = c.GetComponent<IDamageable>();
        if (damageableObject != null) {
            damageableObject.TakeHit(damage, hitPoint, transform.forward);
        }
        // destroy the bullet
        Destroy(gameObject);
    }
}