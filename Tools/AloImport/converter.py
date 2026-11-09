"""Owned background Blender conversion; never contacts or resets a shared session."""
import hashlib
import json
import os
import subprocess
import uuid
from pathlib import Path

from source_audit import SourceError, assembly_instances, bone_world_matrices, resolve_material_policy


def assembly_manifest(recipe,audit,report,textures,job_dir):
    job_dir=Path(job_dir)
    converted={piece['pieceId']:piece for piece in report['pieces']}
    descriptions={piece['id']:piece for piece in recipe['pieces']}
    pieces=[]
    for source in audit['pieces']:
        piece_id=source['pieceId']
        mapping=converted[piece_id]['sourceMap']
        bone_names={row['index']:row['exportedName'] for row in mapping['bones']}
        mesh_names={row['index']:row for row in mapping['meshes']}
        world=bone_world_matrices(source['bones'])
        meshes=[]
        for mesh in source['meshes']:
            exported=mesh_names[mesh['index']]
            slots=[]
            for slot in mesh['materials']:
                policy=resolve_material_policy(recipe,piece_id,mesh['index'],slot)
                named=next(row for row in exported['slots'] if row['index']==slot['slot'])
                slots.append({'index':slot['slot'],'signature':slot['signature'],'shader':slot['shader'],
                    'properties':slot['properties'],'sourceName':named['exportedMaterialName'],
                    'triangleCount':slot['triangleCount'],'vertexCount':slot['vertexCount'],
                    'materialGuid':policy.get('targetGuid'),'policy':policy})
            meshes.append({'index':mesh['index'],'objectIndex':mesh['object_index'],'boneIndex':exported['boneIndex'],
                'sourceName':mesh['name'],'exportedName':exported['exportedName'],'hidden':mesh['hidden'],
                'collision':mesh['collision'],'shading':exported['shading'],'slots':slots})
        pieces.append({'id':piece_id,'purpose':descriptions[piece_id]['purpose'],'sourceSha256':source['sha256'],
            'transportRigName':converted[piece_id]['transportRigName'],
            'fbx':f'staging/{piece_id}.fbx','blend':f'staging/{piece_id}.blend',
            'sourceNormals':f'staging/{piece_id}.source-normals.json',
            'bones':[{'index':bone['index'],'sourceName':bone['name'],'exportedName':bone_names[bone['index']],
                'parentIndex':None if bone['parent_index']==0xffffffff else bone['parent_index'],
                'localMatrix':[bone['matrix'][:4],bone['matrix'][4:8],bone['matrix'][8:12],[0,0,0,1]],
                'worldMatrix':world[bone['index']]} for bone in source['bones']],
            'meshes':meshes,'proxies':source['proxies']})
    instances=assembly_instances(recipe,audit['pieces'])
    return {'schemaVersion':1,'modelId':recipe['modelId'],'coordinateSystem':'alo-source',
        'matrixConvention':'row-major-column-vectors','sourceUnit':'original-alo-units',
        'fbxExport':{'axisForward':'-Z','axisUp':'Y','applyUnitScale':True,'assemblyTransformApplied':False},
        'pieces':pieces,'instances':instances,'roots':[row['id'] for row in instances if row['parentInstance'] is None],
        'levels':recipe.get('station',{}).get('levels',[]),
        'textures':[{'source':key,'path':str(Path(row['path']).relative_to(job_dir)).replace('\\','/'),
            'sha256':row['sha256'],'size':row['size']} for key,row in textures.items()],
        'sourceXml':audit.get('xml',{'units':[],'checks':{'unitProvenance':'not-run','attachmentCoverage':'not-run'}}),
        'checks':{'pieceConversion':report['checks'],'instanceTransforms':'derived-from-original-binary',
                  'unityAssembly':'not-run','proxyAnimation':'not-run'}}


def convert_job(job_dir, frozen):
    from contextlib import ExitStack
    import time
    from storage import PipelineError, digest, exclusive_lock, is_alive, read_json, write_json
    job_dir = Path(job_dir).resolve()
    config = frozen['config']
    blender = config.get('blender')
    if not blender or not Path(blender).is_file():
        raise PipelineError('TOOL_UNAVAILABLE', 'Configure an existing blender executable', 4)
    audit = read_json(job_dir / 'SourceAudit.json')
    process_path=job_dir/'BlenderProcess.json'
    if process_path.exists():
        previous=read_json(process_path)
        if previous['state'] in {'starting','running'} and is_alive(previous['owner']):
            raise PipelineError('OPERATION_UNRESOLVED','Owned Blender process is still active',6)
        if previous['state']=='starting':
            raise PipelineError('OPERATION_UNRESOLVED','Blender launch outcome needs explicit process reconciliation',6)
    for item in audit['files']:
        path=Path(item['path'])
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']:
            raise PipelineError('INPUT_CHANGED','Source changed: '+str(path),3)
    addon=Path(config.get('alamoAddon') or Path(os.environ.get('APPDATA',''))/'Blender Foundation/Blender/3.6/scripts/addons/io_alamo_tools')
    if not (addon/'import_alo.py').is_file():
        raise PipelineError('TOOL_UNAVAILABLE','Configure alamoAddon as io_alamo_tools directory',4)
    inputs = {'recipe': frozen['recipe'], 'audit': audit,
              'blender': hashlib.sha256(Path(blender).read_bytes()).hexdigest(),
              'alamo': {p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in addon.glob('*.py')},
              'pipeline': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in
                           (Path(__file__), Path(__file__).with_name('blender_worker.py'), Path(__file__).with_name('source_audit.py'), Path(__file__).with_name('fidelity.py'))}}
    fingerprint = hashlib.sha256(json.dumps(inputs, sort_keys=True).encode()).hexdigest()
    marker = job_dir / 'ConversionComplete.json'
    manifest=job_dir/'ConversionManifest.json'
    def publish(report):
        if not report.get('outputs') or any(not Path(row['path']).resolve().is_relative_to(job_dir/'staging')
                or not Path(row['path']).is_file() or hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()!=row['sha256']
                for row in report['outputs']):
            raise PipelineError('VERIFICATION_FAILED','Conversion outputs must be complete, hashed files in owned staging',5)
        write_json(manifest,{'schemaVersion':1,'jobId':frozen['jobId'],'inputHash':digest(frozen),
            'state':'succeeded','fingerprint':fingerprint,
            'outputs':[{'path':str(Path(row['path']).relative_to(job_dir)).replace('\\','/'),'sha256':row['sha256']} for row in report['outputs']],
            'checks':report['checks']})
    if marker.exists():
        cached = read_json(marker)
        if cached['fingerprint'] == fingerprint and cached['report']['outputs'] and all(
                Path(r['path']).resolve().is_relative_to(job_dir/'staging') and Path(r['path']).is_file() and
                hashlib.sha256(Path(r['path']).read_bytes()).hexdigest() == r['sha256'] for r in cached['report']['outputs']):
            publish(cached['report'])
            return cached['report']
        marker.unlink()
    manifest.unlink(missing_ok=True)
    capacity = config.get('blenderWorkers', 2)
    if not isinstance(capacity, int) or isinstance(capacity, bool) or capacity < 1:
        raise PipelineError('INVALID_CONFIG', 'blenderWorkers must be a positive integer', 2)
    coordinator = job_dir.parents[1] / 'Coordinator'
    with exclusive_lock(coordinator/'metadata.lock'):
        capacity_file=coordinator/'Capacity.json'
        if capacity_file.exists() and read_json(capacity_file)['blenderWorkers']!=capacity:
            raise PipelineError('INVALID_CONFIG','blenderWorkers differs from checkout Capacity.json',2)
        write_json(capacity_file,{'blenderWorkers':capacity})
    while True:
        for slot in range(capacity):
            locks = ExitStack()
            try:
                locks.enter_context(exclusive_lock(coordinator / f'blender-{slot}.lock', wait_seconds=0))
            except PipelineError as error:
                locks.close()
                if error.code != 'EDITOR_BUSY':
                    raise
                continue
            with locks:
                slot_record=coordinator/f'blender-{slot}.json'
                if slot_record.exists():
                    previous=read_json(slot_record)
                    if previous['state'] in {'starting','running'} and is_alive(previous['owner']):
                        continue
                    if previous['state']=='starting':
                        raise PipelineError('OPERATION_UNRESOLVED','Blender launch outcome needs reconciliation: '+str(slot_record),6)
                try:
                    report = convert(job_dir, frozen['recipe'], audit, blender,
                                     alamo_addon=str(addon), process_record=slot_record)
                except SourceError as error:
                    exit_code=(6 if error.code=='OPERATION_UNRESOLVED' else 3 if error.code=='INPUT_CHANGED'
                               else 4 if error.code in {'SOURCE_MISSING','UNSUPPORTED_CAPABILITY'} else 5)
                    raise PipelineError(error.code, str(error), exit_code) from error
                for item in audit['files']:
                    path=Path(item['path'])
                    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']:
                        raise PipelineError('INPUT_CHANGED','Source changed during conversion: '+str(path),3)
                current_addon={path.name:hashlib.sha256(path.read_bytes()).hexdigest() for path in addon.glob('*.py')}
                if current_addon!=inputs['alamo']:
                    raise PipelineError('INPUT_CHANGED','ALAMO changed during conversion',3)
                write_json(marker, {'fingerprint':fingerprint, 'report':report})
                publish(report)
                return report
        if any(operation.get('cancelRequested') for operation in read_json(job_dir / 'Journal.json')['operations'].values()):
            raise PipelineError('CANCELLED', 'Conversion cancelled while waiting for capacity', 4)
        time.sleep(.1)


def convert(job_dir, recipe, audit, blender, timeout_seconds=600, alamo_addon=None, process_record=None):
    from storage import owner, read_json, write_json
    from PIL import Image
    job_dir = Path(job_dir).resolve()
    output = job_dir / 'staging'
    output.mkdir(exist_ok=True)
    texture_dir = output / 'Textures'
    texture_dir.mkdir(exist_ok=True)
    textures = {}
    for key, item in audit['textures'].items():
        source = Path(item['path'])
        if hashlib.sha256(source.read_bytes()).hexdigest() != item['sha256']:
            raise SourceError('INPUT_CHANGED', str(source))
        name = item['sha256'] + '.png'
        target = texture_dir / name
        with Image.open(source) as original:
            pixels = original.convert('RGBA')
            pixels.save(target)
            with Image.open(target) as decoded:
                if decoded.convert('RGBA').tobytes() != pixels.tobytes():
                    raise SourceError('VERIFICATION_FAILED', 'Lossless decode failed: '+str(source))
            textures[key] = {'path':str(target),'size':list(pixels.size),'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
    nonce = str(uuid.uuid4())
    process_record=Path(process_record) if process_record else job_dir/'BlenderSlot.json'
    process_state={'nonce':nonce,'job':str(job_dir),'owner':owner(),'state':'starting'}
    write_json(process_record,process_state)
    write_json(job_dir/'BlenderProcess.json',process_state)
    spec = {'nonce':nonce,'output':str(output),'recipe':recipe,'audit':audit,'textures':textures,
            'alamoAddon':alamo_addon,'processRecord':str(process_record)}
    request = job_dir / 'ConversionRequest.json'
    request.write_text(json.dumps(spec),encoding='utf-8')
    script = Path(__file__).with_name('blender_worker.py')
    command = [str(blender),'--background','--factory-startup','--python-exit-code','1','--python',str(script),'--',str(request)]
    log = job_dir / 'Blender.log'
    with log.open('wb') as stream:
        try:
            process = subprocess.Popen(command,stdout=stream,stderr=subprocess.STDOUT,
                creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
        except OSError:
            process_state['state']='failed'
            write_json(process_record,process_state)
            write_json(job_dir/'BlenderProcess.json',process_state)
            raise
        try:
            result = process.wait(timeout=timeout_seconds)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            process_state['state']='failed'
            write_json(process_record,process_state)
            write_json(job_dir/'BlenderProcess.json',process_state)
            raise SourceError('CONVERSION_TIMEOUT', 'Owned Blender stopped; see '+str(log))
    process_state=read_json(process_record)
    process_state['state']='succeeded' if result==0 else 'failed'
    write_json(process_record,process_state)
    write_json(job_dir/'BlenderProcess.json',process_state)
    report_path = output / 'ConversionReport.json'
    if result != 0 or not report_path.is_file():
        failure=output/'WorkerFailure.json'
        if failure.exists():
            error=read_json(failure)
            if error.get('nonce')==nonce:
                raise SourceError(error['code'],error['summary']+'; see '+str(log))
        raise SourceError('VERIFICATION_FAILED','Blender conversion failed; see '+str(log))
    report = json.loads(report_path.read_text())
    if report['nonce'] != nonce or report['pid'] != process.pid:
        raise SourceError('OPERATION_UNRESOLVED','Blender ownership report mismatch')
    for row in report['outputs']:
        path = Path(row['path'])
        if not path.resolve().is_relative_to(output) or hashlib.sha256(path.read_bytes()).hexdigest() != row['sha256']:
            raise SourceError('VERIFICATION_FAILED','Invalid conversion output: '+str(path))
    assembly=output/'AssemblyManifest.json'
    write_json(assembly,assembly_manifest(recipe,audit,report,textures,job_dir))
    report['assemblyManifest']='staging/AssemblyManifest.json'
    report['outputs'].append({'path':str(assembly),'sha256':hashlib.sha256(assembly.read_bytes()).hexdigest()})
    write_json(report_path,report)
    return report
