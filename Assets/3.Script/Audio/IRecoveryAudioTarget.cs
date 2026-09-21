using UnityEngine;

public interface IRecoveryAudioTarget
{
    bool TryGetRecoveryAudio(out RecoverySoundType soundType, out Transform soundAnchor);
}