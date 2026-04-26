using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Método para iniciar o jogo (Fase 1)
    public void Play()
    {
        Debug.Log("Carregando InitialScene...");
        SceneManager.LoadScene("InitialScene");
    }

    // Método para abrir a tela de créditos
    public void Creditos()
    {
        Debug.Log("Carregando Creditos...");
        SceneManager.LoadScene("Creditos");
    }

    // Método para voltar ao menu principal (usado na tela de créditos)
    public void Voltar()
    {
        Debug.Log("Voltando para o Menu...");
        SceneManager.LoadScene("Menu");
    }

    // Método para sair do jogo
    public void Sair()
    {
        Debug.Log("Saindo do jogo...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
