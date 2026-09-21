"""Wire the owned InGame composition root; leave prefab contents untouched."""
from pathlib import Path
import re, uuid
root=Path('.')
for p in Path('Assets/3.Script').rglob('*.cs'):
    meta=Path(str(p)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
def guid(path): return re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text()).group(1)
catalog=Path('Assets/GameData/Catalog/GameDataCatalog.asset')
dg=guid('Assets/GameData/CSV/Door.csv'); c=catalog.read_text(encoding='utf-8-sig')
if dg not in c: catalog.write_text(c+f'  - {{fileID: 4900000, guid: {dg}, type: 3}}\n',encoding='utf-8')
p=Path('Assets/1.Scene/InGame.unity'); s=p.read_text(encoding='utf-8-sig')
docs={i:(t,b) for t,i,b in re.findall(r'--- !u!(\d+) &(\d+)(?: stripped)?\n(.*?)(?=--- !u!|\Z)',s,re.S)}
gos={re.search(r'  m_Name: (.*)',b).group(1):i for i,(t,b) in docs.items() if t=='1'}
def replace_doc(id,old,new):
 global s
 t,b=docs[str(id)]; assert old in b,(id,old);nb=b.replace(old,new);s=s.replace(b,nb,1);docs[str(id)]=(t,nb)
replace_doc(691565388,'initialMode: 2','initialMode: 1') if 'initialMode: 2' in docs['691565388'][1] else None
replace_doc(691565388,'playerUI: {fileID: 735222006}','playerUI: {fileID: 0}') if 'playerUI: {fileID: 735222006}' in docs['691565388'][1] else None
replace_doc(287827846,'NearClipPlane: 0.3','NearClipPlane: 0.03') if 'NearClipPlane: 0.3' in docs['287827846'][1] else None
legacy=[gos[n] for n in ['Plane','AnomalyTargets','MapRoot','Canvas','TestPlayer','ChangeView'] if n in gos]
for id in legacy:
 if 'm_IsActive: 1' in docs[id][1]:replace_doc(id,'m_IsActive: 1','m_IsActive: 0')
if '900100003' not in docs:
 common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
 new='--- !u!1 &900100001\nGameObject:\n'+common+'  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 900100002}\n  - component: {fileID: 900100003}\n  m_Layer: 0\n  m_Name: InGameCore\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'
 new+='--- !u!4 &900100002\nTransform:\n'+common+'  m_GameObject: {fileID: 900100001}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
 new+='--- !u!114 &900100003\nMonoBehaviour:\n'+common+'  m_GameObject: {fileID: 900100001}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid('Assets/3.Script/InGame/Session/InGameSceneBootstrapper.cs')+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::InGameSceneBootstrapper\n'
 new+='  dataBootstrapper: {fileID: 1100777710}\n  mapRoot: {fileID: 1540734332}\n  player: {fileID: 1312097968}\n  interactor: {fileID: 1312097970}\n  outputCamera: {fileID: 330585545}\n  cameras: {fileID: 691565388}\n  highlightMaterial: {fileID: 2100000, guid: 66924b64d8e96454ba01b7db9a92a433, type: 2}\n  legacyTestObjects:\n'
 new+=''.join('  - {fileID: '+id+'}\n' for id in legacy)
 new+='  controlRoomFloorName: DutyRoom_FloorFinish\n  controlRoomDoorName: DutyRoom_Door_ASSEMBLY\n  outlineOpacity: 0.9\n  ghostOpacity: 0.15\n  outlineWidth: 0.035\n'
 s=s.replace('--- !u!1660057539 &9223372036854775807',new+'--- !u!1660057539 &9223372036854775807')
 s+='  - {fileID: 900100002}\n'
p.write_text(s,encoding='utf-8')
print('InGame wired; disabled legacy roots:',legacy)
