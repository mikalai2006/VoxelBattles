using Cysharp.Threading.Tasks;
using UnityEngine;

public class AudioManager : StaticInstance<AudioManager>
{
  [SerializeField] private AudioSource _musicSource;
  public AudioSource MusicSource => _musicSource;
  [SerializeField] private AudioSource _entitySource;
  public AudioSource EntitySource => _entitySource;
  [SerializeField] private AudioSource _effectSource;
  public AudioSource EffectSource => _effectSource;
  [SerializeField] private GameSetting GameSetting;
  protected override void Awake()
  {
    base.Awake();
    // _musicSource.volume = GameSetting.Audio.volumeMusic;
    // _entitySource.volume = GameSetting.Audio.volumeEffect;
    // _effectSource.volume = GameSetting.Audio.volumeEffect;
  }

  public void PlayClipMusic(AudioClip clip)
  {
    MusicSource.PlayOneShot((AudioClip)clip);
  }

  public void PlayClipEffect(AudioClip clip)
  {
    // if (EffectSource.isPlaying)
    // {
    //   EffectSource.Stop();
    // }
    EffectSource.PlayOneShot((AudioClip)clip);
    // AudioSource.PlayClipAtPoint(clip, transform.position, GameSetting.Audio.volumeEffect);
  }

  public void PlayEntityEffect(AudioClip clip)
  {
    EntitySource.PlayOneShot((AudioClip)clip);
    // AudioSource.PlayClipAtPoint(clip, transform.position, GameSetting.Audio.volumeEffect);
  }

  public async UniTask PlayClipWithDuration(AudioClip clip, float durationMs, int stepMs, System.Threading.CancellationToken token)
  {
    var _currentTime = 0;
    while (_currentTime < durationMs)
    {
      EffectSource.PlayOneShot((AudioClip)clip);
      await UniTask.Delay(stepMs);
      _currentTime += stepMs;
    }
  }

  public void Click()
  {
    // EffectSource.PlayOneShot((AudioClip)GameSetting.Audio.clickButton);
  }
}
