using System.Collections.Generic;
using UnityEngine;
using ServiceLocator.Player.Projectile;
using ServiceLocator.UI;
using ServiceLocator.Map;
using ServiceLocator.Sound;

namespace ServiceLocator.Player
{
    public class PlayerService
    {
        private PlayerScriptableObject playerScriptableObject;
        private ProjectilePool projectilePool;

        private List<MonkeyController> activeMonkeys;
        private MonkeyView selectedMonkeyView;
        private int health;

        public int Money { get; private set; }

        public PlayerService(PlayerScriptableObject playerScriptableObject)
        {
            this.playerScriptableObject = playerScriptableObject;
            projectilePool = new ProjectilePool(playerScriptableObject.ProjectilePrefab, playerScriptableObject.ProjectileScriptableObjects);
            InitializeVariables();
        }

        private void InitializeVariables()
        {
            health = playerScriptableObject.Health;
            Money = playerScriptableObject.Money;
            GameService.Instance.UIService.UpdateHealthUI(health);
            GameService.Instance.UIService.UpdateMoneyUI(Money);
            activeMonkeys = new List<MonkeyController>();
        }

        public void Update()
        {
            if (activeMonkeys.Count > 0)
            {
                foreach(MonkeyController controller in activeMonkeys)
                {
                    controller.UpdateMonkey();
                }
            }
            if(Input.GetMouseButtonDown(0))
            {
                TrySelectingMonkey();
            }
        }

        private void TrySelectingMonkey()
        {
            RaycastHit2D[] hits = GetRaycastHitsAtMousePoition();

            foreach (RaycastHit2D hit in hits)
            {
                if(IsMonkeyCollider(hit.collider))
                {
                    SetSelectedMonkeyView(hit.collider.GetComponent<MonkeyView>());
                    return;
                }
            }

            selectedMonkeyView?.MakeRangeVisible(false);
        }

        private RaycastHit2D[] GetRaycastHitsAtMousePoition()
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return Physics2D.RaycastAll(mousePosition, Vector2.zero);
        }

        private bool IsMonkeyCollider(Collider2D collider) => collider != null && !collider.isTrigger && collider.GetComponent<MonkeyView>() != null;

        private void SetSelectedMonkeyView(MonkeyView monkeyViewToBeSelected)
        {
            selectedMonkeyView?.MakeRangeVisible(false);
            selectedMonkeyView = monkeyViewToBeSelected;
            selectedMonkeyView.MakeRangeVisible(true);
        }

        public void ValidateSpawnPosition(int monkeyCost, Vector3 dropPosition)
        {
            if (monkeyCost > Money)
                return;

            GameService.Instance.mapService.ValidateSpawnPosition(dropPosition);
        }

        public void TrySpawningMonkey(MonkeyType monkeyType, int monkeyCost, Vector3 dropPosition)
        {
            if (monkeyCost > Money)
                return;

            if (GameService.Instance.mapService.TryGetMonkeySpawnPosition(dropPosition, out Vector3 spawnPosition))
            {
                SpawnMonkey(monkeyType, spawnPosition);
                GameService.Instance.soundService.PlaySoundEffects(SoundType.SpawnMonkey);
            }
        }

        public void SpawnMonkey(MonkeyType monkeyType, Vector3 spawnPosition)
        {
            MonkeyScriptableObject monkeyScriptableObject = GetMonkeyScriptableObjectByType(monkeyType);
            MonkeyController monkey = new MonkeyController(monkeyScriptableObject, projectilePool);
            monkey.SetPosition(spawnPosition);
            activeMonkeys.Add(monkey);

            DeductMoney(monkeyScriptableObject.Cost);
        }

        private MonkeyScriptableObject GetMonkeyScriptableObjectByType(MonkeyType monkeyType) => playerScriptableObject.MonkeyScriptableObjects.Find(so => so.Type == monkeyType);

        public void ReturnProjectileToPool(ProjectileController projectileToReturn) => projectilePool.ReturnItem(projectileToReturn);
        
        public void TakeDamage(int damageToTake)
        {
            int reducedHealth = health - damageToTake;
            health = reducedHealth <= 0 ? 0 : health - damageToTake;

            GameService.Instance.UIService.UpdateHealthUI(health);
            if(health <= 0)
                PlayerDeath();
        }

        private void DeductMoney(int moneyToDedecut)
        {
            Money -= moneyToDedecut;
            GameService.Instance.UIService.UpdateMoneyUI(Money);
        }

        public void GetReward(int reward)
        {
            Money += reward;
            GameService.Instance.UIService.UpdateMoneyUI(Money);
        }

        private void PlayerDeath() => GameService.Instance.UIService.UpdateGameEndUI(false);
    }
}