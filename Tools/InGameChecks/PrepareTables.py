"""Apply the approved deployment switches without deleting future content IDs."""
import csv
from pathlib import Path
base = Path('Assets/GameData/CSV')
def read(name):
    with (base / (name + '.csv')).open(encoding='utf-8-sig', newline='') as f:
        reader = csv.DictReader(f)
        return reader.fieldnames, list(reader)
def save(name, headers, rows):
    with (base / (name + '.csv')).open('w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, headers, lineterminator='\n'); writer.writeheader(); writer.writerows(rows)
h, quests = read('Quest')
disabled = {9:'시계 프리팹 연결 미확정', 10:'I_7_2만 상호작용 허용: I_7_3 제외', 17:'시계 프리팹 연결 미확정', 28:'영정사진 프리팹 연결 미확정'}
for row in quests:
    row['Enabled'] = '0' if int(row['ID']) in disabled else '1'
    row['DisabledReason'] = disabled.get(int(row['ID']), '')
save('Quest', h, quests)
h, schedules = read('Schedule')
for row in schedules:
    candidates = [q for q in quests if q['Enabled'] == '1' and q['ScheduleID'] == row['ID']]
    row.update(Enabled=int(bool(candidates)), DrawCount=int(bool(candidates)), CandidateCount=len(candidates), WeightSum=sum(int(q['Weight']) for q in candidates))
save('Schedule', h, schedules)
h, rounds = read('Round')
for row in rounds:
    slots = [s for s in schedules if s['Enabled'] == 1 and s['RoundID'] == row['ID']]
    fixed = sum(any(q['Enabled'] == '1' and q['ScheduleID'] == s['ID'] and q['SpawnType'] == '0' for q in quests) for s in slots)
    row.update(ScheduleCount=len(slots), FixedCount=fixed, RandomCount=len(slots)-fixed)
save('Round', h, rounds)
h, doors = read('Door')
for row in doors:
    if not row['SceneBinding'].startswith('Interection_Obj/'):
        row['SceneBinding'] = 'Interection_Obj/' + row['SceneBinding']
    if row['SceneBinding'].endswith('Outer_Coffin_L_Door'): row['HingeSide'] = 1
    if row['SceneBinding'].endswith('Outer_Coffin_R_Door'): row['HingeSide'] = -1
save('Door', h, doors)
print('Active quests:', sum(q['Enabled'] == '1' for q in quests), 'Round slots:', [r['ScheduleCount'] for r in rounds])
