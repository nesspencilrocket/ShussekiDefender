import re, json, math
from pathlib import Path
root = Path.cwd()
out = root / 'Temp/DifficultyAudit'
stages = []
for n in range(1,7):
    s=(root / f'Assets/ScriptableObject/Stages/Stage_{n}.asset').read_text(encoding='utf-8-sig')
    def scalar(key, default=0):
        m=re.search(r'^  '+key+r': ([^\n\r]+)',s,re.M)
        return float(m[1]) if m else default
    waves=[]
    for match in re.finditer(r'^  - enemyCount: (\d+)\r?\n    wavesDelayTime: ([\d.]+)\r?\n    constantSpawnTime: ([\d.]+)\r?\n    minRandomDelay: ([\d.]+)\r?\n    maxRandomDelay: ([\d.]+)',s,re.M):
        vals=list(map(float,match.groups()))
        waves.append(dict(count=int(vals[0]),gap=vals[1],constant=vals[2],minimum=vals[3],maximum=vals[4]))
    hp=10*scalar('enemyHPMultiplier',1)
    data=dict(stage=n,hp=hp,time=scalar('clearTime'),loss=scalar('maxEnemyPasses'),money=scalar('initialCoin'),maxTowers=scalar('maxWeapons'),ramp=int(scalar('waveSpawnRampCount')),nodesPerLevel=scalar('nodesPerLevel'),waves=waves)
    data['normalShots']=[math.ceil((hp-1e-5)/d) for d in [2,3.2,4.2,5.2,6.2]]
    data['yellowFirstShots']=[math.ceil((hp-1e-5)/d) for d in [10,9.92,11.52,13.12,14.72]]
    threshold=re.search(r'^  levelUpKills:\n((?:  - \d+\n)+)',s,re.M)
    data['unlockKills']=list(map(int,re.findall(r'\d+',threshold[1])))
    for mode in ['minimum','mean','maximum']:
        t=0.; starts=[]; times=[]
        for widx,w in enumerate(waves):
            starts.append(t)
            for j in range(w['count']):
                times.append(t)
                cur=(w['minimum']+w['maximum'])/2 if mode=='mean' else w[mode]
                if widx>0 and data['ramp']>0:
                    p=min(1,j/data['ramp']); p=p*p*(3-2*p)
                    prev=waves[widx-1]
                    prevd=(prev['minimum']+prev['maximum'])/2 if mode=='mean' else prev[mode]
                    cur=prevd+(cur-prevd)*p
                t+=cur
            if widx<len(waves)-1:t+=w['gap']
        data[mode+'Schedule']=dict(waveStarts=starts,lastSpawn=times[-1],spawnsBeforeClear=sum(x<data['time'] for x in times))
    stages.append(data)
(out/'static-settings.json').write_text(json.dumps(stages,ensure_ascii=False,indent=2),encoding='utf-8')
print('stage HP normalLv1shots yellowLv1shots time loss cap wave2ratejump meanWave5start meanLastSpawn')
for d in stages:
    w=d['waves']; jump=((w[0]['minimum']+w[0]['maximum'])/(w[1]['minimum']+w[1]['maximum']))
    print(d['stage'],round(d['hp'],2),d['normalShots'][0],d['yellowFirstShots'][0],int(d['time']),int(d['loss']),int(d['maxTowers']),round(jump,2),round(d['meanSchedule']['waveStarts'][4],2),round(d['meanSchedule']['lastSpawn'],2))
g=json.loads((out/'pilot/geometry.json').read_text(encoding='utf-8-sig'))
def coverage(point,route,r):
    total=0
    for a,b in zip(route['points'],route['points'][1:]):
        x=a['x']-point['x']; y=a['y']-point['y']; vx=b['x']-a['x'];vy=b['y']-a['y']
        aa=vx*vx+vy*vy;bb=2*(x*vx+y*vy);cc=x*x+y*y-r*r
        disc=bb*bb-4*aa*cc
        if aa>0 and disc>=0:
            lo=max(0,(-bb-math.sqrt(disc))/(2*aa));hi=min(1,(-bb+math.sqrt(disc))/(2*aa))
            total+=max(0,hi-lo)*math.sqrt(aa)/3
    return total
rows=[]
for node in g['nodes']:
    row=dict(order=node['order'],position=node['position'])
    for kind,r in [('normal',2.5),('yellow',6)]:
        v=[coverage(node['position'],rte,r) for rte in g['routes']]
        row[kind]=dict(min=min(v),mean=sum(v)/len(v),max=max(v),zeroRoutes=sum(x==0 for x in v))
    rows.append(row)
(out/'geometry-coverage.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
for row in rows[:4]: print('node',row['order'],row['position'],'normalCoverage',row['normal'])