using ServiceLocator.Events;
using ServiceLocator.Map;
using ServiceLocator.Player;
using ServiceLocator.Sound;
using ServiceLocator.UI;
using ServiceLocator.Wave;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameService : MonoBehaviour
{
    private PlayerService playerService;
    private SoundService soundService;
    private EventService eventService;
    private WaveService waveService;
    private MapService mapService;

    [SerializeField] private UIService uiService;
    public UIService UIService => uiService;

    [SerializeField] private PlayerScriptableObject playerScriptableObject;
    [SerializeField] private SoundScriptableObject soundScriptableObject;
    [SerializeField] private WaveScriptableObject waveScriptableObject;
    [SerializeField] private MapScriptableObject mapScriptableObject;

    [SerializeField] private AudioSource audioEffects;
    [SerializeField] private AudioSource backgroundMusic;

    private void Start()
    {
        CreateServices();
        InjectDependency();
    }

    private void CreateServices()
    {
        playerService = new PlayerService(playerScriptableObject);
        soundService = new SoundService(soundScriptableObject, audioEffects, backgroundMusic);
        eventService = new EventService();
        waveService = new WaveService(waveScriptableObject);
        mapService = new MapService(mapScriptableObject);
        
    }

    private void InjectDependency()
    {
        playerService.Init(uiService,mapService,soundService);
        waveService.Init(eventService,uiService,mapService,soundService,playerService);
        mapService.Init(eventService);
        uiService.Init(playerService,eventService, waveService);
    }
    private void Update()
    {
        playerService.Update();
    }
}
