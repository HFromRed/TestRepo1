# Audio source and integration

Created for this project on 2026-11-01 using Tools/Audio/generate_audio.py.
Original procedural synthesis (oscillators, seeded noise and original rhythmic arrangements).
No external recordings, samples, songs or voices were used. No third-party audio license or attribution requirement applies. These generated project assets may be used and modified with this project.

Runtime loads this Resources/EchoAudio directory. The older Assets/VariablesEcho/Audio directory is a legacy draft and is not loaded by the audio manager.

Music: BGM_Operations (72 seconds), BGM_Combat (72 seconds), BGM_Boss (80 seconds). Stereo, 44.1 kHz; imported as streaming music. One-second crossfade; same active track does not restart across scenes.

Core effects: UI_Confirm, Node_Select, Card_Draw (play/select), Attack_Impact, Shield_Activate, Player_Hurt, Victory_Sting, Chamber_Close. Additional Noise_Collapse and Defeat_Sting cues. Mono, 44.1 kHz.

F10 opens Master/Music/Effects controls. Defaults 100/25/65 percent. Settings use PlayerPrefs and save on change, including zero volume.

The final ResolveFinal flow invokes the chamber cue, stops both music sources, waits for the cue plus 0.7 seconds, then starts operations ambience. Noise collapse plays once on entry, resetting after leaving that state.

Verification: generated files and loop boundary samples inspected; both chapter scenes contain AudioListeners. Actual Unity battle listening, rapid-click playback, cross-scene playback and restart persistence remain to be verified in the running editor. This is not a completed listening acceptance report.


## Abyssal industrial revision

Music and long mechanical gestures now use 26–36 Hz sub-bass, saw oscillators detuned up to approximately +/-4.5%, 150–950 Hz bandpass-filtered white noise, and ADSR layers with 1.8-second attack, 1.2-second decay, 68% sustain, and 4-second release. The complete tails wrap around music loops.

Convolution is rendered offline using scipy.signal.fftconvolve with an original eight-second synthetic, stereo, damped concrete-shelter impulse response. No external IR license is needed. Music uses circular convolution to retain reverb across loop boundaries.

Buttons, node/card selection and hit feedback retain immediate low mechanical transients as an intentional interaction exception to the slow texture ADSR. No high-frequency sine beeps, vocals, pop backbeat or hi-hats are used. Legacy draft WAVs have also been replaced.

Checks after generation: music peaks are 0.42 full scale; 83–99% of spectral energy is below 80 Hz; loop sample discontinuities are 7–23 units in 16-bit PCM. These are file checks, not confirmation of subjective listening quality. Real battle listening remains pending.


## Hydraulic / overload / vault revision

Reproduce this revision by running generate_audio.py first, then Tools/Audio/generate_mechanical_cues.py. All synthesis and cavity impulse responses remain original; no third-party audio or IR is used.

Attack_Impact: sub-bass hydraulic thump, brief inharmonic metal break and white-noise blast through tanh waveshaping, followed by a three-second synthetic metal-cavity convolution tail. Execution_Impact adds a heavier 26 Hz body and triggers on a lethal enemy hit.

Energy_Overload: 1.65-second rising low mechanical charge, hard cutoff, upward high-pass-filtered noise sweep, electrical crackles, heartbeat bass and envelope-driven ducking of the high layer. This is rendered ducking (sidechain-style pumping), not a live compressor plugin. It triggers on positive-noise or Noise-faction card play.

UI_Confirm / Node_Select: low mechanical latch and keyboard-like impact with brief distorted inharmonic chime. These explicit new prompt requests supersede the earlier ban on bright transient accents for these cues. No hover or typewriter sound was added. Background music remains abyssal industrial.

File validation and compilation do not replace the pending in-game battle listening acceptance.


## UI rumble and lethal execution revision

The preceding UI transient is retained before processing, layered with a 52 Hz sine oscillator triggered at sample zero: 0.01 second attack, 0.9 second squared release. The entire stereo cavity output passes through a 3000 Hz lowpass. Wet proportion is increased relative to the preceding version; transient and rumble are baked into the same clip for sample-aligned playback.

Execution_Impact now uses an 80-to-30 Hz exponential plunge, heavily waveshaped white noise through a second-order resonant bandpass (2200 Hz, Q 0.85), detuned saw motor decay, steam and staggered inharmonic part clatters, plus the three-second cavity IR. Enemy lethal damage triggers it and ducks both current/crossfading music sources to 4% for 0.5 seconds, recovering over 1.25 seconds. This affects the managed music/ambience bus, not unrelated sound effects. All synthesis remains original and reproducible through generate_mechanical_cues.py.

These resources implement the requested oscillators and filters offline. The music sidechain envelope runs at runtime so it follows the actual execution event. Runtime audition remains pending.


## Structural shockwave aftermath

Execution_Impact retains its hydraulic opening but replaces the motor/steam aftermath with six descending structural fracture bursts, 29 Hz descending pressure-wave pulses, distorted broadband shear cracks, metal-fragment collision noise and low bandpass shock air. The existing cavity reverb and runtime lethal-hit ducking remain. Regenerate just this cue using generate_mechanical_cues.py --execution-only; other runtime audio files are preserved. File checks: 4.7 seconds, peak 0.68 full scale, no clipped samples. Subjective in-game audition remains pending.
