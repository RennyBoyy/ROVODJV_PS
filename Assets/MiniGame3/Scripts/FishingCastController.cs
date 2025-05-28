using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class FishingCastController : MonoBehaviour
{
    [Tooltip("Check this on the Player1 GameObject, leave unchecked on Player2 (for future logic)")]
    [SerializeField] private bool isPlayerOne = false;

    // reference to the Cast action in our input actions asset
    private InputAction _castAction;

    // state tracking
    private bool _pulledDown = false;

    private void Awake()
    {
        // If this is Player2 but there's no second gamepad, disable the whole script
        if (!isPlayerOne && Gamepad.all.Count < 2)
        {
            Debug.Log($"{name}: No controller detected for Player2, disabling Player2 actions");
            enabled = false;
            return;
        }

        var pi = GetComponent<PlayerInput>();

        // only set scheme on Player 1
        if (isPlayerOne && Gamepad.all.Count > 0)
            pi.SwitchCurrentControlScheme("PS4_P1", Gamepad.all[0]);
        else if (!isPlayerOne && Gamepad.all.Count > 1)
            pi.SwitchCurrentControlScheme("PS4_P2", Gamepad.all[1]);

        // grab the Cast action from the PlayerInput component
        _castAction = pi.actions["Cast"];
    }

    private void OnEnable()
    {
        // only subscribe if _castAction was set
        if (_castAction != null)
            _castAction.performed += OnCastPerformed;
    }

    private void OnDisable()
    {
        // only unsubscribe if we have a valid action
        if (_castAction != null)
            _castAction.performed -= OnCastPerformed;
    }

    private void OnCastPerformed(InputAction.CallbackContext ctx)
    {
        // filter out callbacks on the wrong scheme
        var scheme = GetComponent<PlayerInput>().currentControlScheme;
        if (isPlayerOne ? scheme != "PS4_P1" : scheme != "PS4_P2")
            return;

        float value = ctx.ReadValue<float>();

        // detect initial pull down
        if (!_pulledDown && value < -0.9f)
        {
            _pulledDown = true;
            Debug.Log($"{name}: Cast pull-down detected from scheme {scheme}");
            return;
        }

        // once pulled down, detect push up
        if (_pulledDown && value > 0.9f)
        {
            _pulledDown = false;
            Debug.Log($"{name}: Cast push-up detected from scheme {scheme}");
            DoCast();
        }
    }

    private void DoCast()
    {
        Debug.Log($"{name}: performed cast logic");
    }
}
