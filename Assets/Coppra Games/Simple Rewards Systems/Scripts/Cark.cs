using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;

namespace EasyUI.CarkUI
{
    public class Cark : MonoBehaviour
    {
        [Header("References :")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private Transform linesParent;

        [Space]
        [SerializeField] private Transform CarkTransform;
        [SerializeField] private Transform wheelCircle;
        [SerializeField] private GameObject wheelPiecePrefab;
        [SerializeField] private Transform wheelPiecesParent;

        [Space]
        [Header("İndicator Ayarları :")]
        [SerializeField] private Transform indicatorTransform;
        [SerializeField] [Range(0f, 360f)] private float indicatorAngle = 0f;
        [SerializeField] private bool showIndicatorInSceneView = true;

        [Space]
        [Header("Sesler :")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tickAudioClip;
        [SerializeField] [Range(0f, 1f)] private float volume = .5f;
        [SerializeField] [Range(-3f, 3f)] private float pitch = 1f;

        [Space]
        [Header("Çark ayarlari :")]
        [Range(1, 20)] public int spinDuration = 8;
        [SerializeField] [Range(.2f, 2f)] private float wheelSize = 1f;

        [Space]
        [Header("Çark parcalari :")]
        public CarkParca[] carkParcalari;

        // 2x özelliği artık private ama setter ile kontrol edilecek
        private bool doubleReward = false;
        public bool DoubleReward => doubleReward;
        public void SetDoubleReward(bool value) => doubleReward = value;

        private UnityAction onSpinStartEvent;
        private UnityAction<CarkParca> onSpinEndEvent;

        private Tween spinTween;
        private bool _isSpinning = false;

        // Skip / finish mantığı için saklananlar
        private CarkParca currentPiece;
        private float finalTargetAngleDeg;

        public bool IsSpinning => _isSpinning;

        private Vector2 pieceMinSize = new Vector2(81f, 146f);
        private Vector2 pieceMaxSize = new Vector2(144f, 213f);
        private int piecesMin = 2;
        private int piecesMax = 12;

        private float pieceAngle;
        private float halfPieceAngle;
        private float halfPieceAngleWithPaddings;

        private double accumulatedWeight;
        private System.Random rand = new System.Random();
        private List<int> nonZeroChancesIndices = new List<int>();

        private void Start()
        {
            pieceAngle = 360f / carkParcalari.Length;
            halfPieceAngle = pieceAngle / 2f;
            halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle / 4f);

            Generate();
            CalculateWeightsAndIndices();
            if (nonZeroChancesIndices.Count == 0)
                Debug.LogError("Bütün olasılıklar sıfır olamaz");

            SetupAudio();
            UpdateIndicatorPosition();
        }

        private void SetupAudio()
        {
            if (audioSource != null)
            {
                audioSource.clip = tickAudioClip;
                audioSource.volume = volume;
                audioSource.pitch = pitch;
            }
        }

        private void Generate()
        {
            wheelPiecePrefab = InstantiatePiece();
            RectTransform rt = wheelPiecePrefab.transform.GetChild(0).GetComponent<RectTransform>();
            float pieceWidth = Mathf.Lerp(pieceMinSize.x, pieceMaxSize.x, 1f - Mathf.InverseLerp(piecesMin, piecesMax, carkParcalari.Length));
            float pieceHeight = Mathf.Lerp(pieceMinSize.y, pieceMaxSize.y, 1f - Mathf.InverseLerp(piecesMin, piecesMax, carkParcalari.Length));
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pieceWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pieceHeight);

            for (int i = 0; i < carkParcalari.Length; i++)
                DrawPiece(i);

            Destroy(wheelPiecePrefab);
        }

        private void DrawPiece(int index)
        {
            CarkParca piece = carkParcalari[index];
            Transform pieceTrns = InstantiatePiece().transform.GetChild(0);

            pieceTrns.GetChild(0).GetComponent<Image>().sprite = piece.Icon;
            pieceTrns.GetChild(1).GetComponent<Text>().text = piece.Label;
            pieceTrns.GetChild(2).GetComponent<Text>().text = piece.Amount.ToString();

            Transform lineTrns = Instantiate(linePrefab, linesParent.position, Quaternion.identity, linesParent).transform;
            lineTrns.RotateAround(wheelPiecesParent.position, Vector3.back, (pieceAngle * index) + halfPieceAngle);
            pieceTrns.RotateAround(wheelPiecesParent.position, Vector3.back, pieceAngle * index);
        }

        private GameObject InstantiatePiece() => Instantiate(wheelPiecePrefab, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent);

        public void Spin()
        {
            if (_isSpinning) return;

            _isSpinning = true;
            onSpinStartEvent?.Invoke();

            int index = GetRandomPieceIndex();
            currentPiece = carkParcalari[index];

            if (currentPiece.Chance == 0 && nonZeroChancesIndices.Count != 0)
            {
                index = nonZeroChancesIndices[Random.Range(0, nonZeroChancesIndices.Count)];
                currentPiece = carkParcalari[index];
            }

            float targetPieceAngle = -(pieceAngle * index);
            float adjustedTargetAngle = targetPieceAngle + indicatorAngle;

            float rightOffset = (adjustedTargetAngle - halfPieceAngleWithPaddings);
            float leftOffset = (adjustedTargetAngle + halfPieceAngleWithPaddings);

            float randomAngle = Random.Range(leftOffset, rightOffset);
            finalTargetAngleDeg = randomAngle + 2f * 360f * spinDuration;
            Vector3 targetRotation = Vector3.back * finalTargetAngleDeg;

            float prevAngle = wheelCircle.eulerAngles.z;
            float currentAngle = prevAngle;
            bool isIndicatorOnTheLine = false;

            spinTween = wheelCircle
                .DORotate(targetRotation, spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutQuart)
                .OnUpdate(() =>
                {
                    float diff = Mathf.Abs(prevAngle - currentAngle);
                    if (diff >= halfPieceAngle)
                    {
                        if (isIndicatorOnTheLine && audioSource != null && audioSource.clip != null)
                            audioSource.PlayOneShot(audioSource.clip);
                        prevAngle = currentAngle;
                        isIndicatorOnTheLine = !isIndicatorOnTheLine;
                    }
                    currentAngle = wheelCircle.eulerAngles.z;
                })
                .OnComplete(FinishSpin);
        }

        public void SkipSpin()
        {
            if (!_isSpinning || spinTween == null) return;

            if (spinTween.IsActive()) spinTween.Kill();

            float currentZ = wheelCircle.eulerAngles.z;
            float diff = Mathf.Repeat(finalTargetAngleDeg - currentZ, 360f);

            float minDuration = 0.25f;
            float maxDuration = 0.6f;
            float duration = Mathf.Clamp(diff / 360f * 0.5f, minDuration, maxDuration);

            spinTween = wheelCircle
                .DORotate(Vector3.back * finalTargetAngleDeg, duration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishSpin);
        }

        private void FinishSpin()
        {
            _isSpinning = false;

            if (onSpinEndEvent != null)
            {
                CarkParca finalPiece = currentPiece;
                if (doubleReward)
                {
                    finalPiece = new CarkParca()
                    {
                        Icon = currentPiece.Icon,
                        Label = currentPiece.Label,
                        Amount = currentPiece.Amount * 2,
                        Chance = currentPiece.Chance,
                        Index = currentPiece.Index,
                        _weight = currentPiece._weight
                    };
                }
                onSpinEndEvent.Invoke(finalPiece);
            }

            onSpinStartEvent = null;
            onSpinEndEvent = null;
            spinTween = null;
        }

        public void OnSpinStart(UnityAction action) => onSpinStartEvent = action;
        public void OnSpinEnd(UnityAction<CarkParca> action) => onSpinEndEvent = action;

        private int GetRandomPieceIndex()
        {
            double r = rand.NextDouble() * accumulatedWeight;
            for (int i = 0; i < carkParcalari.Length; i++)
                if (carkParcalari[i]._weight >= r) return i;
            return 0;
        }

        private void CalculateWeightsAndIndices()
        {
            for (int i = 0; i < carkParcalari.Length; i++)
            {
                CarkParca piece = carkParcalari[i];
                accumulatedWeight += piece.Chance;
                piece._weight = accumulatedWeight;
                piece.Index = i;

                if (piece.Chance > 0)
                    nonZeroChancesIndices.Add(i);
            }
        }

        private void UpdateIndicatorPosition()
        {
            if (indicatorTransform == null || wheelPiecesParent == null) return;

            float angleRad = indicatorAngle * Mathf.Deg2Rad;
            float radius = Vector3.Distance(indicatorTransform.position, wheelPiecesParent.position);
            if (radius == 0f) radius = 100f;

            Vector3 newPos = wheelPiecesParent.position + new Vector3(
                Mathf.Sin(angleRad) * radius,
                Mathf.Cos(angleRad) * radius,
                0f
            );
            indicatorTransform.position = newPos;

            Vector3 dir = (wheelPiecesParent.position - indicatorTransform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            indicatorTransform.rotation = Quaternion.Euler(0, 0, angle + 90f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showIndicatorInSceneView || wheelPiecesParent == null) return;

            float radiusGizmo = 150f;
            Vector3 indicatorDir = new Vector3(
                Mathf.Sin(indicatorAngle * Mathf.Deg2Rad),
                Mathf.Cos(indicatorAngle * Mathf.Deg2Rad),
                0f
            );

            Vector3 indicatorPos = wheelPiecesParent.position + indicatorDir * radiusGizmo;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(wheelPiecesParent.position, indicatorPos);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(indicatorPos, 10f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            indicatorAngle = indicatorAngle % 360f;
            if (indicatorAngle < 0f) indicatorAngle += 360f;

            if (Application.isPlaying)
                UpdateIndicatorPosition();
        }
#endif
    }
}
