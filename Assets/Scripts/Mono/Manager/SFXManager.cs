using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("音频源")]
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;
    
    [Header("多音乐播放支持")]
    public List<AudioSource> musicAudioSources = new List<AudioSource>();
    private Dictionary<int, AudioSource> activeMusicSources = new Dictionary<int, AudioSource>();
    
    [Header("背景音乐")]
    public AudioClip[] backgroundMusic;
    public float musicVolume = 0.5f;

    [Header("音效")]
    public AudioClip[] soundEffects;
    public float sfxVolume = 1f;

    private int currentMusicIndex = 0;
    private bool isMusicPlaying = false;
    
    [Header("UI音效")]
    public AudioClip buttonClickSFX;
    public AudioClip buttonHoverSFX;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 自动播放背景音乐
        if (backgroundMusic.Length > 0)
        {
            PlayBackgroundMusic(0);
        }
    }

    private void InitializeAudioSources()
    {
        // 初始化第一个音乐AudioSource
        if (musicAudioSource == null)
        {
            GameObject musicObj = new GameObject("MusicAudioSource_0");
            musicObj.transform.SetParent(transform);
            musicAudioSource = musicObj.AddComponent<AudioSource>();
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
        }

        // 将主音乐源添加到列表中
        if (!musicAudioSources.Contains(musicAudioSource))
        {
            musicAudioSources.Add(musicAudioSource);
        }

        if (sfxAudioSource == null)
        {
            GameObject sfxObj = new GameObject("SFXAudioSource");
            sfxObj.transform.SetParent(transform);
            sfxAudioSource = sfxObj.AddComponent<AudioSource>();
            sfxAudioSource.loop = false;
            sfxAudioSource.playOnAwake = false;
        }

        // 设置初始音量
        musicAudioSource.volume = musicVolume;
        sfxAudioSource.volume = sfxVolume;
    }

    // 播放背景音乐（支持多音乐共存）
    // 播放背景音乐（支持多音乐共存）
    public void PlayBackgroundMusic(int musicIndex)
    {
        if (backgroundMusic.Length == 0 || musicIndex < 0 || musicIndex >= backgroundMusic.Length)
        {
            Debug.LogError($"音乐索引{musicIndex}无效，backgroundMusic数组长度: {backgroundMusic.Length}");
            return;
        }

        if (backgroundMusic[musicIndex] == null)
        {
            Debug.LogError($"backgroundMusic[{musicIndex}]为空");
            return;
        }

        // 移除重复播放检查，允许多音乐共存
        // 如果需要停止相同音乐，可以手动调用StopSpecificBackgroundMusic
    
        // 获取或创建新的AudioSource
        AudioSource musicSource = GetAvailableMusicSource();

        musicSource.clip = backgroundMusic[musicIndex];
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();

        // 记录这个音乐索引对应的AudioSource
        activeMusicSources[musicIndex] = musicSource;
        isMusicPlaying = true;

        Debug.Log($"开始播放背景音乐[{musicIndex}]: {backgroundMusic[musicIndex].name}，使用AudioSource: {musicSource.name}");
        Debug.Log($"当前活跃音乐数量: {activeMusicSources.Count}");
    }

    // 获取可用的AudioSource
    private AudioSource GetAvailableMusicSource()
    {
        // 查找未使用的AudioSource
        foreach (var source in musicAudioSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // 如果没有可用的，创建新的
        GameObject musicObj = new GameObject($"MusicAudioSource_{musicAudioSources.Count}");
        musicObj.transform.SetParent(transform);
        AudioSource newSource = musicObj.AddComponent<AudioSource>();
        newSource.loop = true;
        newSource.playOnAwake = false;
        newSource.volume = musicVolume;

        musicAudioSources.Add(newSource);
        return newSource;
    }
    

    // 停止背景音乐
    public void StopBackgroundMusic()
    {
        musicAudioSource.Stop();
        isMusicPlaying = false;
        Debug.Log("背景音乐已停止");
    }

    // 停止指定索引的背景音乐
    public void StopSpecificBackgroundMusic(int musicIndex)
    {
        if (activeMusicSources.ContainsKey(musicIndex))
        {
            AudioSource source = activeMusicSources[musicIndex];
            if (source.isPlaying)
            {
                source.Stop();
                Debug.Log($"已停止播放背景音乐[{musicIndex}]: {backgroundMusic[musicIndex].name}");
            }
            activeMusicSources.Remove(musicIndex);
        }
        else
        {
            Debug.Log($"音乐[{musicIndex}]当前没有在播放");
        }
    }

    // 停止所有背景音乐
    public void StopAllBackgroundMusic()
    {
        foreach (var kvp in activeMusicSources)
        {
            if (kvp.Value.isPlaying)
            {
                kvp.Value.Stop();
            }
        }
        activeMusicSources.Clear();
        isMusicPlaying = false;
        Debug.Log("所有背景音乐已停止");
    }

    // 检查特定音乐是否正在播放
    public bool IsMusicPlaying(int musicIndex)
    {
        return activeMusicSources.ContainsKey(musicIndex) &&
               activeMusicSources[musicIndex].isPlaying;
    }

    // 暂停/恢复背景音乐
    public void PauseBackgroundMusic()
    {
        if (isMusicPlaying)
        {
            musicAudioSource.Pause();
            Debug.Log("背景音乐已暂停");
        }
    }

    public void ResumeBackgroundMusic()
    {
        if (isMusicPlaying)
        {
            musicAudioSource.UnPause();
            Debug.Log("背景音乐已恢复");
        }
    }

    // 播放下一首音乐
    public void PlayNextMusic()
    {
        if (backgroundMusic.Length > 1)
        {
            currentMusicIndex = (currentMusicIndex + 1) % backgroundMusic.Length;
            PlayBackgroundMusic(currentMusicIndex);
        }
    }

    // 播放上一首音乐
    public void PlayPreviousMusic()
    {
        if (backgroundMusic.Length > 1)
        {
            currentMusicIndex = (currentMusicIndex - 1 + backgroundMusic.Length) % backgroundMusic.Length;
            PlayBackgroundMusic(currentMusicIndex);
        }
    }

    // 播放音效
    public void PlaySFX(int sfxIndex)
    {
        if (soundEffects.Length == 0 || sfxIndex < 0 || sfxIndex >= soundEffects.Length)
            return;

        sfxAudioSource.PlayOneShot(soundEffects[sfxIndex]);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    // 设置音量
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicAudioSource.volume = musicVolume;
        
        // 同时更新所有音乐AudioSource的音量
        foreach (var source in musicAudioSources)
        {
            source.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxAudioSource.volume = sfxVolume;
    }

    // 静音控制
    public void MuteMusicToggle()
    {
        musicAudioSource.mute = !musicAudioSource.mute;
        
        // 同时控制所有音乐AudioSource的静音状态
        foreach (var source in musicAudioSources)
        {
            source.mute = musicAudioSource.mute;
        }
    }

    public void MuteSFXToggle()
    {
        sfxAudioSource.mute = !sfxAudioSource.mute;
    }

    // 获取音效AudioSource
    public AudioSource GetAudioSource()
    {
        return sfxAudioSource;
    }

    // 获取当前状态
    public bool IsMusicPlaying() => isMusicPlaying && musicAudioSource.isPlaying;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public string GetCurrentMusicName() =>
        backgroundMusic.Length > 0 && currentMusicIndex < backgroundMusic.Length ?
        backgroundMusic[currentMusicIndex].name : "无";

    void Update()
    {
        bool anyMusicPlaying = false;
        foreach (var kvp in activeMusicSources)
        {
            if (kvp.Value.isPlaying)
            {
                anyMusicPlaying = true;
                break;
            }
        }
        isMusicPlaying = anyMusicPlaying;
    }

    // 播放按钮点击音效
    public void PlayButtonClickSFX()
    {
        if (buttonClickSFX != null)
        {
            sfxAudioSource.time = 0.3f;
            sfxAudioSource.PlayOneShot(buttonClickSFX);
        }
    }

    public void OnButtonClick()
    {
        Debug.Log($"按钮被点击，backgroundMusic数组长度: {backgroundMusic.Length}");
        Debug.Log($"当前音乐播放状态: {isMusicPlaying}");

        // 播放点击音效
        SFXManager.Instance.PlayButtonClickSFX();

        // 检查数组长度
        if (backgroundMusic.Length > 1)
        {
            SFXManager.Instance.PlayBackgroundMusic(1);
            Debug.Log("开始播放backgroundMusic[1]");
        }
        else
        {
            Debug.LogError("backgroundMusic数组中没有索引为1的音乐！");
        }
    }

    // 播放按钮悬停音效
    public void PlayButtonHoverSFX()
    {
        if (buttonHoverSFX != null)
        {
            sfxAudioSource.PlayOneShot(buttonHoverSFX);
        }
    }

    // 通用的播放指定音效的方法（通过名称）
    public void PlaySFXByName(string clipName)
    {
        for (int i = 0; i < soundEffects.Length; i++)
        {
            if (soundEffects[i] != null && soundEffects[i].name == clipName)
            {
                sfxAudioSource.PlayOneShot(soundEffects[i]);
                return;
            }
        }
        Debug.LogWarning($"未找到名为 {clipName} 的音效");
    }
}