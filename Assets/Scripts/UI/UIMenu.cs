using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Zenject;
using Input = Scripts.Input;

public class UIMenu : MonoBehaviour
{
    [Inject] private Input _input;
    
    public AudioMixer audioMixer;
    public GameObject mainPanel;
    
    public Button btnContinue;
    public Button btnRestart;
    public Button btnExit;
    public Slider mouseSensitivity;
    public Slider volumeMaster;
    public Slider volumeMusic;
    public Slider volumeVoice;
    public Slider volumeVFX;
    public Slider volumeCutscene;

    public float GetMouseSens => mouseSensitivity.value;
    
    public event Action OnResumed;

    private void Awake()
    {
        mainPanel.SetActive(false);
        // Скрыть курсор
        Cursor.visible = false;
        
        // Заблокировать курсор в центре экрана (опционально)
        Cursor.lockState = CursorLockMode.Locked;
        
        _input.OnActionEcs += HideOrShow;
    }

    private void OnEnable()
    {
        btnContinue.onClick.AddListener(Hide);
        btnRestart.onClick.AddListener(Hide);
        btnExit.onClick.AddListener(ExitGame);
        
        mouseSensitivity.onValueChanged.AddListener(_input.ChangeMouseSensitivity);
    }

    private void Start()
    {
        ChangeVolume();
    }

    private void FixedUpdate()
    {
        if(!mainPanel.activeSelf) return;

        ChangeVolume();
    }

    private void ChangeVolume()
    {
        audioMixer.SetFloat("Master", math.log10(volumeMaster.value) * 20f);
        audioMixer.SetFloat("Music", math.log10(volumeMusic.value) * 20f);
        audioMixer.SetFloat("Voice", math.log10(volumeVoice.value) * 20f);
        audioMixer.SetFloat("VFX", math.log10(volumeVFX.value) * 20f);
        audioMixer.SetFloat("Cutscene", math.log10(volumeCutscene.value) * 20f);
    }

    private void OnDisable()
    {
        btnContinue.onClick.RemoveListener(Hide);
        btnRestart.onClick.RemoveListener(Hide);
        btnExit.onClick.RemoveListener(ExitGame);
        
        mouseSensitivity.onValueChanged.RemoveListener(_input.ChangeMouseSensitivity);
    }

    private void OnDestroy()
    {
        _input.OnActionEcs -= HideOrShow;
    }

    public void Show()
    {
        mainPanel.SetActive(true);
        // Скрыть курсор
        Cursor.visible = true;
        
        // Заблокировать курсор в центре экрана (опционально)
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
    }
    
    public void Hide()
    {
        Cursor.visible = false;
        
        // Заблокировать курсор в центре экрана (опционально)
        Cursor.lockState = CursorLockMode.Locked;
        mainPanel.SetActive(false);
        Time.timeScale = 1f;
        OnResumed?.Invoke();
    }

    private void HideOrShow()
    {
        if (mainPanel.activeSelf)
            Hide();
        else
            Show();
    }

    private void ExitGame()
    {
        Application.Quit();   
    }
}
