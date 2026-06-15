using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("Troca de cena")]
    [SerializeField] private bool changeSceneOnCollect = true;
    [SerializeField] private string nextSceneName = "fase2";

    [Header("Munição")]
    [SerializeField] private int balasNoPente = 10;
    [SerializeField] private int tamanhoDoPente = 10;
    [SerializeField] private int balasNoBolso = 40;

    [Header("Tiro")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private float fireRate = 0.1f;

    [Header("Recarregamento")]
    [SerializeField] private float tempoRecarga = 1.5f;
    [SerializeField] private float reloadAnimHeight = 0.2f;
    [SerializeField] private float reloadAnimDuration = 0.5f;

    private bool isReloading;
    private bool hasBeenCollected;
    private float lastShotTime;
    private Vector3 originalPosition;
    private Player playerController;

    private void Update()
    {
        if (!hasBeenCollected) return;
        if (playerController == null) return;

        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused())
            return;

        HandleShooting();
        HandleReload();
    }

    public void Collect(Transform handPoint, Player player)
    {
        if (hasBeenCollected) return;

        hasBeenCollected = true;
        playerController = player;

        transform.SetParent(handPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            Destroy(rb);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        originalPosition = transform.localPosition;

        UpdateAmmoUI();
        if (changeSceneOnCollect)
        {
            GameStateManager.Instance.GoToScene(nextSceneName);
        }

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

        Camera playerCamera = playerController.GetPlayerCamera();

        Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * 0.5f;
        Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.up, playerCamera.transform.forward);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRotation);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb == null)
            bulletRb = bullet.AddComponent<Rigidbody>();

        bulletRb.linearVelocity = playerCamera.transform.forward * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript == null)
            bullet.AddComponent<Bullet>();

        balasNoPente--;

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

        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;

            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * t);

            yield return null;
        }

        yield return new WaitForSeconds(tempoRecarga - reloadAnimDuration);

        elapsedTime = 0f;

        while (elapsedTime < reloadAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / reloadAnimDuration;

            transform.localPosition = originalPosition + Vector3.up * (reloadAnimHeight * (1 - t));

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

    public bool IsReloading()
    {
        return isReloading;
    }
}