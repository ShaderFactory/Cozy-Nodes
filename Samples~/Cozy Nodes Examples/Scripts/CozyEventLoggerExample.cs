using UnityEngine;
using ShaderFactory.CozyGraphToolkit.Runtime;

public class CozyEventLoggerExample : MonoBehaviour
{
    [SerializeField] private CozyManager cozyManager;

    private void OnEnable()
    {
        cozyManager.EventInvoked += OnCozyEvent;
    }

    private void OnDisable()
    {
        if (cozyManager != null)
            cozyManager.EventInvoked -= OnCozyEvent;
    }

    private void OnCozyEvent(CozyEvent cozyEvent)
    {
        Debug.Log(
            "Evento: " + cozyEvent.EventName +
            " | Valor: " + cozyEvent.Payload
        );
    }
}