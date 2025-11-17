using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using MelonLoader;
using RumbleModdingAPI;
using System.Collections;
using UnityEngine;

namespace GamblingMod
{

    [RegisterTypeInIl2Cpp]
    public class InteractionLever : MonoBehaviour
    {
        private bool leftHandIn = false;
        private bool rightHandIn = false;
        private bool handleActive = false;
        public Action LeverPulled, OnLeverReleased, OnLeverReleasedComplete;
        public object leverReleaseCoroutine = null;
        public GameObject lastInteractedHand;
        public Player lastInteractedPlayer;

        void OnTriggerEnter(Collider other)
        {
            //checked if it's a hand
            if ((other.gameObject.name != "Bone_HandAlpha_L") && (other.gameObject.name != "Bone_HandAlpha_R")) { return; }
            if (other.gameObject.name == "Bone_HandAlpha_L") { leftHandIn = true; }
            if (other.gameObject.name == "Bone_HandAlpha_R") { rightHandIn = true; }
        }

        void OnTriggerExit(Collider other)
        {
            //checked if it's a hand
            if ((other.gameObject.name != "Bone_HandAlpha_L") && (other.gameObject.name != "Bone_HandAlpha_R")) { return; }
            if (other.gameObject.name == "Bone_HandAlpha_L") { leftHandIn = false; }
            if (other.gameObject.name == "Bone_HandAlpha_R") { rightHandIn = false; }
        }

        void OnTriggerStay(Collider other)
        {
            //checked if it's a hand and grip is held
            if ((other.gameObject.name != "Bone_HandAlpha_L") && (other.gameObject.name != "Bone_HandAlpha_R")) { return; }
            lastInteractedHand = other.gameObject;
            lastInteractedPlayer = lastInteractedHand.transform.parent.parent.parent.parent.parent.parent.parent.parent.parent.GetComponent<PlayerController>().AssignedPlayer;
            if (leftHandIn)
            {
                if ((!handleActive) && (lastInteractedHand.transform.GetChild(3).GetChild(0).rotation.eulerAngles.x >= 45f))
                {
                    handleActive = true;
                    MelonCoroutines.Start(PullLever());
                }
            }
            else if (rightHandIn)
            {
                if ((!handleActive) && (lastInteractedHand.transform.GetChild(3).GetChild(0).rotation.eulerAngles.x >= 45f))
                {
                    handleActive = true;
                    MelonCoroutines.Start(PullLever());
                }
            }
        }

        private IEnumerator PullLever()
        {
            float fingerRotation = lastInteractedHand.transform.GetChild(3).GetChild(0).localRotation.eulerAngles.x;
            while ((leverReleaseCoroutine == null) && (fingerRotation >= 45f))
            {
                try
                {
                    fingerRotation = lastInteractedHand.transform.GetChild(3).GetChild(0).localRotation.eulerAngles.x;
                    RotateLever();
                }
                catch { break; }
                yield return new WaitForFixedUpdate();
            }
            if (leverReleaseCoroutine == null)
            {
                leverReleaseCoroutine = MelonCoroutines.Start(ReleaseLever());
            }
            yield break;
        }

        private void RotateLever()
        {
            Transform parent = this.transform.parent;
            parent.LookAt(lastInteractedHand.transform.position);
            parent.localRotation = Quaternion.Euler(parent.localRotation.eulerAngles.x, 0f, 0f);
            if (parent.localRotation.eulerAngles.x < 295f)
            {
                parent.localRotation = Quaternion.Euler(295f, 0f, 0f);
            }
            else if (parent.localRotation.eulerAngles.x >= 340f)
            {
                parent.localRotation = Quaternion.Euler(340f, 0f, 0f);
                leverReleaseCoroutine = MelonCoroutines.Start(ReleaseLever());
                Delegate[] listeners = LeverPulled?.GetInvocationList();
                if (listeners != null)
                {
                    foreach (Delegate listener in listeners)
                    {
                        try
                        {
                            // Invoke each listener individually
                            listener.DynamicInvoke();
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg("RotateLever Error for LeverPulled: " + listener.Target);
                            MelonLogger.Error(e.InnerException);
                            // The loop continues to the next listener even if one fails
                        }
                    }
                }
            }
        }

        private IEnumerator ReleaseLever()
        {
            Delegate[] listeners = OnLeverReleased?.GetInvocationList();
            if (listeners != null)
            {
                foreach (Delegate listener in listeners)
                {
                    try
                    {
                        // Invoke each listener individually
                        listener.DynamicInvoke();
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg("ReleaseLever Error for OnLeverReleased: " + listener.Target);
                        MelonLogger.Error(e.InnerException);
                        // The loop continues to the next listener even if one fails
                    }
                }
            }
            Transform parent = this.transform.parent;
            int currentAngle = (int)(parent.transform.localRotation.eulerAngles.x);
            if (currentAngle < 295) { currentAngle += 360; }
            int rotationPerTick = 1;
            while (currentAngle != 295)
            {
                yield return new WaitForFixedUpdate();
                currentAngle -= rotationPerTick;
                parent.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
            }
            handleActive = false;
            Delegate[] listeners2 = OnLeverReleasedComplete?.GetInvocationList();
            if (listeners2 != null)
            {
                foreach (Delegate listener in listeners2)
                {
                    try
                    {
                        // Invoke each listener individually
                        listener.DynamicInvoke();
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg("ReleaseLever Error for OnLeverReleasedComplete: " + listener.Target);
                        MelonLogger.Error(e.InnerException);
                        // The loop continues to the next listener even if one fails
                    }
                }
            }
            leverReleaseCoroutine = null;
            yield break;
        }
    }
}
