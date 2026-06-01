using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("Munição")]
    [SerializeField] private int balasNoPente = 10;
    [SerializeField] private int tamanhoDoPente = 10;
    [SerializeField] private int balasNoBolso = 40;

    [Header("Tiro")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private float fireRate = 0.2f;

    [Header("Recarregamento")]
    [SerializeField] private float tempoRecarga = 1.5f;
    [SerializeField] private float reloadAnimHeight = 0.2f;
    [SerializeField] private float reloadAnimDuration = 0.5f;

    private bool isReloading;
    private float lastShotTime;
    private Vector3 originalPosition;
    private Player playerController;
    private bool hasBeenCollected;

    private void Start()
    {
        playerController = FindObjectOfType<Player>();

        if (shootPoint == null)
        {
            GameObject shootPointObj = new GameObject("ShootPoint");
            shootPointObj.transform.SetParent(transform);
            shootPointObj.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            shootPointObj.transform.localRotation = Quaternion.identity;
            shootPoint = shootPointObj.transform;
        }

        originalPosition = transform.localPosition;
        UpdateAmmoUI();
    }

    private void Update()
    {
        if (playerController == null) return;
        if (!playerController.HasEquippedWeapon()) return;

        HandleShooting();
        HandleReload();
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && !isReloading)
        {
            if (Time.time - lastShotTime >= fireRate)
            {
                Shoot();
            }
        }
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            if (balasNoPente < tamanhoDoPente && balasNoBolso > 0)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Shoot()
    {
        if (balasNoPente <= 0) return;
        if (bulletPrefab == null) return;

        Camera playerCamera = Camera.main;

        if (playerCamera == null && playerController != null)
        {
            playerCamera = playerController.GetComponentInChildren<Camera>();
        }

        if (playerCamera == null) return;

        Vector3 spawnPos = shootPoint != null
            ? shootPoint.position
            : playerCamera.transform.position + playerCamera.transform.forward * 0.6f;

        Quaternion bulletRotation = Quaternion.LookRotation(playerCamera.transform.forward);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRotation);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb == null)
        {
            bulletRb = bullet.AddComponent<Rigidbody>();
        }

        bulletRb.useGravity = false;
        bulletRb.isKinematic = false;
        bulletRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        bulletRb.linearVelocity = playerCamera.transform.forward * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<Bullet>();
        }

        balasNoPente--;
        lastShotTime = Time.time;

        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in allEnemies)
        {
            enemy.OnPlayerShot(playerCamera.transform.position);
        }

        Destroy(bullet, bulletLifetime);
        UpdateAmmoUI();
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        float elapsedTime = 0f;

        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;
            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * t);
            yield return null;
        }

        float waitTime = Mathf.Max(0f, tempoRecarga - reloadAnimDuration);
        yield return new WaitForSeconds(waitTime);

        elapsedTime = 0f;

        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;
            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * (1f - t));
            yield return null;
        }

        transform.localPosition = originalPosition;

        int balasFaltando = tamanhoDoPente - balasNoPente;
        int balasTransferidas = Mathf.Min(balasFaltando, balasNoBolso);

        balasNoPente += balasTransferidas;
        balasNoBolso -= balasTransferidas;

        isReloading = false;

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateAmmo(balasNoPente, tamanhoDoPente, balasNoBolso);
        }
    }

    private void OnCollected()
    {
        if (hasBeenCollected) return;

        hasBeenCollected = true;
        originalPosition = transform.localPosition;
        UpdateAmmoUI();
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    public void NotifyCollected()
    {
        OnCollected();
    }
}