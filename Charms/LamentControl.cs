﻿using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    internal class LamentControl : MonoBehaviour
    {
        public List<GameObject> markedEnemies = new List<GameObject>();
        private PlayMakerFSM _spellControl;
        private PlayerData _pd = PlayerData.instance;
        private HeroController _hc = HeroController.instance;
        private PlayMakerFSM _pvControl;
        private List<GameObject> _blast = new List<GameObject>();
        private List<GameObject> _line = new List<GameObject>();
        private List<GameObject> _focusLines = new List<GameObject>();
        private void OnEnable()
        {
            On.HealthManager.TakeDamage += ApplyStatus;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ClearList;

            _pvControl = _hc.gameObject.Find("HK Prime(Clone)(Clone)").LocateMyFSM("Control");

            _spellControl = HeroController.instance.spellControl;
            if (_spellControl != null)
            {
                _spellControl.InsertMethod("Cancel All", 14, BlastControlCancel);
                _spellControl.InsertMethod("Focus Cancel", 14, BlastControlCancel);
                _spellControl.InsertMethod("Focus Cancel 2", 17, BlastControlCancel);

                _spellControl.InsertMethod("Focus", 14, BlastControlFadeIn);
                _spellControl.InsertMethod("Start MP Drain", 1, BlastControlFadeIn);
               
                _spellControl.InsertMethod("Focus Heal", 15, BlastControlMain);
                _spellControl.InsertMethod("Focus Heal 2", 17, BlastControlMain);
            }
        }
        private void BlastControlCancel()
        {
            foreach (GameObject enemy in markedEnemies)
            {
                Log("Called CancelBlast");
                var index = markedEnemies.IndexOf(enemy);
               try
                {
                    Log("Attempting to stop focus lines");
                    _focusLines[index].GetComponent<tk2dSpriteAnimator>().Stop();
                    Log("An animation was playing and was stopped");
                    Destroy(_focusLines[index]);
                    Log("The object was deleted");
                    _focusLines.RemoveAt(index);
                    Log($"The index number {index} was cleared");
                }
                catch(ArgumentOutOfRangeException e) { }
                try
                {
                    markedEnemies[index].GetComponent<Afflicted>().SoulEffect.SetActive(true);
                    markedEnemies[index].GetComponent<Afflicted>().SoulEffect.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
                    Log("Reactivated Soul Effeect");
                }
                catch(ArgumentOutOfRangeException e) { Log("Exception caught in soul effect"); }
                try
                {
                    Destroy(_line[index]);
                    _line.RemoveAt(index);
                    Log("Removed line");
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Log("Exception caught in line"); }
                try
                {
                    Destroy(_blast[index]);
                    _blast.RemoveAt(index);
                    Log("Removed blast");
                }
                catch (ArgumentOutOfRangeException e) { Log("Exception caught in blast"); }
                //_hc.gameObject.GetComponent<LamentControl>()._audio.Stop();
                Log("Removed Blast Object");

            }

        }
        private void BlastControlFadeIn ()
        {
            foreach (GameObject enemy in markedEnemies)
            {
                foreach (GameObject compare in markedEnemies)
                {
                    if (markedEnemies.IndexOf(compare) == markedEnemies.IndexOf(enemy)) continue;
                    if (compare != enemy) continue;

                    Log("Removed Duplicate Object");
                    Log($"{compare} was in the list twice");
                    markedEnemies.Remove(compare);

                }
                Log("Start coroutine: FadeIn");
                Log("Enemy index: " + markedEnemies.IndexOf(enemy));
                if (enemy == null)
                {
                    markedEnemies.RemoveAt(markedEnemies.IndexOf(enemy));
                    Log("Removed null entity");
                    continue;
                }
                GameManager.instance.StartCoroutine(PureVesselBlastFadeIn(enemy));
            }
        }
        private void BlastControlMain()
        {
            foreach (GameObject enemy in markedEnemies)
            {
                Log("Start Coroutine: Blast");
                var index = markedEnemies.IndexOf(enemy);
                if (enemy == null)
                {
                    try
                    {
                        if (_line[index] != null) Destroy(_line[index]);
                        if (_blast[index] == null) _line.RemoveAt(index);
                    }
                    catch (ArgumentOutOfRangeException e) { }
                    try
                    {
                        if (_blast[index] != null) Destroy(_blast[index]);
                        if (_blast[index] == null) _blast.RemoveAt(index);
                    }
                    catch (ArgumentOutOfRangeException e) { }
                    markedEnemies.RemoveAt(index);
                    Log("Removed null entity");
                    continue;
                }
                GameManager.instance.StartCoroutine(PureVesselBlast(enemy));
            }

        }
        private void OnDisable()
        {
            On.HealthManager.TakeDamage -= ApplyStatus;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ClearList;

            if (_spellControl != null)
            {

                _spellControl.RemoveAction("Cancel All", 14);
                _spellControl.RemoveAction("Focus Cancel", 14);
                _spellControl.RemoveAction("Focus Cancel 2", 17);

                _spellControl.RemoveAction("Focus", 14);
                _spellControl.RemoveAction("Start MP Drain", 1);

                _spellControl.RemoveAction("Focus Heal", 15);
                _spellControl.RemoveAction("Focus Heal 2", 17);
            }

        }

        private void ClearList(Scene PrevScene, Scene NextScene)
        {
            markedEnemies.Clear();
            _focusLines.Clear();
            _line.Clear();
            _blast.Clear();
            foreach (GameObject go in markedEnemies)
            {
                Log($"This should be empty but {go} is still there");
            }
            Log("Cleared list");
        }

        private void ApplyStatus(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if (hitInstance.AttackType == AttackTypes.Nail)
            {
                if (self.gameObject.GetComponent<Afflicted>() == null)
                {

                    self.gameObject.AddComponent<Afflicted>();
                    markedEnemies.Add(self.gameObject);
                    foreach (GameObject go in markedEnemies)
                    {
                        Log(go + " in list");
                    }
                }
            }
        }
        private IEnumerator PureVesselBlastFadeIn(GameObject enemy)
        {
            Log("Called PureVesselBlastFadeIn");
            Log("Recieved GO: " + enemy);
            var index = markedEnemies.IndexOf(enemy);
            Log("Blast Index: " + index);

            GameManager.instance.StartCoroutine(markedEnemies[index].GetComponent<Afflicted>().FadeOut());

            CreateLine(index, enemy);
            _focusLines.Add(Instantiate(_hc.gameObject.Find("Focus Effects").Find("Lines Anim"), enemy.transform.position, new Quaternion(0,0,0,0)));
            _focusLines[index].GetComponent<tk2dSpriteAnimator>().Play("Focus Effect");
                
            this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Charge", 2).audioClip.Value, 1.2f, 1.5f);
            var blast = Instantiate(FiveKnights.preloadedGO["Blast"]);
            _blast.Add(blast);
            _blast[index].transform.position += markedEnemies[index].transform.position;
            _blast[index].SetActive(true);
            Destroy(_blast[index].FindGameObjectInChildren("hero_damager"));

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                _blast[index].transform.localScale *= 2.5f;
            }
            else
            {
                _blast[index].transform.localScale *= 1.5f;
            }

            Animator anim = _blast[index].GetComponent<Animator>();
            anim.speed = 1;
            if (_pd.GetBool("equippedCharm_" + Charms.QuickFocus))
            {
                anim.speed *= 1.5f;
            }

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                anim.speed -= anim.speed * 0.35f;
            }
            yield return null;
            Log("Fade in finished");
        }
        private void CreateLine(int index, GameObject enemy)
        {
            var enemypos = enemy.transform.position;
            var heropos = gameObject.transform.position - new Vector3(0, 1, 0);

            var linepos = Vector3.Lerp(heropos, enemypos, .5f);     
            
            float num = heropos.y - enemypos.y;
            float num2 = heropos.x - enemypos.x;
            float lineangle;
            for (lineangle = Mathf.Atan2(num, num2) * (180f / (float)Math.PI); lineangle < 0f; lineangle += 360f)
            {
            }
            Log(lineangle);
            var linesize = Vector2.Distance(heropos, enemypos);

            _line.Insert(index, Instantiate(FiveKnights.preloadedGO["SoulTwister"].LocateMyFSM("Mage").GetAction<CreateObject>("Tele Line").gameObject.Value, linepos, new Quaternion(0, 0, 0, 0)));
            _line[index].transform.SetRotationZ(lineangle);
            _line[index].transform.localScale = new Vector3(linesize, 1, 1);
            _line[index].GetComponent<ParticleSystem>().loop = true;
            _line[index].GetComponent<ParticleSystem>().startSize = .25f;
            _line[index].GetComponent<ParticleSystem>().Emit(0);
            _line[index].SetActive(true);
            _line[index].GetComponent<ParticleSystem>().Play();
        }

        private IEnumerator PureVesselBlast(GameObject enemy)
        {
            Log("Called PureVesselBlast");
            Log("Recieved GO: " + enemy);
            var index = markedEnemies.IndexOf(enemy);
            _line[index].GetComponent<ParticleSystem>().loop = false;
            _focusLines[index].GetComponent<tk2dSpriteAnimator>().Play("Focus Effect End");
            _blast[index].layer = 17;
            Animator anim = _blast[index].GetComponent<Animator>();
            anim.speed = 1;
            int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            anim.PlayInFixedTime(hash, -1, 0.8f);

            Log("Adding CircleCollider2D");            
            CircleCollider2D blastCollider = _blast[index].AddComponent<CircleCollider2D>();
            blastCollider.radius = 2.5f;
            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                blastCollider.radius *= 2.5f;
            }
            else
            {
                blastCollider.radius *= 1.5f;
            }

            blastCollider.offset = Vector3.down;
            blastCollider.isTrigger = true;
            Log("Adding DebugColliders");
            //_blast.AddComponent<DebugColliders>();
            Log("Adding DamageEnemies");
            _blast[index].AddComponent<DamageEnemies>();
            DamageEnemies damageEnemies = _blast[index].GetComponent<DamageEnemies>();
            damageEnemies.damageDealt = 30;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.ignoreInvuln = false;
            damageEnemies.enabled = true;
            Log("Playing AudioClip");
            this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Burst", 8).audioClip.Value, 1.5f, 1.5f);
            Log("Audio Clip finished");
            yield return new WaitForSeconds(0.11f);
            index = markedEnemies.IndexOf(enemy);
            Destroy(markedEnemies[index].GetComponent<Afflicted>());
            Destroy(_blast[index]);
            _blast.RemoveAt(index);
            Destroy(_focusLines[index]);
            _focusLines.RemoveAt(index);
            markedEnemies.RemoveAt(index);  

            Log("Blast Finished");
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][LamentControl] " + message);
    }

    internal class Afflicted : MonoBehaviour
    {
        public GameObject SoulEffect;
        private void Start()
        {
            SoulEffect = Instantiate(FiveKnights.preloadedGO["SoulEffect"], gameObject.transform);
            SoulEffect.transform.localPosition = new Vector3(0, 0, -0.0001f);
            SoulEffect.transform.localScale = new Vector3(.75f, .75f, .75f);
            SoulEffect.SetActive(true);
        }
        public IEnumerator FadeOut()
        {
            while (SoulEffect.GetComponent<SpriteRenderer>().color.a > 0)
            {
                yield return new WaitForSeconds(.01f);
                SoulEffect.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, SoulEffect.GetComponent<SpriteRenderer>().color.a - .1f);
            }
            SoulEffect.SetActive(false);
        }
        private void OnDisable()
        {
            Destroy(SoulEffect);
        }
        private void Log(object message) => Modding.Logger.Log("[FiveKnights][LamentControl] " + message);
    }

}
