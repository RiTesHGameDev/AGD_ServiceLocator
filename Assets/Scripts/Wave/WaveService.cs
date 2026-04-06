using ServiceLocator.Events;
using ServiceLocator.Map;
using ServiceLocator.Player;
using ServiceLocator.Sound;
using ServiceLocator.UI;
using ServiceLocator.Wave.Bloon;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ServiceLocator.Wave
{
    public class WaveService
    {
        private WaveScriptableObject waveScriptableObject;
        private BloonPool bloonPool;

        private int currentWaveId;
        private List<WaveData> waveDatas;
        private List<BloonController> activeBloons;

        private EventService eventService;
        private UIService uIService;
        private MapService mapService;
        private SoundService soundService;
        private PlayerService playerService;

        public WaveService(WaveScriptableObject waveScriptableObject) 
        {
            this.waveScriptableObject = waveScriptableObject;
        }

        public void Init(EventService eventService,UIService uIService,
            MapService mapService,SoundService soundService,PlayerService playerService)
        {
            this.eventService = eventService;
            this.uIService = uIService;
            this.mapService = mapService;
            this.soundService = soundService;
            this.playerService = playerService;

            InitializeBloons();
            SubscribeToEvents();
        }

        private void InitializeBloons()
        {
            bloonPool = new BloonPool(waveScriptableObject,playerService,this,soundService);
            activeBloons = new List<BloonController>();
        }

        private void SubscribeToEvents() => eventService.OnMapSelected.AddListener(LoadWaveDataForMap);

        private void LoadWaveDataForMap(int mapId)
        {
            currentWaveId = 0;
            waveDatas = waveScriptableObject.WaveConfigurations.Find(config => config.MapID == mapId).WaveDatas;
            uIService.UpdateWaveProgressUI(currentWaveId, waveDatas.Count);
        }

        public void StarNextWave()
        {
            currentWaveId++;
            var bloonsToSpawn = GetBloonsForCurrentWave();
            var spawnPosition = mapService.GetBloonSpawnPositionForCurrentMap();
            SpawnBloons(bloonsToSpawn, spawnPosition, 0, waveScriptableObject.SpawnRate);
        }

        public void SpawnBloons(List<BloonType> bloonsToSpawn, Vector3 spawnPosition, int startingWaypointIndex, float spawnRate)
        {
            CoroutineRunner.Instance.StartCoroutine(SpawnBloonsCoroutine(bloonsToSpawn, spawnPosition, startingWaypointIndex, spawnRate));
        }

        private IEnumerator SpawnBloonsCoroutine(List<BloonType> bloonsToSpawn, Vector3 spawnPosition, int startingWaypointIndex, float spawnRate)
        {
            foreach (BloonType bloonType in bloonsToSpawn)
            {
                BloonController bloon = bloonPool.GetBloon(bloonType);
                bloon.SetPosition(spawnPosition);
                bloon.SetWayPoints(mapService.GetWayPointsForCurrentMap(), startingWaypointIndex);

                AddBloon(bloon);

                yield return new WaitForSeconds(spawnRate);
            }
        }

        private void AddBloon(BloonController bloonToAdd)
        {
            activeBloons.Add(bloonToAdd);
            bloonToAdd.SetOrderInLayer(-activeBloons.Count);
        }

        public void RemoveBloon(BloonController bloon)
        {
            bloonPool.ReturnItem(bloon);
            activeBloons.Remove(bloon);
            if (HasCurrentWaveEnded())
            {
                soundService.PlaySoundEffects(Sound.SoundType.WaveComplete);
                uIService.UpdateWaveProgressUI(currentWaveId, waveDatas.Count);

                if(IsLevelWon())
                    uIService.UpdateGameEndUI(true);
                else
                    uIService.SetNextWaveButton(true);
            }
        }

        private List<BloonType> GetBloonsForCurrentWave() => waveDatas.Find(waveData => waveData.WaveID == currentWaveId).ListOfBloons;

        private bool HasCurrentWaveEnded() => activeBloons.Count == 0;

        private bool IsLevelWon() => currentWaveId >= waveDatas.Count;
    }
}