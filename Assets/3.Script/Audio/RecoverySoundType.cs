public enum RecoverySoundType
{
    None = 0,
    RestoreMovement = 1, // 이동·회전된 물체를 원래 상태로 복구
    RestoreMissing = 2,  // 사라진 물체 복구
    RemoveAdded = 3      // 추가된 물체 제거
}