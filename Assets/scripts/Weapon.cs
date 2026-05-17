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
            shootPointObj.transform.SetParent(transform);
            shootPointObj.transform.localPosition = new Vector3(0, 0, 0.5f);
            shootPoint = shootPointObj.transform;
        }

        playerController = FindObjectOfType<Player>();
    }

    private void OnCollected()
    {
        if (hasBeenCollected) return;
        hasBeenCollected = true;

        // Salva posição APÓS ser coletada (quando está na mão)
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

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb == null)
            bulletRb = bullet.AddComponent<Rigidbody>();

        bulletRb.linearVelocity = shootPoint.forward * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
            bulletScript = bullet.AddComponent<Bullet>();

        balasNoPente--;
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

        // Garantir que volta exatamente para originalPosition
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