using System.Collections;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class RoomEntering : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Act());
        }

        private IEnumerator Act()
        {
            var stage = Game.instance.stage;
            var objectPool = stage.objectPool;
            
            Widget.Find<LoadingScreen>().Show();

            stage.stageId = 0;
            stage.LoadBackground("room");

            var clearPlayers = stage.GetComponentsInChildren<Character.Player>();
            foreach (var clearPlayer in clearPlayers)
            {
                clearPlayer.DisableHUD();
                objectPool.Remove<Character.Player>(clearPlayer.gameObject);
            }
            objectPool.ReleaseAll();

            var boss = stage.GetComponentInChildren<Character.Boss.BossBase>();
            if (boss != null)
            {
                Destroy(boss.gameObject);
            }

            var playerFactory = Game.instance.stage.playerFactory;
            GameObject player = playerFactory.Create(States.Instance.CurrentAvatarState.Value);
            player.transform.position = stage.roomPosition - new Vector2(3.0f, 0.0f);
            var playerComp = player.GetComponent<Character.Player>();
            playerComp.StartRun();

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);

            ActionCamera.instance.SetPoint(0f, 0f);
            ActionCamera.instance.Idle();

            yield return new WaitForSeconds(1.0f);
            Widget.Find<LoadingScreen>()?.Close();

            if (player)
                while (player.transform.position.x < stage.roomPosition.x)
                {
                    yield return null;
                }
            playerComp.RunSpeed = 0.0f;
            playerComp.Animator.Idle();

            var dialog = Widget.Find<Dialog>();
            dialog.Show(1);
            if (!(stage.AvatarState is null))
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(stage.AvatarState);
            }

            Destroy(this);
        }
    }
}
