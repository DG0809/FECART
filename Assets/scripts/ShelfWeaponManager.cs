using System.Collections.Generic;
using UnityEngine;

public class ShelfWeaponManager : MonoBehaviour
{
    [Header("Fase")]
    [SerializeField] private string nextSceneName = "fase2";

    [Header("Estantes")]
    [SerializeField] private List<WeaponShelf> shelves = new List<WeaponShelf>();
    [SerializeField] private bool findShelvesAutomatically = true;

    private WeaponShelf correctShelf;
    private bool weaponFound;

    private void Start()
    {
        if (findShelvesAutomatically || shelves.Count == 0)
        {
            shelves.Clear();
            shelves.AddRange(FindObjectsOfType<WeaponShelf>());
        }

        ChooseRandomShelf();

        ObjectiveHud.GetOrCreate().ShowObjective();
    }

    private void ChooseRandomShelf()
    {
        if (shelves.Count == 0)
        {
            Debug.LogWarning("ShelfWeaponManager: nenhuma estante foi encontrada.");
            return;
        }

        int randomIndex = Random.Range(0, shelves.Count);
        correctShelf = shelves[randomIndex];

        foreach (WeaponShelf shelf in shelves)
        {
            if (shelf == null) continue;

            shelf.Setup(this, shelf == correctShelf);
        }
    }

    public void OnCorrectShelfOpened()
    {
        if (weaponFound) return;

        weaponFound = true;

        if (ObjectiveHud.Instance != null)
            ObjectiveHud.Instance.HideObjective();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartFoundWeaponSequence(nextSceneName);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
