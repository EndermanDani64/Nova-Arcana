using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Base values")]
    public bool staminaRegen;
    public float stamina { get; private set; } = 100;
    public float health = 100; // zzz

    /// <summary>
    /// Addition of stamina and staminaDecrease (parameter).
    /// </summary>
    /// <param name="staminaDecrease">If negative stamina will be lovered by that amount, if positive it's the opposite.</param>
    public void ModifyStamina(float staminaDecrease)
    {
        // If the player doesn't running and we are trying to decrease the stamina, we won't let that happen.
        if (staminaRegen && staminaDecrease < 0) return;

        if (stamina + staminaDecrease < 0)
            stamina = 0;
        else if (stamina + staminaDecrease > 100)
            stamina = 100;
        else
            stamina += staminaDecrease;

        staminaText.text = Mathf.Round(stamina).ToString();
        staminaBar.localScale = new Vector2((float)(stamina / 100), staminaBar.localScale.y);
    }

    [Header("References")]
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private RectTransform staminaBar;
}
