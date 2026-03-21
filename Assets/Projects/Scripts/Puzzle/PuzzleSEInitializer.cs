using System;
using Projects.Scripts.Audio;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    public class PuzzleSeInitializer : MonoBehaviour
    {
        [Header("SoundClip Settings")]
        [Tooltip("ピースがグリッドに配置されたときのサウンドクリップ")]
        [SerializeField] private AudioClip piecePlacedClip;
        
        [Tooltip("ピースをクリックしたときのサウンドクリップ")]
        [SerializeField] private AudioClip pieceClickClip;
        
        [Tooltip("ピースの配置がキャンセルされたときのサウンドクリップ")]
        [SerializeField] private AudioClip pieceCancelClip;
        
        private void Start()
        {
            AudioManager.Register("PiecePlace", piecePlacedClip);
            AudioManager.Register("PieceClick", pieceClickClip);
            AudioManager.Register("PieceCancel", pieceCancelClip);
        }
    }
}