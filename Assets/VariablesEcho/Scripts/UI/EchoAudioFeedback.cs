using UnityEngine;

namespace VariablesEcho
{
    /// <summary>Central sound feedback and adaptive combat music for Variables Echo.</summary>
    public sealed class EchoAudioFeedback : MonoBehaviour
    {
        static EchoAudioFeedback instance;
        AudioSource music, outgoing;
        readonly AudioSource[] voices = new AudioSource[8];
        readonly float[] gains = new float[2];
        readonly System.Random pitchRandom = new System.Random();
        float master, musicLevel, effectsLevel;
        bool settings, collapsed, closing;
        Coroutine transition;
        float duckUntil = -10f;
        float duckGain = 1f;
        AudioClip bossMusic, closeClip, nodeClip, hurtClip, collapseClip, executionClip, overloadClip;
        public static void NodeSelect() { if(instance!=null) instance.Play(instance.nodeClip); }
        public static void Redeploy() { if(instance!=null && !instance.closing) instance.StartCoroutine(instance.CloseSequence()); }
        System.Collections.IEnumerator CloseSequence() {
            closing=true; if(transition!=null) StopCoroutine(transition);
            music.Stop(); outgoing.Stop(); music.clip=null; outgoing.clip=null; gains[0]=gains[1]=0; Play(closeClip);
            yield return new WaitForSecondsRealtime(closeClip!=null ? closeClip.length+.7f : 1.5f);
            closing=false; PlayMusic(mapMusic);
        }
        void OnNoise(NoiseController noise) { bool now=noise.Tier==EchoNoiseTier.Collapse; if(now && !collapsed) Play(collapseClip); collapsed=now; }
        void Update() {
            foreach(var voice in voices) if(voice!=null) voice.volume=master*effectsLevel*.65f;
            if(Input.GetKeyDown(KeyCode.F10)) settings=!settings;
            // Near-silence for 0.5 seconds; recover over 1.25 seconds in real time.
            duckGain=Time.unscaledTime<duckUntil ? .04f : Mathf.MoveTowards(duckGain,1f,Time.unscaledDeltaTime/1.25f);
            music.volume=gains[0]*master*musicLevel*duckGain; outgoing.volume=gains[1]*master*musicLevel*duckGain;
        }
        void OnGUI() {
            if(!settings)return;
            GUILayout.BeginArea(new Rect(20,20,320,230),GUI.skin.box);
            GUILayout.Label("Audio settings (F10)");
            float a=GUILayout.HorizontalSlider(master,0,1); GUILayout.Label("Master "+Mathf.RoundToInt(a*100)+"%");
            float b=GUILayout.HorizontalSlider(musicLevel,0,1); GUILayout.Label("Music "+Mathf.RoundToInt(b*100)+"%");
            float c=GUILayout.HorizontalSlider(effectsLevel,0,1); GUILayout.Label("Effects "+Mathf.RoundToInt(c*100)+"%");
            if(a!=master || b!=musicLevel || c!=effectsLevel) { master=a; musicLevel=b; effectsLevel=c; PlayerPrefs.SetFloat("EchoAudio.Master",a); PlayerPrefs.SetFloat("EchoAudio.Music",b); PlayerPrefs.SetFloat("EchoAudio.Effects",c); PlayerPrefs.Save(); }
            if(GUILayout.Button("Close")) { Button(); settings=false; } GUILayout.EndArea();
        }
        AudioClip click, draw, attack, shield, victory, defeat, battleMusic, mapMusic;
        static AudioClip Load(string name) { return Resources.Load<AudioClip>("EchoAudio/" + name); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("Echo Audio Feedback");
            instance = go.AddComponent<EchoAudioFeedback>();
            DontDestroyOnLoad(go);
        }
        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            master=PlayerPrefs.GetFloat("EchoAudio.Master",1); musicLevel=PlayerPrefs.GetFloat("EchoAudio.Music",.25f); effectsLevel=PlayerPrefs.GetFloat("EchoAudio.Effects",.65f);
            for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false; voices[i].spatialBlend=0; }
            music=gameObject.AddComponent<AudioSource>(); outgoing=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{music,outgoing}) { source.playOnAwake=false; source.loop=true; source.spatialBlend=0; source.volume=0; }
            executionClip=Load("Execution_Impact"); overloadClip=Load("Energy_Overload");
            bossMusic=Load("BGM_Boss"); closeClip=Load("Chamber_Close"); nodeClip=Load("Node_Select"); hurtClip=Load("Player_Hurt"); collapseClip=Load("Noise_Collapse");
            EchoEvents.NoiseChanged+=OnNoise;
            click=Load("UI_Confirm"); draw=Load("Card_Draw"); attack=Load("Attack_Impact"); shield=Load("Shield_Activate");
            victory=Load("Victory_Sting"); defeat=Load("Defeat_Sting"); battleMusic=Load("BGM_Combat"); mapMusic=Load("BGM_Operations");
            EchoEvents.BattleStarted += OnBattleStarted; EchoEvents.BattleEnded += OnBattleEnded;
            EchoEvents.CardPlayed += OnCardPlayed; EchoEvents.DamageDealt += OnDamage;
            PlayMusic(mapMusic);
        }
        void OnDestroy()
        {
            if (instance != this) return;
            EchoEvents.NoiseChanged-=OnNoise;
            EchoEvents.BattleStarted -= OnBattleStarted; EchoEvents.BattleEnded -= OnBattleEnded;
            EchoEvents.CardPlayed -= OnCardPlayed; EchoEvents.DamageDealt -= OnDamage; instance = null;
        }
        void PlayMusic(AudioClip clip) {
            if(clip==null || closing || music.clip==clip)return;
            if(transition!=null)StopCoroutine(transition);
            var temp=outgoing; outgoing=music; music=temp; gains[1]=gains[0]; gains[0]=0;
            music.clip=clip; music.volume=0; music.Play(); transition=StartCoroutine(Fade());
        }
        System.Collections.IEnumerator Fade() {
            float start=gains[1]; float time=0;
            while(time<1) { time+=Time.unscaledDeltaTime; gains[0]=Mathf.Clamp01(time); gains[1]=start*(1-Mathf.Clamp01(time)); yield return null; }
            outgoing.Stop(); transition=null;
        }
        void OnBattleStarted(EchoBattleContext battle) { collapsed=false; var director=EchoChapterController.Active; PlayMusic(director!=null && director.CurrentNode!=null && director.CurrentNode.kind==ChapterNodeKind.Boss ? bossMusic : battleMusic); }
        void OnBattleEnded(bool died) { Play(died ? defeat : victory); PlayMusic(mapMusic); }
        void OnCardPlayed(CCGKit.CardTemplate card, PlayedCardRecord record) { Play(record!=null && record.IsNoiseCard ? overloadClip : draw,true); if(card!=null && card.Effects.Exists(e=>e is CCGKit.GainShieldEffect)) Play(shield,true); }
        void OnDamage(CCGKit.RuntimeCharacter source, CCGKit.RuntimeCharacter target, int amount) {
            if(amount<=0 || target==null)return;
            bool player=EchoRuntime.Battle!=null && target==EchoRuntime.Battle.PlayerRuntime;
            if(!player && target.Hp.Value<=0) {
                duckUntil=Time.unscaledTime+.5f; duckGain=.04f;
                music.volume=gains[0]*master*musicLevel*duckGain;
                outgoing.volume=gains[1]*master*musicLevel*duckGain;
                Play(executionClip,true);
            } else Play(player ? hurtClip : attack,true);
        }
        void Play(AudioClip clip,bool combat=false) {
            if(clip==null)return;
            int active=0; AudioSource free=null, matching=null;
            foreach(var voice in voices) { if(voice.isPlaying && voice.clip==clip) { active++; matching=voice; } if(!voice.isPlaying && free==null)free=voice; }
            if(!combat && matching!=null) {
                if(matching.time<.07f)return;
                free=matching;
            } else if(clip==executionClip) {
                // A lethal hit must remain audible even when the voice pool is busy.
                if(free==null) free=matching!=null ? matching : voices[0];
            } else if(active>=(combat?2:1) || free==null)return;
            free.clip=clip; free.pitch=combat?(.96f+(float)pitchRandom.NextDouble()*.08f):1; free.volume=master*effectsLevel*.65f; free.Play();
        }
        public static void Button() { if (instance != null) instance.Play(instance.click); }
        public static void CardSelect() { if (instance != null) instance.Play(instance.draw); }
    }
}
