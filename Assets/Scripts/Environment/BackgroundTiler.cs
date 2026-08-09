using UnityEngine;

namespace ConductorSymphony.Environment
{
    // 2026-08-09: 마룻바닥 도트 패턴(Assets/Resources/Sprites/Background/ParquetFloor.png)을
    // 카메라 화면 전체에 깔아서 렌더링하는 무한 반복 배경. CameraController가 플레이어를 그대로
    // 따라다니고(경계 없음, 무한 평면 전제) 몬스터도 플레이어 중심 반경으로 계속 스폰되는 기존
    // 구조에 맞춘 것 - 벽/경계 없이 카메라가 어디로 움직이든 배경이 항상 화면을 덮는다.
    //
    // 2026-08-09 전면 재작성(2번째 버그 수정, 실사용자 수동 플레이로 발견): 1차 시도는
    // SpriteRenderer.drawMode=Tiled + material.mainTextureOffset로 텍스처를 스크롤하는 방식이었는데,
    // Unity MCP 세션의 리플렉션 검증(수치만 확인)에서는 통과했지만 실제 화면에선 여전히 배경이
    // 고정되어 있었다. 원인: Sprites-Default를 비롯한 대부분의 스프라이트 셰이더는 _MainTex_ST
    // (Tiling/Offset)를 아예 셰이더에서 읽지 않는다(스프라이트는 원래 아틀라스 패킹으로 텍스처
    // 좌표를 미리 구워두는 방식이라, 머티리얼 프로퍼티로 오프셋을 주는 걸 애초에 지원하지 않음).
    // 즉 material.mainTextureOffset 값 자체는 정상적으로 설정됐지만(그래서 리플렉션으로 값을
    // 읽으면 맞게 나옴), 화면에는 전혀 반영되지 않았던 것 - 셰이더/머티리얼에 의존하는 트릭이라
    // 애초에 SpriteRenderer 기본 셰이더로는 불가능한 방법이었다.
    //
    // 수정: 셰이더 트릭을 완전히 버리고, 실제 타일 스프라이트 여러 장을 격자(그리드)로 배치해서
    // 카메라를 따라 재배치하는 고전적인 방식으로 재작성. 텍스처/머티리얼과 무관하게 순수
    // Transform.position 계산만으로 동작하므로 셰이더가 뭘 지원하든 확실하게 동작한다.
    public class BackgroundTiler : MonoBehaviour
    {
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Camera targetCamera;

        // 화면을 덮는 데 필요한 최소 타일 수보다 상하좌우 몇 칸씩 더 깔아둘지(카메라가 빠르게
        // 움직이거나 화면 가장자리에서 빈틈이 보이지 않도록 하는 여유분).
        [SerializeField] private int gridPaddingTiles = 1;

        private SpriteRenderer[,] tiles;
        private float tileWorldSize; // 타일 스프라이트 한 장이 차지하는 월드 유닛 크기(정사각형 가정)
        private int gridSize;        // gridSize x gridSize 타일을 미리 만들어두고 재배치만 한다

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            // 이 오브젝트 자체에 SpriteRenderer(예: 프리팹 초기 세팅)가 있다면 거기서 스프라이트를
            // 가져오고, 더 이상 그 SpriteRenderer로는 아무것도 그리지 않는다(격자 타일들이 대신
            // 그림 - 이중 렌더링 방지).
            SpriteRenderer selfRenderer = GetComponent<SpriteRenderer>();
            if (tileSprite == null && selfRenderer != null)
            {
                tileSprite = selfRenderer.sprite;
            }
            if (selfRenderer != null)
            {
                selfRenderer.enabled = false;
            }

            if (tileSprite == null || targetCamera == null) return;

            tileWorldSize = tileSprite.rect.width / tileSprite.pixelsPerUnit;
            BuildGrid();
        }

        private void BuildGrid()
        {
            float visibleHeight = targetCamera.orthographicSize * 2f;
            float visibleWidth = visibleHeight * targetCamera.aspect;

            int tilesNeededX = Mathf.CeilToInt(visibleWidth / tileWorldSize) + gridPaddingTiles * 2;
            int tilesNeededY = Mathf.CeilToInt(visibleHeight / tileWorldSize) + gridPaddingTiles * 2;
            gridSize = Mathf.Max(tilesNeededX, tilesNeededY);
            if (gridSize % 2 == 0) gridSize += 1; // 홀수로 고정 - 가운데 타일을 카메라 기준 중심으로 대칭 배치하기 위함
            gridSize = Mathf.Max(gridSize, 3);

            tiles = new SpriteRenderer[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(transform);
                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = tileSprite;
                    sr.sortingOrder = -100; // 항상 다른 모든 스프라이트보다 뒤에 그려짐
                    tiles[x, y] = sr;
                }
            }
        }

        private void LateUpdate()
        {
            if (tiles == null || targetCamera == null) return;

            Vector3 camPos = targetCamera.transform.position;
            // 카메라가 지금 어느 "격자 칸"에 있는지 계산 - 타일들은 항상 이 칸을 중심으로 배치된다.
            int centerX = Mathf.RoundToInt(camPos.x / tileWorldSize);
            int centerY = Mathf.RoundToInt(camPos.y / tileWorldSize);
            int half = gridSize / 2;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    int worldGridX = centerX - half + x;
                    int worldGridY = centerY - half + y;
                    tiles[x, y].transform.position = new Vector3(
                        worldGridX * tileWorldSize,
                        worldGridY * tileWorldSize,
                        transform.position.z);
                }
            }
        }
    }
}
