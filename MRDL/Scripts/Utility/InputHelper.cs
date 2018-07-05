using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRDL.Utility
{

    public static class InputHelper
    {
        public static Vector3 GetHandPos(IInputSource currentInputSource, uint currentInputSourceId)
        {

#if UNITY_2017_2_OR_NEWER
            Vector3 pos = Vector3.zero;
            InteractionSourceInfo sourceKind;
            currentInputSource.TryGetSourceKind(currentInputSourceId, out sourceKind);
            switch (sourceKind)
            {
                case InteractionSourceInfo.Hand:
                    currentInputSource.TryGetGripPosition(currentInputSourceId, out pos);
                    break;
                case InteractionSourceInfo.Controller:
                    currentInputSource.TryGetPointerPosition(currentInputSourceId, out pos);
                    break;
            }
#else
            currentInputSource.TryGetPointerPosition(currentInputSourceId, out pos);
#endif
            return pos;
        }


        public static bool PassedHandThreshold(Vector3 posA, Vector3 posB, out float distance, float margin = 0.0001f)
        {
            distance = Vector3.Distance(posA, posB);

            if ( distance >= margin)
            {
                return true;
            }

            return false;
        }


    }
}