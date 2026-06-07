using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileControlsGenerator : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerInputHandler inputHandler;

    private RectTransform knob;
    private float joystickRadius = 100f;
    private GameObject canvasGO;

    private void Start()
    {
        // Cria a interface apenas em plataformas mobile ou se o target da build for Android/iOS
        if (!Application.isMobilePlatform && SystemInfo.deviceType != DeviceType.Handheld)
        {
            return;
        }

        playerMovement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();

        SetupExistingCanvas();
    }

    private void SetupExistingCanvas()
    {
        // Certifica-se de que existe um EventSystem na cena
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Tenta encontrar o Canvas mesmo que ele esteja desativado (invisível) na cena
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            // Garante que é um objeto da cena e não um Prefab solto nos arquivos
            if (c.gameObject.scene.isLoaded)
            {
                if (c.CompareTag("Joystick") || c.name == "MobileControlsCanvas")
                {
                    canvasGO = c.gameObject;
                    break;
                }
            }
        }

        // Se ainda não encontrar, avisa o usuário
        if (canvasGO == null)
        {
            Debug.LogError("Não foi possível encontrar o MobileControlsCanvas na cena! Certifique-se de colocar o seu Prefab na cena.");
            return;
        }

        // Se estamos aqui, é porque estamos no Mobile (devido ao check no Start), então ativamos!
        canvasGO.SetActive(true);

        // Buscar o JoystickBase
        Transform baseTransform = FindChildRecursive(canvasGO.transform, "JoystickBase");
        if (baseTransform != null)
        {
            Transform knobTransform = FindChildRecursive(baseTransform, "JoystickKnob");
            if (knobTransform != null)
            {
                knob = knobTransform.GetComponent<RectTransform>();

                // Configurar EventTriggers do Joystick
                EventTrigger trigger = baseTransform.GetComponent<EventTrigger>();
                if (trigger == null) trigger = baseTransform.gameObject.AddComponent<EventTrigger>();
                
                trigger.triggers.Clear(); // Limpar triggers antigos salvos no prefab

                EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDown.callback.AddListener((data) => { OnDragJoystick((PointerEventData)data); });
                trigger.triggers.Add(pointerDown);

                EventTrigger.Entry drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                drag.callback.AddListener((data) => { OnDragJoystick((PointerEventData)data); });
                trigger.triggers.Add(drag);

                EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pointerUp.callback.AddListener((data) => { 
                    knob.anchoredPosition = Vector2.zero; 
                    if (playerMovement != null) 
                    {
                        playerMovement.SetMoveInput(Vector2.zero);
                        playerMovement.SetSprinting(false);
                    }
                });
                trigger.triggers.Add(pointerUp);
            }
        }

        // Buscar o botão de pulo (JumpBtn)
        Transform jumpBtnTransform = FindChildRecursive(canvasGO.transform, "JumpBtn");
        if (jumpBtnTransform != null)
        {
            EventTrigger trigger = jumpBtnTransform.GetComponent<EventTrigger>();
            if (trigger == null) trigger = jumpBtnTransform.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            Image img = jumpBtnTransform.GetComponent<Image>();
            Color originalColor = Color.white;
            if (img != null) originalColor = img.color;
            
            EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => { 
                if (img != null) {
                    Color pressedColor = originalColor;
                    pressedColor.a *= 0.6f; // Diminui o alpha pra dar efeito de clique
                    img.color = pressedColor;
                }
                if (playerMovement != null) playerMovement.Jump(); 
            });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => { 
                if (img != null) img.color = originalColor; // Restaura a cor e brilho originais
            });
            trigger.triggers.Add(pointerUp);
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDragJoystick(PointerEventData data)
    {
        if (knob == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            knob.parent.GetComponent<RectTransform>(), 
            data.position, 
            data.pressEventCamera, 
            out Vector2 localPoint
        );

        Vector2 direction = localPoint;
        if (direction.magnitude > joystickRadius)
        {
            direction = direction.normalized * joystickRadius;
        }
        knob.anchoredPosition = direction;

        if (playerMovement != null)
        {
            float inputMagnitude = direction.magnitude / joystickRadius;
            playerMovement.SetMoveInput(direction / joystickRadius);

            // Correr automaticamente se o joystick for puxado perto da borda (ex: > 80%)
            playerMovement.SetSprinting(inputMagnitude > 0.8f);
        }
    }

    private void OnDestroy()
    {
        // Agora usamos o Canvas da cena (o Prefab), então NÃO destruímos ele aqui
        // O Canvas pode ficar fixo na cena.
    }
}
