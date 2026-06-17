using UnityEngine;

public class WeaponShelf : MonoBehaviour
{
    [Header("Objetos da estante")]
    [SerializeField] private GameObject greenMarker;
    [SerializeField] private GameObject weaponVisual;

    private ShelfWeaponManager manager;
    private bool containsWeapon;
    private bool hasBeenOpened;

    private void Awake()
    {
        if (greenMarker != null)
            greenMarker.SetActive(true);

        if (weaponVisual != null)
            weaponVisual.SetActive(false);
    }

    public void Setup(ShelfWeaponManager shelfManager, bool hasWeapon)
    {
        manager = shelfManager;
        containsWeapon = hasWeapon;
        hasBeenOpened = false;

        if (greenMarker != null)
            greenMarker.SetActive(true);

        if (weaponVisual != null)
            weaponVisual.SetActive(false);
    }

    public void OpenShelf()
    {
        if (hasBeenOpened) return;

        hasBeenOpened = true;

        if (greenMarker != null)
            greenMarker.SetActive(false);

        if (!containsWeapon)
            return;

        if (weaponVisual != null)
            weaponVisual.SetActive(true);

        if (manager != null)
            manager.OnCorrectShelfOpened();
    }
}