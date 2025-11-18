public interface IInteractable
{
    // Qualquer script que implementar esta interface
    // é OBRIGADO a ter essas duas funções:

    // 1. A função que é chamada quando o player interage
    void Interact();

    // 2. A função que retorna o texto de "dica" (ex: "Pegar Chave")
    // (Isso é para a nossa UI de "prompt")
    string GetInteractText();
}