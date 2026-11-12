using UnityEngine;

namespace AndesSim
{
    /// <summary>Stage-one inspection or discrete navigation destination.</summary>
    [DisallowMultipleComponent]
    public sealed class ExplorationTarget : MonoBehaviour
    {
        public enum Kind { Inspection, Teleport, Portal }

        public Kind kind;
        public string uniqueId;
        public string stationId;
        public string title;
        [TextArea(3, 12)] public string body;
        public Transform destination;
        public Transform aimPoint;
        public Renderer[] highlightRenderers;

        MaterialPropertyBlock[] originalBlocks;
        MaterialPropertyBlock highlightBlock;
        bool highlighted;

        public Vector3 AimPosition => aimPoint != null ? aimPoint.position : transform.position;
        public bool IsHighlighted => highlighted;

        public void SetHighlighted(bool value)
        {
            if (highlighted == value) return;
            highlighted = value;
            if (highlightRenderers == null) return;
            if (originalBlocks == null || originalBlocks.Length != highlightRenderers.Length)
            {
                originalBlocks = new MaterialPropertyBlock[highlightRenderers.Length];
                for (int i = 0; i < highlightRenderers.Length; i++)
                {
                    originalBlocks[i] = new MaterialPropertyBlock();
                    if (highlightRenderers[i] != null)
                        highlightRenderers[i].GetPropertyBlock(originalBlocks[i]);
                }
                highlightBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                Renderer item = highlightRenderers[i];
                if (item == null) continue;
                if (value)
                {
                    item.GetPropertyBlock(highlightBlock);
                    highlightBlock.SetColor("_BaseColor", new Color(0.18f, 0.72f, 0.57f));
                    highlightBlock.SetColor("_EmissionColor", new Color(0.08f, 0.65f, 0.42f));
                    item.SetPropertyBlock(highlightBlock);
                }
                else item.SetPropertyBlock(originalBlocks[i]);
            }
        }

        void OnDisable() => SetHighlighted(false);
    }
}
