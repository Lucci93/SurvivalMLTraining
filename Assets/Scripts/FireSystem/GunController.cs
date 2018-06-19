using UnityEngine;

public class GunController : MonoBehaviour {

    public Transform weaponHold;
    public Gun startingGun;
    Gun equippedGun;

    void Start() {
        // at start equip the default gun
        if (startingGun != null) {
            EquipGun(startingGun);
        }
    }

    // equip the gun
    public void EquipGun(Gun gunToEquip) {
        if (equippedGun != null) {
            Destroy(equippedGun.gameObject);
        }
        equippedGun = Instantiate(gunToEquip, weaponHold.position, weaponHold.rotation);
        equippedGun.transform.parent = weaponHold;
    }

    // shoot the projectile
    public void Shoot() {
        if (equippedGun != null) {
            equippedGun.Shoot();
        }
    }
}