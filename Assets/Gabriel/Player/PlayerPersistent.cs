using UnityEngine;

public class PlayerPersistent : MonoBehaviour
{
    public static PlayerPersistent Instance { get; private set; }

    void Awake()
    {
        // Se já existe um Player vindo de outra cena, EU sou o impostor.
        // Isso acontece se você colocar um player solto na cena para testes.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Se não existe, eu assumo o posto.
        Instance = this;
        DontDestroyOnLoad(gameObject); // <--- A MÁGICA ESTÁ AQUI
    }
}