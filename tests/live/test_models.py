import json,time
from mcp_live import Client,HERE
c=Client(); tb=str(HERE/'Acceptance.atbx'); mn='ScratchBuffer'
def call(name,/,**args):
    r=c.call(name,args)['result']; assert not r.get('isError'),r
    txt=next(x['text'] for x in r['content'] if x['type']=='text')
    try: value=json.loads(txt)
    except ValueError: value=txt
    print(name,json.dumps(value),flush=True); return value
try:
    call('ping'); assert call('echo',text='local MCP acceptance')=='echo: local MCP acceptance'
    call('set_basemap',basemap='None',map='MCP_Secondary')
    call('bridge_op',op='pro.getActiveMapName',argsJson='{}')
    call('create_toolbox',name='MCP Acceptance',path=tb)
    definition={'name':mn,'description':'Synthetic acceptance buffer','inputs':[
        {'name':'InPoints','type':'GPFeatureLayer','default':str(HERE/'validation.gdb/MCP_Points')},
        {'name':'Distance','type':'GPLinearUnit','default':'100 Meters'}],
        'steps':[{'name':'Buffer Step','tool':'analysis.Buffer','parameters':{
            'in_features':{'ref':'InPoints'},'out_feature_class':{'output':'BufferOut','type':'DEFeatureClass','parameter':True},
            'buffer_distance_or_field':{'ref':'Distance'},'dissolve_option':'NONE'}}]}
    call('create_model',toolboxPath=tb,definition=json.dumps(definition))
    before=call('describe_model',toolboxPath=tb,modelName=mn)
    call('set_parameter_default',toolboxPath=tb,modelName=mn,parameterName='Distance',defaultValue='150 Meters')
    call('set_step_parameter',toolboxPath=tb,modelName=mn,stepName='Buffer Step',paramKey='dissolve_option',paramValue='"NONE"')
    current=call('describe_model',toolboxPath=tb,modelName=mn)
    (HERE/'model-description.json').write_text(json.dumps(current,indent=2))
    definition['inputs'][1]['default']='200 Meters'
    call('update_model',toolboxPath=tb,modelName=mn,definition=json.dumps(definition))
    call('list_models',toolboxPath=tb)
    sync=call('run_model',toolboxPath=tb,modelName=mn,variableOverrides=json.dumps({'BufferOut':str(HERE/'validation.gdb/Model_Buffer')}))
    assert sync.get('success',True),sync
    dataset=call('describe_dataset',path=str(HERE/'validation.gdb/Model_Buffer'))
    assert dataset['rowCount']==3 and dataset['geometry']['geometryType']=='Polygon',dataset
    job=call('start_run_model',toolboxPath=tb,modelName=mn,parameters='{"Distance":"250 Meters"}',variableOverrides=json.dumps({'BufferOut':str(HERE/'validation.gdb/Async_Buffer')}))
    for i in range(12):
        status=call('get_run_status',jobId=job['jobId'])
        if status.get('endedUtc'): break
        time.sleep(10)
    assert status['status']=='succeeded',status
    dataset=call('describe_dataset',path=str(HERE/'validation.gdb/Async_Buffer'))
    assert dataset['rowCount']==3,dataset
    call('save_project')
    (HERE/'models-passed.json').write_text(json.dumps({'sync':True,'async':True,'editingDefinition':True},indent=2))
finally: c.close()
