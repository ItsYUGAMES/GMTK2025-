using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("音频源")]
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;

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
            DontDestroyOnLoad(gameObject); // 已经设置了
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
        // 如果没有指定音频源，自动创建
        if (musicAudioSource == null)
        {
            GameObject musicObj = new GameObject("MusicAudioSource");
            musicObj.transform.SetParent(transform);
            musicAudioSource = musicObj.AddComponent<AudioSource>();
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
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

    // 播放背景音乐
    public void PlayBackgroundMusic(int musicIndex)
    {
        if (backgroundMusic.Length == 0 || musicIndex < 0 || musicIndex >= backgroundMusic.Length)
            return;

        currentMusicIndex = musicIndex;
        musicAudioSource.clip = backgroundMusic[musicIndex];
        musicAudioSource.Play();
        isMusicPlaying = true;

        Debug.Log($"开始播放背景音乐: {backgroundMusic[musicIndex].name}");
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
        if (isMusicPlaying && currentMusicIndex == musicIndex)
        {
            musicAudioSource.Stop();
            isMusicPlaying = false;
            Debug.Log($"已停止播放背景音乐[{musicIndex}]: {backgroundMusic[musicIndex].name}");
        }
        else if (currentMusicIndex != musicIndex)
        {
            Debug.Log($"当前播放的不是音乐[{musicIndex}]，无需停止");
        }
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
    // 获取当前状态
    public bool IsMusicPlaying() => isMusicPlaying && musicAudioSource.isPlaying;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public string GetCurrentMusicName() => 
        backgroundMusic.Length > 0 && currentMusicIndex < backgroundMusic.Length ? 
        backgroundMusic[currentMusicIndex].name : "无";

    void Update()
    {
        // 检查音乐是否播放完毕，自动播放下一首
        if (isMusicPlaying && !musicAudioSource.isPlaying && backgroundMusic.Length > 1)
        {
            PlayNextMusic();
        }
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