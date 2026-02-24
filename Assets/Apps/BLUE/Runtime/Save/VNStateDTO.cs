using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNStateDTO
    {
        public string scriptId;
        public int pointer;

        // JsonUtility는 Dictionary 직렬화 안 됨 → 리스트로 우회
        public List<IntVarDTO> intVars = new List<IntVarDTO>();

        // (선택) 드링크 결과 카운트
        public int greatCount;
        public int successCount;
        public int failCount;
        public string lastResult;
    }

    [Serializable]
    public sealed class IntVarDTO
    {
        public string key;
        public int value;
    }
}