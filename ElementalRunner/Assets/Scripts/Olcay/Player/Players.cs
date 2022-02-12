using System;
using System.Collections;
using Olcay.Animations;
using Olcay.Managers;
using Simla;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Olcay.Player
{
    public class Players : MonoBehaviour
    {
        [SerializeField] private GameObject boyPrefab;
        [SerializeField] private GameObject girlPrefab;

        private float timer = 0f;
        private readonly float instantiateCD = 0f;


        private GameObject girlPlayer;
        private GameObject boyPlayer;
        private bool isGirlActive;
        [SerializeField] private bool isFinish = false;
        [SerializeField] private float startScale;
        private bool isGameStart;
        public static event Action<bool> playerChanged; //Observer
        public static event Action playerCollisionWithFinish;
        public static event Action playerCollisionWithLevelFinish;
        public static event Action calculateFinishScore;

        public static event Action levelFailed;
        //public static event Action<bool,Vector3> playerSetUp;


        private void Awake()
        {
            girlPlayer = Instantiate(girlPrefab, transform.position, transform.rotation);
            girlPlayer.transform.parent = this.gameObject.transform;
            //isGirlActive = true;
            

            boyPlayer = Instantiate(boyPrefab, transform.position, transform.rotation);
            boyPlayer.transform.parent = this.gameObject.transform;
            //boyPlayer.SetActive(false);
            isGirlActive = Random.value<0.5f;
            if (isGirlActive)
            {
                boyPlayer.SetActive(false);
            }
            else
            {
                girlPlayer.SetActive(false);
            }
            
            playerChanged?.Invoke(isGirlActive);
            
            //SwapCurrentPlayer(isGirlActive);
            //startScale = gameObject.transform.localScale.x;

            PlayerMovement.gameStarting += ChangeGameStartState;
        }

        private void Update()
        {
            GenerateStairs();
        }

        private void OnDestroy()
        {
            PlayerMovement.gameStarting -= ChangeGameStartState;
            StopAllCoroutines();
        }


        #region StairsGenerateAndSetActiveFalse

        private void GenerateStairs()
        {
            if (Input.GetMouseButton(0) && isGameStart && !isFinish && !Extentions.IsOverUi())
            {
                timer += Time.deltaTime;

                if (timer >= instantiateCD)
                {
                    var pos = transform.position;
                    if (isGirlActive)
                    {
                        GameObject stair = SpawnManager.Instance.SpawnStair("WaterStairs",
                            new Vector3(pos.x, pos.y + 0.01f, pos.z),
                            Quaternion.identity);
                        StartCoroutine(SetActiveFalseRoutine(stair));
                    }
                    else if (!isGirlActive)
                    {
                        GameObject stair = SpawnManager.Instance.SpawnStair("FireStairs",
                            new Vector3(pos.x, pos.y + 0.01f, pos.z),
                            Quaternion.identity);
                        StartCoroutine(SetActiveFalseRoutine(stair));
                    }

                    transform.localScale -=
                        new Vector3(0.05f, 0.05f,
                            0.05f); //every stair will decrease players scale 0.05 or we can change this value with Gamesettings
                    if (gameObject.transform.localScale.x < 1f) //fail olmasın zıplayamasın.
                    {
                        FailDetection();
                    }

                    timer -= 0.2f;
                }
            }
        }

        private IEnumerator SetActiveFalseRoutine(GameObject stair)
        {
            yield return new WaitForSeconds(2f);
            stair.transform.position = Vector3.zero;
            stair.SetActive(false);
        }

        #endregion

        #region PlayerSwapWithGateInTrigger

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Gate"))
            {
                SwapCurrentPlayer(isGirlActive);
            }
        }

        private void SwapCurrentPlayer(bool isGirlActive)
        {
            if (isGirlActive)
            {
                boyPlayer.SetActive(true);
                boyPlayer.transform.position = transform.position;
                boyPlayer.transform.parent = gameObject.transform;

                girlPlayer.transform.position = Vector3.zero;
                girlPlayer.SetActive(false);
                isGirlActive = false;
                playerChanged?.Invoke(isGirlActive);
            }
            else
            {
                girlPlayer.SetActive(true);
                girlPlayer.transform.position = transform.position;
                girlPlayer.transform.parent = gameObject.transform;

                boyPlayer.transform.position = Vector3.zero;
                boyPlayer.SetActive(false);
                isGirlActive = true;
                playerChanged?.Invoke(isGirlActive);
            }
        }

        #endregion

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Finish"))
            {
                isFinish = true;
                playerCollisionWithFinish?.Invoke();
                //var pos = transform.position;
                //collisionWithFinish?.Invoke();
                //tap işlemi yapmamız lazım.  -> bunu araştırmamız gerekiyor.
                InvokeRepeating(nameof(ThrowABallRoutine), 1f, 1f);
            }
            else if (other.gameObject.CompareTag("LevelFinish"))
            {
                playerCollisionWithLevelFinish?.Invoke();
                CancelInvoke(nameof(ThrowABallRoutine));
                AnimationController.Instance.ChangeAnimationState(State.Dance);
                calculateFinishScore?.Invoke();
            }
        }
        private void FailDetection()
        {
            isFinish = true;
            levelFailed?.Invoke();
            GameManager.Instance.Failed(); //its will be change with UI Manager.
        }

        private void ThrowABallRoutine()
        {
            if (isGirlActive)
            {
                AnimationController.Instance.ChangeAnimationState(State.Throw);
                var pos = transform.position;
                var localScale = transform.localScale;
                var posY = localScale.y / 2f;
                SpawnManager.Instance.SpawnBall("WaterBalls",
                    new Vector3(pos.x, posY, pos.z + 0.1f),
                    Quaternion.identity);
                localScale -= new Vector3(1f, 1f, 1f);
                transform.localScale = localScale;
            }
            else
            {
                AnimationController.Instance.ChangeAnimationState(State.Throw);
                var pos = transform.position;
                var localScale = transform.localScale;
                var posY = localScale.y / 2f;
                SpawnManager.Instance.SpawnBall("FireBalls",
                    new Vector3(pos.x, posY, pos.z + 0.1f),
                    Quaternion.identity);
                localScale -= new Vector3(1f, 1f, 1f);
                transform.localScale = localScale;
            }

            if (gameObject.transform.localScale.x <= 1f && isFinish)
            {
                CancelInvoke();
                //game finish
                // //o anki basamağın üstündeki colliderdan alırız x kaç olduğunu
            }
        }

        

        private void ChangeGameStartState()
        {
            isGameStart = true;
        }
    }
}