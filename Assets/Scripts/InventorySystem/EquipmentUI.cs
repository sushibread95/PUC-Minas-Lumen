using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    public GameObject equipmentPanel;
    
    // Removido referência ao PlayerControllerSystem pois não precisamos mais travar cursor aqui
    
    void Start()
    {
        // Garante que comece fechado, ou deixa o CharacterMenuWindow gerenciar
        if(equipmentPanel) equipmentPanel.SetActive(false);
    }

    // Este script ficou bem vazio pois a lógica pesada foi para o CharacterMenuWindow.
    // Futuramente, aqui entrará a lógica de mostrar os itens equipados nos slots de armadura/arma.
    
    public void RefreshEquipmentDisplay()
    {
        // Lógica para atualizar ícones de equipamentos
    }
    
    // Se precisar fazer algo quando a aba abre:
    public void OnEquipmentTabOpened()
    {
        RefreshEquipmentDisplay();
    }
}