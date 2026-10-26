from pathlib import Path
import json, wave
import numpy as np

# Original score and procedural instrument models; no sampled recordings.
OUT = Path(__file__).resolve().parent
SR = 44100
BPM = 65
BEAT = 60 / BPM
N = round(160 * BEAT * SR)
DURATION = N / SR
rng = np.random.default_rng(10252026)
music = np.zeros((N, 2), dtype=np.float32)
nature = np.zeros_like(music)

def hz(note):
    return 440 * 2 ** ((note - 69) / 12)

def add(bus, signal, start, gain=1, pan=0):
    signal = np.asarray(signal, dtype=np.float32)
    pos = round(start * SR) % N
    stereo = signal[:, None] * np.array([np.cos((pan+1)*np.pi/4), np.sin((pan+1)*np.pi/4)], dtype=np.float32) * gain
    first = min(len(signal), N-pos)
    bus[pos:pos+first] += stereo[:first]
    if first < len(signal):
        bus[:len(signal)-first] += stereo[first:]

def timeline(seconds):
    return np.arange(round(seconds*SR), dtype=np.float64)/SR

def filtered_noise(size, low, high):
    white = rng.normal(size=size)
    freq = np.fft.rfftfreq(size, 1/SR)
    lo = 1 - np.exp(-(freq/max(low, 1))**4)
    hi = np.exp(-(freq/high)**4)
    result = np.fft.irfft(np.fft.rfft(white)*lo*hi, n=size)
    return (result / (np.std(result)+1e-10)).astype(np.float32)

def flute(note, length, quena=True):
    t = timeline(length+.7)
    f = hz(note)
    # A soft breath transient, unequal harmonic amplitudes and slow pitch drift.
    vibrato = .0018*np.sin(2*np.pi*4.65*t) * np.minimum(t/.9, 1)
    drift = .0009*np.sin(2*np.pi*.47*t+rng.uniform(0,6.28))
    phase = 2*np.pi*np.cumsum(f*(1+vibrato+drift-.003*np.exp(-t/.08)))/SR
    amplitudes = [1,.22,.10,.035,.012] if quena else [1,.11,.025,.008]
    s = sum(a*np.sin((h+1)*phase+.04*h) for h,a in enumerate(amplitudes))
    breath = filtered_noise(len(t), 1400, 6300)
    env = (1-np.exp(-t/.18)) * np.minimum(1,np.maximum(0,(length+.7-t)/.85))**1.8
    env *= .94+.06*np.sin(2*np.pi*.7*t)
    return (s*.7+breath*.035)*env

def pluck(note, kind):
    f=hz(note)
    length=4.8 if kind=='harp' else 2.4
    t=timeline(length)
    if kind=='harp':
        s=sum((1/(h**1.8))*np.sin(2*np.pi*f*h*t+rng.uniform(0,.08))*np.exp(-t*(.75+.33*h)) for h in range(1,9))
        env=(1-np.exp(-t/.012))*np.minimum(1,(length-t)/.15)
    else:
        s=sum(a*np.sin(2*np.pi*f*r*t)*np.exp(-t/decay) for r,a,decay in [(1,1,.65),(4,.14,.17),(9.15,.025,.08)])
        env=(1-np.exp(-t/.009))*np.minimum(1,(length-t)/.1)
    return s*env

chords = [
    [50,57,60,64,65], [48,55,59,62,64], [43,55,57,59,64],
    [50,57,60,64,65], [53,57,60,64,67], [48,55,59,62,64],
    [43,55,57,59,64], [52,55,59,62,69], [50,57,60,64,65], [50,57,60,64,69]
]
print('Rendering pads and original score...', flush=True)
for block, chord in enumerate(chords):
    start=block*16*BEAT-2.0
    length=16*BEAT+4
    t=timeline(length)
    env=np.minimum(1,t/4)*np.minimum(1,(length-t)/4)
    env=.5-.5*np.cos(np.pi*env)
    for j,note in enumerate(chord):
        f=hz(note)
        phase=rng.uniform(0,2*np.pi)
        s=(np.sin(2*np.pi*f*t+phase)+.26*np.sin(2*np.pi*f*2*t+phase)+.10*np.sin(2*np.pi*f*3*t))
        s+=.32*np.sin(2*np.pi*f*1.0012*t+phase+.6)
        s*=env*(.94+.06*np.sin(2*np.pi*.1*t+j))
        add(music,s,start,.024 if j else .034,(j-2)*.26)
    # Sparse harp figures, voiced above the pad, varying within each harmony.
    choices=[chord[1]+12,chord[3]+12,chord[2]+12,chord[4]+12]
    for k,b in enumerate([.35,3,5.5,8.5,11,13.5]):
        note=choices[(k+block)%4]
        add(music,pluck(note,'harp'),(block*16+b)*BEAT+rng.uniform(-.035,.035),rng.uniform(.035,.050),-.38)
    for k,b in enumerate([2,7.5,12]):
        note=chord[[1,3,2][k]]+12
        add(music,pluck(note,'marimba'),(block*16+b)*BEAT,.027,.40)

# Melodic cells: D-Dorian, with breathing space and a recurring E–D gesture.
phrases=[
 (1.1,[(74,2.6),(76,1.6),(81,2.8),(79,1.8),(76,2.7)]),
 (19.0,[(76,2.6),(74,1.6),(71,3.8)]),
 (34.5,[(69,2.7),(71,1.7),(74,3.2),(76,2.8)]),
 (50.5,[(77,2.6),(76,2.0),(74,4.1)]),
 (66.0,[(76,2.5),(79,2.8),(81,2.1),(79,3.0)]),
 (83.0,[(76,2.2),(74,2.5),(71,3.8)]),
 (99.0,[(74,2.7),(76,2.0),(79,3.8)]),
 (115.0,[(76,2.8),(74,2.0),(71,3.2),(69,2.0)]),
 (131.0,[(74,2.7),(76,1.6),(81,2.7),(79,2.0)]),
 (147.0,[(76,3.3),(74,4.6)])
]
for p,(start,notes) in enumerate(phrases):
    cursor=start
    for k,(note,length) in enumerate(notes):
        add(music,flute(note,length*BEAT,quena=p%3!=1),cursor*BEAT,.066 if p%3!=1 else .059,-.13 if p%3!=1 else .18)
        cursor+=length+.35

print('Rendering soft percussion and rainforest ambience...', flush=True)
for bar in range(40):
    t=timeline(.36)
    s=filtered_noise(len(t),2200,8000)*(1-np.exp(-t/.014))*np.exp(-t/.065)*np.minimum(1,(.36-t)/.04)
    for offset in [1.45,3.4]:
        add(music,s,(bar*4+offset)*BEAT+rng.uniform(-.03,.03),.008,rng.uniform(-.5,.5))
    if bar%2==0:
        t=timeline(1.1)
        phase=2*np.pi*(78*t+4*.07*(1-np.exp(-t/.07)))
        s=(np.sin(phase)+.16*np.sin(1.59*phase))*np.exp(-t/.24)*(1-np.exp(-t/.018))
        s*=np.minimum(1,(1.1-t)/.1)
        add(music,s,bar*4*BEAT,.025,.06)

# Full-length periodic noise fields make the ambience continuous at the loop.
for ch in range(2):
    t=np.arange(N,dtype=np.float64)/SR
    water=filtered_noise(N,230,3200)
    wind=filtered_noise(N,90,850)
    rustle=filtered_noise(N,2400,7600)
    mod=.84+.10*np.sin(2*np.pi*7*t/DURATION+ch)+.06*np.sin(2*np.pi*19*t/DURATION)
    leafmod=(.5+.5*np.sin(2*np.pi*11*t/DURATION+ch*2))**5
    nature[:,ch]+=water*.010*mod+wind*.0035+rustle*.0017*leafmod
    del t,water,wind,rustle

# Nocturnal insects and crickets: narrow-band, gently irregular pulse trains.
for start in np.arange(0,DURATION,1.85):
    dur=rng.uniform(.35,1.1)
    t=timeline(dur)
    f=rng.uniform(3650,5100)
    pulses=np.maximum(0,np.sin(2*np.pi*rng.uniform(18,25)*t))**4
    s=np.sin(2*np.pi*f*t+.6*np.sin(2*np.pi*31*t))*pulses*np.sin(np.pi*t/dur)**2
    add(nature,s,start+rng.uniform(0,.6),rng.uniform(.0025,.006),rng.uniform(-.9,.9))

# Occasional soft frog calls and distant tropical bird-like whistles.
for start in [5,17,29,44,58,74,86,103,119,133,143]:
    dur=rng.uniform(.38,.7); t=timeline(dur); f=rng.uniform(340,570)
    phase=2*np.pi*(f*t+22*.13*(1-np.exp(-t/.13)))
    s=(np.sin(phase)+.22*np.sin(2*phase))*np.sin(np.pi*t/dur)**2*(.7+.3*np.cos(2*np.pi*23*t))
    pan=rng.uniform(-.8,.8)
    add(nature,s,start,.012,pan)
    add(nature,s,start+dur+.13,.008,pan)
for start in [11,38,64,93,122,139]:
    pan=rng.uniform(-.8,.8)
    for j in range(3):
        dur=.3+j*.08; t=timeline(dur); f=rng.uniform(1400,1950)
        phase=2*np.pi*(f*t+230*dur/np.pi*np.sin(np.pi*t/dur))
        s=np.sin(phase+1.8*np.sin(2*np.pi*7*t))*np.sin(np.pi*t/dur)**2
        add(nature,s,start+j*.47,.007*(1-.15*j),pan)

def reverb(bus, wet):
    result=bus.copy()
    # Circular reflections carry the final tails into the first samples.
    delays=[.071,.113,.173,.239,.317,.419,.557,.733,.941,1.193,1.487,1.829,2.231,2.701,3.239,3.863]
    for i,delay in enumerate(delays):
        shift=round(delay*SR)
        src=bus[:,::-1] if i%2 else bus
        result+=np.roll(src,shift,axis=0)*(wet*np.exp(-delay/1.35)*(.8 if i%3==0 else 1))
    return result

print('Mixing seamless stereo master...', flush=True)
music=reverb(music,.105)
nature=reverb(nature,.052)
mix=music+nature
mix-=np.mean(mix,axis=0)
# Gentle saturation and one global gain preserve the calm internal dynamics.
mix=np.tanh(mix*.95)
mix*=.77/np.max(np.abs(mix))
# Remove only the microscopic discontinuity of the sampled loop endpoints.
edge=256
delta=mix[0]-mix[-1]
mix[-edge:]+=np.linspace(0,1,edge,dtype=np.float32)[:,None]*delta
pcm=np.round(np.clip(mix,-1,1)*32767).astype('<i2')
path=OUT/'Bruma_del_Caqueta_65BPM_LOOP.wav'
with wave.open(str(path),'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
windows=[float(np.sqrt(np.mean(mix[i:i+SR*8]**2))) for i in range(0,N-SR*8,SR*8)]
report={
 'title':'Bruma del Caquetá','bpm':BPM,'bars':40,'meter':'4/4','duration_seconds':DURATION,
 'sample_rate':SR,'frames':N,'channels':2,'pcm_bits':16,
 'peak_dbfs':float(20*np.log10(np.max(np.abs(mix)))),
 'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(mix**2)))),
 'eight_second_rms_range_db':float(20*np.log10(max(windows)/min(windows))),
 'loop_endpoint_difference_pcm':(pcm[0].astype(int)-pcm[-1].astype(int)).tolist(),
 'finite_samples':bool(np.isfinite(mix).all()),
 'production':'Original composition. Synthesized instruments and environmental sound design; no voices or field recordings.'
}
(OUT/'verificacion_audio.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(report,indent=2,ensure_ascii=False),flush=True)
