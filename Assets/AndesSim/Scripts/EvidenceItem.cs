using UnityEngine;

namespace AndesSim
{
    [DisallowMultipleComponent]
    public sealed class EvidenceItem : MonoBehaviour
    {
        public string evidenceId;

        public bool Collect(GameManager game) => game != null && game.RegisterEvidence(evidenceId);
    }
}
