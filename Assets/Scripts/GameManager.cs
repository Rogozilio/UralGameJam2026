using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public enum GameStep
{
    Menu,
    CutsceneBegin,
    Tutorial,
    CutsceneMatchToOpenBox,
    MainGame,
    СutsceneDropAxe,
    lastGame
}

public class GameManager : MonoBehaviour
{
    public GameStep gameStep;
    [Space] public PlayableDirector cutsceneBegin;

    [InspectorName("CutsceneMentosMove->Fall")]
    public PlayableDirector cutsceneMatchToOpenBox;

    [InspectorName("CutsceneMentosFall->Gun")]
    public PlayableDirector cutsceneRespawn;

    [InspectorName("CutsceneMentosGun->Toaster")]
    public PlayableDirector cutsceneDropAxe;

    [Space] 
    public UnityEvent Menu;
    public UnityEvent Tutorial;
    public UnityEvent mainGame;
    public UnityEvent lastGame;

    private void Awake()
    {
        SwitchGameStep(gameStep);
    }

    private void Start()
    {
        SwitchGameStep(gameStep);
    }

    private void OnEnable()
    {
        cutsceneBegin.stopped += (ctx) => SwitchGameStep(GameStep.Tutorial);
        cutsceneMatchToOpenBox.stopped +=(ctx) => SwitchGameStep(GameStep.MainGame);
        cutsceneDropAxe.stopped +=(ctx) => SwitchGameStep(GameStep.lastGame);
      
    }

    private void OnDisable()
    {
        cutsceneBegin.stopped -= (ctx) => SwitchGameStep(GameStep.Tutorial);
        cutsceneMatchToOpenBox.stopped -=(ctx) => SwitchGameStep(GameStep.MainGame);
        cutsceneDropAxe.stopped -=(ctx) => SwitchGameStep(GameStep.lastGame);
    }

    public void SwitchOn(int index)
    {
        SwitchGameStep((GameStep)index);
    }
    
    public void SwitchGameStep(GameStep gameStep, float time = 0f)
    {
        this.gameStep = gameStep;
        
        switch (this.gameStep)
        {
            case GameStep.Menu:
                Menu?.Invoke();
                break;
            case GameStep.CutsceneBegin:
                PlayCutscene(cutsceneBegin);
                break;
            case GameStep.Tutorial:
                Tutorial?.Invoke();
                break;
            case GameStep.CutsceneMatchToOpenBox:
                PlayCutscene(cutsceneMatchToOpenBox);
                break;
            case GameStep.MainGame:
                mainGame?.Invoke();
                break;
            case GameStep.СutsceneDropAxe:
                PlayCutscene(cutsceneDropAxe);
                break;
            case GameStep.lastGame:
                lastGame?.Invoke();
                break;
        }
    }

    private void PlayCutscene(PlayableDirector cutscene)
    {
        if (cutscene)
            cutscene.Play();
        else
            SwitchGameStep(gameStep + 1);
    }
}