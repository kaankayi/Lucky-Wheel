using UnityEngine ;
using UnityEngine.UI ;
using DG.Tweening ;
using UnityEngine.Events ;
using System.Collections.Generic ;

namespace EasyUI.CarkUI {

   public class Cark : MonoBehaviour {

      [Header ("References :")]
      [SerializeField] private GameObject linePrefab ;
      [SerializeField] private Transform linesParent ;

      [Space]
      [SerializeField] private Transform CarkTransform ;
      [SerializeField] private Transform wheelCircle ;
      [SerializeField] private GameObject wheelPiecePrefab ;
      [SerializeField] private Transform wheelPiecesParent ;

      [Space]
      [Header ("İndicator Ayarları :")]
      [SerializeField] private Transform indicatorTransform ; // Gerçek indicator görseli
      [SerializeField] [Range(0f, 360f)] private float indicatorAngle = 0f ; // İndicator açısı (0 = üstte)
      [SerializeField] private bool showIndicatorInSceneView = true ; // Scene view'da indicator pozisyonunu göster

      [Space]
      [Header ("Sesler :")]
      [SerializeField] private AudioSource audioSource ;
      [SerializeField] private AudioClip tickAudioClip ; 
      [SerializeField] [Range (0f, 1f)] private float volume = .5f ;
      [SerializeField] [Range (-3f, 3f)] private float pitch = 1f ;

      [Space]
      [Header ("Cark ayarlari :")]
      [Range (1, 20)] public int spinDuration = 8 ;
      [SerializeField] [Range (.2f, 2f)] private float wheelSize = 1f ;

      [Space]
      [Header ("Cark parcalari :")]
      public CarkParca[] carkParcalari ;
      [Header("Özel Ayarlar :")]
      [SerializeField] private bool doubleReward = false; // True olursa ödüller 2x
public bool DoubleReward => doubleReward; // sadece okunabilir
      
      private UnityAction onSpinStartEvent ;
      private UnityAction<CarkParca> onSpinEndEvent ;


      private bool _isSpinning = false ;

      public bool IsSpinning { get { return _isSpinning ; } }


      private Vector2 pieceMinSize = new Vector2 (81f, 146f) ;
      private Vector2 pieceMaxSize = new Vector2 (144f, 213f) ;
      private int piecesMin = 2 ;
      private int piecesMax = 12 ;

      private float pieceAngle ;
      private float halfPieceAngle ;
      private float halfPieceAngleWithPaddings ;


      private double accumulatedWeight ;
      private System.Random rand = new System.Random () ;

      private List<int> nonZeroChancesIndices = new List<int> () ;

      private void Start () {
         pieceAngle = 360 / carkParcalari.Length ;
         halfPieceAngle = pieceAngle / 2f ;
         halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle / 4f) ;

         Generate () ;  

         CalculateWeightsAndIndices () ;
         if (nonZeroChancesIndices.Count == 0)
            Debug.LogError ("Bütün olasılıklar sıfır olamaz") ;

         SetupAudio () ;
      }

      private void SetupAudio () {
         audioSource.clip = tickAudioClip ;
         audioSource.volume = volume ;
         audioSource.pitch = pitch ;
      }

      private void Generate () {
         wheelPiecePrefab = InstantiatePiece () ;

         RectTransform rt = wheelPiecePrefab.transform.GetChild (0).GetComponent <RectTransform> () ;
         float pieceWidth = Mathf.Lerp (pieceMinSize.x, pieceMaxSize.x, 1f - Mathf.InverseLerp (piecesMin, piecesMax, carkParcalari.Length)) ;
         float pieceHeight = Mathf.Lerp (pieceMinSize.y, pieceMaxSize.y, 1f - Mathf.InverseLerp (piecesMin, piecesMax, carkParcalari.Length)) ;
         rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, pieceWidth) ;
         rt.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, pieceHeight) ;

         for (int i = 0; i < carkParcalari.Length; i++)
            DrawPiece (i) ;

         Destroy (wheelPiecePrefab) ;
      }

      private void DrawPiece (int index) {
         CarkParca piece = carkParcalari [ index ] ;
         Transform pieceTrns = InstantiatePiece ().transform.GetChild (0) ;

         pieceTrns.GetChild (0).GetComponent <Image> ().sprite = piece.Icon ;
         pieceTrns.GetChild (1).GetComponent <Text> ().text = piece.Label ;
         pieceTrns.GetChild (2).GetComponent <Text> ().text = piece.Amount.ToString () ;

         //Line
         Transform lineTrns = Instantiate (linePrefab, linesParent.position, Quaternion.identity, linesParent).transform ;
         lineTrns.RotateAround (wheelPiecesParent.position, Vector3.back, (pieceAngle * index) + halfPieceAngle) ;

         pieceTrns.RotateAround (wheelPiecesParent.position, Vector3.back, pieceAngle * index) ;
      }

      private GameObject InstantiatePiece () {
         return Instantiate (wheelPiecePrefab, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent) ;
      }

      public void Spin () {
         if (!_isSpinning) {
            _isSpinning = true ;
            if (onSpinStartEvent != null)
               onSpinStartEvent.Invoke () ;

            int index = GetRandomPieceIndex () ;
            CarkParca piece = carkParcalari [ index ] ;

            if (piece.Chance == 0 && nonZeroChancesIndices.Count != 0) {
               index = nonZeroChancesIndices [ Random.Range (0, nonZeroChancesIndices.Count) ] ;
               piece = carkParcalari [ index ] ;
            }

            // İndicator pozisyonuna göre hedef açıyı hesapla
            float targetPieceAngle = -(pieceAngle * index) ;
            float indicatorOffset = indicatorAngle ; // İndicator açısını doğru yönde uygula
            float adjustedTargetAngle = targetPieceAngle + indicatorOffset ;

            float rightOffset = (adjustedTargetAngle - halfPieceAngleWithPaddings) % 360 ;
            float leftOffset = (adjustedTargetAngle + halfPieceAngleWithPaddings) % 360 ;

            float randomAngle = Random.Range (leftOffset, rightOffset) ;

            Vector3 targetRotation = Vector3.back * (randomAngle + 2 * 360 * spinDuration) ;

            float prevAngle, currentAngle ;
            prevAngle = currentAngle = wheelCircle.eulerAngles.z ;

            bool isIndicatorOnTheLine = false ;

            wheelCircle
            .DORotate (targetRotation, spinDuration, RotateMode.FastBeyond360)
            .SetEase (Ease.InOutQuart)
            .OnUpdate (() => {
               float diff = Mathf.Abs (prevAngle - currentAngle) ;
               if (diff >= halfPieceAngle) {
                  if (isIndicatorOnTheLine) {
                     audioSource.PlayOneShot (audioSource.clip) ;
                  }
                  prevAngle = currentAngle ;
                  isIndicatorOnTheLine = !isIndicatorOnTheLine ;
               }
               currentAngle = wheelCircle.eulerAngles.z ;
            })
            .OnComplete (() => {
               _isSpinning = false ;
               
               if (onSpinEndEvent != null)
    {
        // Kazanılan parçayı al
        CarkParca finalPiece = piece;

        // Eğer doubleReward açıksa miktarı 2x yap
        if (doubleReward)
        {
            finalPiece = new CarkParca()
            {
                Icon = piece.Icon,
                Label = piece.Label,
                Amount = piece.Amount * 2,
                Chance = piece.Chance,
                Index = piece.Index,
                _weight = piece._weight
            };
        }

        onSpinEndEvent.Invoke(finalPiece);
    }

    onSpinStartEvent = null; 
    onSpinEndEvent = null;
}) ;
         }
      }


      public void OnSpinStart (UnityAction action) {
         onSpinStartEvent = action ;
      }

      public void OnSpinEnd (UnityAction<CarkParca> action) {
         onSpinEndEvent = action ;
      }

      private int GetRandomPieceIndex () {
         double r = rand.NextDouble () * accumulatedWeight ;

         for (int i = 0; i < carkParcalari.Length; i++)
            if (carkParcalari [ i ]._weight >= r)
               return i ;

         return 0 ;
      }

      private void CalculateWeightsAndIndices () {
         for (int i = 0; i < carkParcalari.Length; i++) {
            CarkParca piece = carkParcalari [ i ] ;

            //add weights:
            accumulatedWeight += piece.Chance ;
            piece._weight = accumulatedWeight ;

            //add index :
            piece.Index = i ;

            //save non zero chance indices:
            if (piece.Chance > 0)
               nonZeroChancesIndices.Add (i) ;
         }
      }

      // İndicator için otomatik rotasyon offset hesaplama
      private float GetIndicatorRotationOffset(float angle) {
         // İndicator'ın merkeze bakması için her zaman angle + 180° olması gerekiyor
         // Çünkü indicator pozisyonu angle°'de ise, merkeze bakmak için 180° daha dönmeli
         return 180f;
      }

     private void UpdateIndicatorPosition() {
    if (indicatorTransform != null && wheelPiecesParent != null) {
        float angleRad = -indicatorAngle * Mathf.Deg2Rad;
        float radius = Vector3.Distance(indicatorTransform.position, wheelPiecesParent.position);
        if (radius == 0f) radius = 100f;

        // Pozisyon
        Vector3 newPosition = wheelPiecesParent.position + new Vector3(
            Mathf.Cos(angleRad + Mathf.PI / 2f) * radius,
            Mathf.Sin(angleRad + Mathf.PI / 2f) * radius,
            0f
        );
        indicatorTransform.position = newPosition;

        // Rotasyon → merkeze baksın
        Vector3 dir = (wheelPiecesParent.position - indicatorTransform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Prefab’ın editördeki varsayılan yönüne göre offset ver
        indicatorTransform.rotation = Quaternion.Euler(0, 0, angle + 90f);
    }
}


      // Scene view'da indicator pozisyonunu görsel olarak göstermek için
      private void OnDrawGizmosSelected() {
         if (!showIndicatorInSceneView || wheelPiecesParent == null) return;

         // İndicator pozisyonunu hesapla
         float radiusGizmo = 150f; // Gizmo çizgi uzunluğu
         Vector3 indicatorDirection = new Vector3(
            Mathf.Sin(indicatorAngle * Mathf.Deg2Rad),
            Mathf.Cos(indicatorAngle * Mathf.Deg2Rad),
            0f
         );

         Vector3 indicatorPos = wheelPiecesParent.position + indicatorDirection * radiusGizmo;

         // İndicator çizgisini çiz
         Gizmos.color = Color.red;
         Gizmos.DrawLine(wheelPiecesParent.position, indicatorPos);
         
         // İndicator ucunda küçük bir daire çiz
         Gizmos.color = Color.yellow;
         Gizmos.DrawWireSphere(indicatorPos, 10f);
      }

#if UNITY_EDITOR 
      // Inspector'da değişiklik yapıldığında çağrılır
      private void OnValidate() {
         // İndicator açısını 0-360 arasında tut
         indicatorAngle = indicatorAngle % 360f;
         if (indicatorAngle < 0f) 
            indicatorAngle += 360f;
            
         // Oyun çalışırken indicator pozisyonunu güncelle
         if (Application.isPlaying) {
            UpdateIndicatorPosition();
         }
      }
#endif
   }
}