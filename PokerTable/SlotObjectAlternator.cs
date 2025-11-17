
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace GamblingMod
{

    public class SlotObjectAlternator : MonoBehaviour
    {
        GameObject gemsParent;
        object gemsRotateCoroutine;

        public SlotObjectAlternator(GameObject gemsParent)
        {
            this.gemsParent = gemsParent;
            gemsRotateCoroutine = MelonCoroutines.Start(Run());
        }

        void OnDestroy()
        {
            MelonCoroutines.Stop(gemsRotateCoroutine);
        }

        private IEnumerator Run()
        {
            //do loop
            int spot = -1;
            while (gemsParent != null)
            {
                spot++;
                int childCount = gemsParent.transform.childCount;
                if (spot == childCount) { spot = 0; }
                for (int i = 0; i < childCount; i++)
                {
                    gemsParent.transform.GetChild(i).gameObject.SetActive(i == spot);
                }
                yield return new WaitForSeconds(3f);
            }
            yield break;
        }
    }
}
