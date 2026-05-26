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
    [SerializeField] private float fireRate = 0.1f;

    [Header("Recarregamento")]
    [SerializeField] private float tempoRecarga = 1.5f;
    [SerializeField] private float reloadAnimHeight = 0.2f;
    [SerializeField] private float reloadAnimDuration = 0.5f;

    private bool isReloading = false;
    private float lastShotTime = 0f;
    private Vector3 originalPosition;
    private Player playerController;
    private bool hasBeenCollected = false;

    private void Start()
    {
        if (shootPoint == null)
        {
            GameObject shootPointObj = new GameObject("ShootPoint");
            shootPointObj.transform.SetParent(transform.parent);
            shootPointObj.transform.localPosition = transform.localPosition + new Vector3(0, 0, 0.5f);
            shootPoint = shootPointObj.transform;
        }

        playerController = FindObjectOfType<Player>();
    }

    private void OnCollected()
    {
        if (hasBeenCollected) return;
        hasBeenCollected = true;

        originalPosition = transform.localPosition;
        UpdateAmmoUI();
    }

    private void Update()
    {
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
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && (balasNoPente < tamanhoDoPente && balasNoBolso > 0))
        {
            StartCoroutine(Reload());
        }
    }

    private void Shoot()
    {
        if (balasNoPente <= 0) return;

        // Pega a câmera do player
        Camera playerCamera = playerController.GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = playerController.GetComponentInChildren<Camera>();
        }

        // Tira a bala da câmera
        Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * 0.5f;

        // Rotaciona o cilindro para ficar deitado na direção do tiro
        Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.up, playerCamera.transform.forward);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRotation);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb == null)
            bulletRb = bullet.AddComponent<Rigidbody>();

        bulletRb.linearVelocity = playerCamera.transform.forward * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
            bulletScript = bullet.AddComponent<Bullet>();

        balasNoPente--;

        // AVISAR INIMIGOS DO TIRO
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in allEnemies)
        {
            enemy.OnPlayerShot(playerCamera.transform.position);
        }

        lastShotTime = Time.time;

        Destroy(bullet, bulletLifetime);
        UpdateAmmoUI();
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        float elapsedTime = 0f;

        // Animação: subir
        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;
            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * t);
            yield return null;
        }

        // Tempo de recarga
        yield return new WaitForSeconds(tempoRecarga - reloadAnimDuration);

        // Animação: descer
        elapsedTime = 0f;
        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;
            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * (1 - t));
            yield return null;
        }

        transform.localPosition = originalPosition;

        // Completar o pente
        int balasFaltando = tamanhoDoPente - balasNoPente;
        int balasTransferidas = Mathf.Min(balasFaltando, balasNoBolso);

        balasNoPente += balasTransferidas;
        balasNoBolso -= balasTransferidas;

        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        GameManager.instance.UpdateAmmo(balasNoPente, tamanhoDoPente, balasNoBolso);
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