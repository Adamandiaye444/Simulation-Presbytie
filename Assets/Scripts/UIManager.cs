using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// G�re l'interface utilisateur pour contr�ler la simulation du daltonisme.
/// Connecte les boutons, le dropdown et met � jour dynamiquement la description.
/// </summary>
public class UIManager : MonoBehaviour
{
    public DaltonismSimulator simulator;
    public PresbytieSimulator presbytieSimulator;

    public Button normalButton;
    public Button protanopiaButton;
    public Button deuteranopiaButton;
    public Button tritanopiaButton;
    public Button vssButton;
    public Button resetButton;

    public Button presbytieButton;


    public Dropdown modeDropdown;
    public Text descriptionText; // Zone de texte pour la description dynamique

    private string[] descriptions = new string[]
    {
        "Normale: vision classique sans alt�ration de la perception des couleurs. Ce mode repr�sente la vision humaine standard, utilis�e comme r�f�rence pour comparer les effets des autres modes.",

        "Protanopie : les personnes atteintes de protanopie ne per�oivent pas la lumi�re rouge. Les rouges apparaissent comme des nuances de brun ou de vert fonc�, et les violets peuvent �tre confondus avec les bleus. Ce type de daltonisme affecte la vision des contrastes entre le rouge et le vert.",

        "Deut�ranopie : les individus deut�ranopes ne d�tectent pas la lumi�re verte. Les verts, rouges et oranges sont per�us comme tr�s similaires, ce qui rend la distinction entre ces couleurs difficile. Ce type est l�un des plus fr�quents et peut affecter la lecture de graphiques ou panneaux de signalisation.",

        "Tritanopie : tr�s rare, la tritanopie entra�ne une difficult� � diff�rencier le bleu du vert et le jaune du violet. Les bleus peuvent para�tre verd�tres et les jaunes peuvent sembler ros�s ou incolores.",

        "Visual Snow Syndrome : une perturbation neurologique o� la personne voit en permanence une sorte de \"neige\" ou bruit visuel, comme un �cran de t�l�vision non r�gl�. Le bruit est souvent plus visible dans les zones sombres et peut interf�rer avec la vision normale, m�me dans des environnements lumineux.",

        "Presbytie : trouble de la vision lié à l’âge, affectant la capacité à voir de près. Cette simulation applique un flou aux objets proches pour illustrer la difficulté à faire la mise au point.",

        "R�initialisation : retour au mode normal. Annule les effets de simulation pour retrouver une perception classique des couleurs."
    };


    void Start()
    {
        if (simulator == null)
        {
            Debug.LogError("UIManager : DaltonismSimulator non assign� !");
            return;
        }

        // Assignation des �v�nements aux boutons
        if (normalButton != null) normalButton.onClick.AddListener(() => OnModeSelected(0));
        if (protanopiaButton != null) protanopiaButton.onClick.AddListener(() => OnModeSelected(1));
        if (deuteranopiaButton != null) deuteranopiaButton.onClick.AddListener(() => OnModeSelected(2));
        if (tritanopiaButton != null) tritanopiaButton.onClick.AddListener(() => OnModeSelected(3));
        if (vssButton != null) vssButton.onClick.AddListener(() => OnModeSelected(4));
        if (presbytieButton != null) presbytieButton.onClick.AddListener(TogglePresbytie);
        if (resetButton != null) resetButton.onClick.AddListener(() => OnModeSelected(0));

        // Dropdown
        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeSelected);
            modeDropdown.value = 0;
            modeDropdown.RefreshShownValue();
        }

        // Description initiale
        UpdateDescription(0);

        Debug.Log("UIManager : Interface correctement initialis�e.");
    }




    void OnModeSelected(int mode)
    {
        ChangeDaltonismMode(mode);
        UpdateDescription(mode);
    }

    void ChangeDaltonismMode(int mode)
    {
        if (simulator != null)
        {
            simulator.SetDaltonismMode(mode);
            Debug.Log("Mode Daltonisme chang� : " + mode);
        }
    }

    void UpdateDescription(int mode)
    {
        if (descriptionText != null && mode >= 0 && mode < descriptions.Length)
        {
            descriptionText.text = descriptions[mode];
        }
    }


   void TogglePresbytie()
   {
       if (presbytieSimulator == null) return;

       if (presbytieSimulator.isActive)
       {
           presbytieSimulator.DisablePresbytie();
           descriptionText.text = "Simulation désactivée (mode normal)";
       }
       else
       {
           presbytieSimulator.EnablePresbytie();

           descriptionText.text = "Simulation Presbytie activée";
       }
   }

}
