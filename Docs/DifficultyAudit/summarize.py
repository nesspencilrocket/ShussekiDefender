import csv,json
from pathlib import Path
base=Path('Temp/DifficultyAudit')
rows=[]
for name in ['main1','main2']:
    p=base/name/'results.csv'
    if p.exists():
        with p.open(encoding='utf-8-sig',newline='') as f:
            for r in csv.DictReader(f):
                if not r.get('id') or r.get('errors') is None:continue
                for k in ['stage','seed','fps','passed','limit','kills','gold','towers','upgrades','skills','spawned','errors']:
                    r[k]=int(r[k])
                for k in ['elapsed','delay','wallSeconds','minDelta','maxDelta']:
                    r[k]=float(r[k])
                r['clear']=r['clear'].lower()=='true'
                r['batch']=name
                rows.append(r)
print('completed',len(rows),'errors',sum(r['errors'] for r in rows),'wall',round(sum(r['wallSeconds'] for r in rows),1))
policies=['InitialNormal','InitialMixed','ReactiveNormal','ReactiveMixed','ReactiveMixedSkill','SkillOnly']
for p in policies:
    data=[r for r in rows if r['policy']==p and r['delay']==5 and r['fps']==60 and '-Normal' not in r['id']]
    a=[]
    for s in range(1,7):
        v=[r for r in data if r['stage']==s]
        a.append(f'{sum(r["clear"] for r in v)}/{len(v)}')
    print(p, ' '.join(a))
print('early decisions')
for r in rows:
    if 'EarlyMixedSkill' in r['id']:
        print(r['id'],r['clear'],r['passed'],r['limit'],r['elapsed'])
print('stage1 layouts')
for r in rows:
    if r['stage']==1 and '-Normal' in r['id']:
        print(r['id'],r['clear'],r['passed'],r['limit'],r['elapsed'])
(base/'partial-summary.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')