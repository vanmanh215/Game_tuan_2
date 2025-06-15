using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Logic")]
    [SerializeField] private float moveRate = 0.2f;

    private bool canMove = false;
    private Vector2 movementDirection = Vector2.right;

    [Header("Snake Composition")]
    [SerializeField] private Transform bodyParent;
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private int initialSize = 4;

    [Header("Snake Sprites (Visuals)")]
    [SerializeField] private Sprite headSprite;

    [SerializeField] private Sprite bodyHorizontalSprite1; 
    [SerializeField] private Sprite bodyHorizontalSprite2;
    [SerializeField] private Sprite bodyVerticalSprite;   

    [SerializeField] private Sprite cornerSprite4;
    [SerializeField] private Sprite cornerSprite5;
    [SerializeField] private Sprite cornerSprite6;

    [SerializeField] private Sprite tailSprite;

    [SerializeField] private Sprite headEatSprite;
    [SerializeField] private Sprite headHitSprite;

    private List<Transform> segments = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>(); 

    private SpriteRenderer headSpriteRenderer;
    private bool shouldGrow = false;
    private PlayerControls playerControls;
    [SerializeField] private float knockbackMoveRate = 0.08f;
    [SerializeField] private GameObject faceVisual;
    [Header("Effects")]
    [SerializeField] private GameObject chiliEffectPrefab;

    public void ActivatePlayer()
    {
        this.canMove = true;
        Debug.Log("Player Controls have been ACTIVATED.");
    }
    public void AttemptMove(Vector2 direction)
    {
        if (!canMove || (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing))
        {
            return;
        }

        if (direction != -movementDirection && direction != Vector2.zero)
        {
            StartCoroutine(Move(direction));
        }
    }
    private void Awake()
    {
        playerControls = new PlayerControls();
        headSpriteRenderer = GetComponent<SpriteRenderer>();
        if (headSpriteRenderer) headSpriteRenderer.sprite = headSprite;
    }

    private void OnEnable()
    {
        playerControls.Snake.Enable();
    }

    private void OnDisable()
    {
        playerControls.Snake.Disable();
    }

    void Start()
    {
        InitializeSnake();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.upButton.onClick.AddListener(OnPressUp);
            UIManager.Instance.downButton.onClick.AddListener(OnPressDown);
            UIManager.Instance.leftButton.onClick.AddListener(OnPressLeft);
            UIManager.Instance.rightButton.onClick.AddListener(OnPressRight);
        }
    }
    private void OnDestroy()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.upButton.onClick.RemoveListener(OnPressUp);
            UIManager.Instance.downButton.onClick.RemoveListener(OnPressDown);
            UIManager.Instance.leftButton.onClick.RemoveListener(OnPressLeft);
            UIManager.Instance.rightButton.onClick.RemoveListener(OnPressRight);
        }
    }

    // *** TẠO CÁC HÀM CÔNG KHAI NÀY CHO BUTTON ***
    public void OnPressUp() { AttemptMove(Vector2.up); }
    public void OnPressDown() { AttemptMove(Vector2.down); }
    public void OnPressLeft() { AttemptMove(Vector2.left); }
    public void OnPressRight() { AttemptMove(Vector2.right); }
    void Update()
    {
        if (!canMove || (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)) return;
        //HandleInput();
    }

    private void LateUpdate()
    {
        if (segments.Count > 0)
        {
            Quaternion autoRotation = Quaternion.LookRotation(Vector3.forward, movementDirection);
            transform.rotation = autoRotation * Quaternion.Euler(0, 0, 180);
        }
    }

    private void InitializeSnake()
    {
        // --- BƯỚC 1: DỌN DẸP TRIỆT ĐỂ ---
        foreach (Transform child in bodyParent)
        {
            Destroy(child.gameObject);
        }
        segments.Clear();

        // --- BƯỚC 2: THIẾT LẬP ĐẦU RẮN ---
        segments.Add(this.transform);
        Vector3Int startingCell = GridManager.Instance.WorldToCell(transform.position);
        transform.position = GridManager.Instance.GetCellCenter(startingCell);

        // --- BƯỚC 3: TẠO THÂN RẮN ---
        for (int i = 1; i < initialSize; i++)
        {
            // Vị trí của đốt thân mới sẽ ở ngay phía sau đốt vừa được thêm vào cuối danh sách.
            Vector3 spawnPosition = segments.Last().position - (Vector3)movementDirection;
            GameObject newSegment = Instantiate(bodyPrefab, spawnPosition, Quaternion.identity, bodyParent);
            segments.Add(newSegment.transform);
        }

        // --- BƯỚC 4: CẬP NHẬT HÌNH ẢNH ---
        UpdateVisuals();
    }
    private void HandleInput()
    {
        Vector2 input = playerControls.Snake.Move.ReadValue<Vector2>();
        if (input == Vector2.zero) return;

        Vector2 potentialMoveDir = Vector2.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            potentialMoveDir.x = Mathf.Sign(input.x);
        }
        else
        {
            potentialMoveDir.y = Mathf.Sign(input.y);
        }
        if (potentialMoveDir != -movementDirection)
        {
            StartCoroutine(Move(potentialMoveDir));
        }
    }
    private IEnumerator Move(Vector2 potentialDirection)
    {
        canMove = false;

        Vector3Int nextHeadCell = GridManager.Instance.WorldToCell(transform.position + (Vector3)potentialDirection);
        TileType nextTileType = GridManager.Instance.GetTileTypeAt(nextHeadCell);

        if (nextTileType == TileType.Stone || nextTileType == TileType.Wood || IsHittingOwnBody(nextHeadCell))
        {
            canMove = true;
            yield break;
        }
        if (nextTileType == TileType.Hole)
        {
            yield return StartCoroutine(WinSequenceRoutine(GridManager.Instance.GetCellCenter(nextHeadCell)));
            yield break;
        }
        if (nextTileType == TileType.Banana || nextTileType == TileType.Chili)
        {
            Vector3Int pushTargetCell = nextHeadCell + Vector3Int.RoundToInt(potentialDirection);
            TileType typeAtPushTarget = GridManager.Instance.GetTileTypeAt(pushTargetCell);

            if (typeAtPushTarget == TileType.None && !IsHittingOwnBody(pushTargetCell))
            {
                GridManager.Instance.MoveTile(nextHeadCell, pushTargetCell);
                nextTileType = TileType.None;
            }
            else
            {
                GridManager.Instance.ClearTile(GridManager.Instance.ItemsTilemap, nextHeadCell);
                GameManager.Instance?.OnItemCollected();

                if (nextTileType == TileType.Banana)
                {
                    shouldGrow = true;
                    StartCoroutine(ChangeFaceTemporarily(headEatSprite, 0.3f));
                }
                else if (nextTileType == TileType.Chili)
                {
                    StartCoroutine(ChangeFaceTemporarily(headHitSprite, 0.5f));
                    yield return StartCoroutine(KnockbackRoutine(potentialDirection));
                    canMove = true;
                    yield break;
                }
            }
        }
        if (nextTileType == TileType.Hole)
        {
            GameManager.Instance?.WinGame();
            yield break;

        }

        movementDirection = potentialDirection;
        Vector3 previousHeadPos = transform.position;
        transform.position = GridManager.Instance.GetCellCenter(nextHeadCell);

        Vector3 previous = previousHeadPos;
        for (int i = 1; i < segments.Count; i++)
        {
            Vector3 temp = segments[i].position;
            segments[i].position = previous;
            previous = temp;
        }

        if (shouldGrow)
        {
            GameObject newSegment = Instantiate(bodyPrefab, previous, Quaternion.identity, bodyParent);
            segments.Add(newSegment.transform);
            shouldGrow = false;
        }
        UpdateVisuals();
        yield return new WaitForSeconds(moveRate);
        canMove = true;
    }
    private IEnumerator KnockbackRoutine(Vector2 impactDirection)
    {
        Vector3 knockbackDirection = -impactDirection;

        if (chiliEffectPrefab != null)
        {
            Vector3 spawnPosition = transform.position + (Vector3)movementDirection;

            Quaternion baseRotation = Quaternion.LookRotation(Vector3.forward, movementDirection);

            Quaternion offsetRotation = Quaternion.Euler(0, 0, -90f);
            Quaternion finalRotation = baseRotation * offsetRotation;

            GameObject effectInstance = Instantiate(
                chiliEffectPrefab,
                spawnPosition,
                finalRotation,
                transform);

            // Tự động hủy hiệu ứng sau 0.5 giây để dọn dẹp
            Destroy(effectInstance, 0.5f);
        }
        while (true)
        {
            // --- PEEK AHEAD: "Nhìn trước" xem bước trượt tiếp theo có an toàn không ---
            bool obstacleAhead = false;
            Vector3Int knockbackOffset = Vector3Int.RoundToInt(knockbackDirection);
            BoundsInt mapBounds = GridManager.Instance.GetMapBounds();

            // Kiểm tra từng đốt của con rắn
            foreach (Transform segment in segments)
            {
                Vector3Int futureCell = GridManager.Instance.WorldToCell(segment.position) + knockbackOffset;

                // Điều kiện dừng 1: Bất kỳ đốt nào sắp va vào vật cản
                TileType typeAtFutureCell = GridManager.Instance.GetTileTypeAt(futureCell);
                if (typeAtFutureCell == TileType.Stone || typeAtFutureCell == TileType.Wood)
                {
                    obstacleAhead = true;
                    break; // Tìm thấy vật cản -> dừng kiểm tra
                }

                //// Điều kiện dừng 2: Bất kỳ đốt nào sắp đi ra khỏi bản đồ
                //if (!mapBounds.Contains(futureCell))
                //{
                //    GameManager.Instance?.LoseGame("Trượt ra khỏi bản đồ");
                //    yield break; // Kết thúc coroutine ngay lập tức
                //}
            }

            // Nếu "nhìn trước" thấy có vật cản -> dừng trượt
            if (obstacleAhead)
            {
                break; // Thoát khỏi vòng lặp while
            }

            foreach (Transform segment in segments)
            {
                Vector3Int currentCell = GridManager.Instance.WorldToCell(segment.position);
                Vector3Int nextCell = currentCell + knockbackOffset;
                segment.position = GridManager.Instance.GetCellCenter(nextCell);
            }

            // Cập nhật hình ảnh và chờ một chút để tạo hiệu ứng trượt
            UpdateVisuals();
            yield return new WaitForSeconds(knockbackMoveRate);
        }
    }

    private IEnumerator WinSequenceRoutine(Vector3 holeCenter)
    {
        canMove = false;

        while (segments.Count > 0)
        {
            List<Vector3> startPositions = segments.Select(s => s.position).ToList();
            float timer = 0f;
            float duration = moveRate;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);

                if (segments.Count > 0)
                {
                    segments[0].position = Vector3.Lerp(startPositions[0], holeCenter, t);
                }

                for (int i = 1; i < segments.Count; i++)
                {
                    segments[i].position = Vector3.Lerp(startPositions[i], startPositions[i - 1], t);
                }

                yield return null;
            }

            Transform consumedSegment = segments[0];
            segments.RemoveAt(0);
            if (consumedSegment == this.transform)
            {
                if (headSpriteRenderer != null)
                    headSpriteRenderer.enabled = false;

                if (faceVisual != null)
                    faceVisual.SetActive(false);
            }
            else
            {
                consumedSegment.gameObject.SetActive(false);
            }
        }

        GameManager.Instance?.WinGame();
    }

    private void UpdateHeadRotation()
    {
        if (segments.Count > 0)
        {
            float angle = Vector2.SignedAngle(Vector2.right, movementDirection);
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private bool IsHittingOwnBody(Vector3Int position)
    {
        for (int i = 0; i < segments.Count - 1; i++)
        {
            if (GridManager.Instance.WorldToCell(segments[i].position) == position)
            {
                return true;
            }
        }
        return false;
    }
    public void Grow()
    {
        shouldGrow = true;
    }

    private void UpdateVisuals()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            SpriteRenderer sr = segments[i].GetComponent<SpriteRenderer>();
            Vector2 directionToPrev = (segments[i - 1].position - segments[i].position).normalized;

            if (i == segments.Count - 1)
            {
                sr.sprite = tailSprite;
                Quaternion autoRotation = Quaternion.LookRotation(Vector3.forward, directionToPrev);
                sr.transform.rotation = autoRotation * Quaternion.Euler(0, 0, -90);
            }
            else
            {
                Vector2 directionFromNext = (segments[i].position - segments[i + 1].position).normalized;

                if (Vector2.Dot(directionToPrev, directionFromNext) > 0.9f)
                {
                    if (directionToPrev == Vector2.up || directionToPrev == Vector2.down)
                    {
                        sr.sprite = bodyVerticalSprite;
                        sr.transform.rotation = Quaternion.LookRotation(Vector3.forward, directionToPrev);
                    }
                    else
                    {
                        sr.sprite = Random.Range(0, 2) == 0 ? bodyHorizontalSprite1 : bodyHorizontalSprite2;
                        float angle = Vector2.SignedAngle(Vector2.right, directionToPrev);
                        sr.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }
                // Thân cong
                else
                {
                    float angle;
                    sr.sprite = GetCornerSprite(directionFromNext, directionToPrev, out angle);
                    sr.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
    }
    public IEnumerator ChangeFaceTemporarily(Sprite newFace, float duration)
    {
        if (headSpriteRenderer == null) yield break;
        Sprite originalFace = headSprite;
        headSpriteRenderer.sprite = newFace;
        yield return new WaitForSeconds(duration);
        headSpriteRenderer.sprite = originalFace;
    }
    private Sprite GetCornerSprite(Vector2 fromDir, Vector2 toDir, out float angle)
    {
        angle = 0f;
        // --- Quy tắc cho Sprite 4 --
        if (fromDir == Vector2.left && toDir == Vector2.up) { angle = 90f; return cornerSprite4; }
        if (fromDir == Vector2.down && toDir == Vector2.right) { angle = 90f; return cornerSprite4; }
        // --- Quy tắc cho Sprite 5 ---
        if (fromDir == Vector2.right && toDir == Vector2.up) { angle = 270f; return cornerSprite5; }
        if (fromDir == Vector2.down && toDir == Vector2.left) { angle = 270f; return cornerSprite5; }
        if (fromDir == Vector2.up && toDir == Vector2.left) { angle = 0f; return cornerSprite5; }
        // --- Quy tắc cho Sprite 6 ---
        if (fromDir == Vector2.left && toDir == Vector2.down) { angle = 270f; return cornerSprite6; }
        if (fromDir == Vector2.up && toDir == Vector2.right) { angle = 270f; return cornerSprite6; }
        Debug.LogWarning($"Không tìm thấy quy tắc sprite góc phù hợp cho: From {fromDir} To {toDir}");
        return cornerSprite4;
    }
}