"""Stateful scratch-only acceptance. Run once; inspect evidence before any retry."""
import json
from mcp_live import Client, HERE, PROJECT
c = Client()
def call(name, **args):
    r = c.call(name,args)['result']
    assert not r.get('isError'), r
    value = json.loads(next(x['text'] for x in r['content'] if x['type']=='text'))
    print(name, json.dumps(value), flush=True)
    return value
def denied(tool, **args):
    r=c.call(tool,args)['result']; assert r.get('isError'), r
    print('EXPECTED DENIAL',tool,flush=True)
def count(layer, expected):
    r=call('count_features',layer=layer)
    assert r['count']==expected, r
try:
    info=call('get_project_info')
    assert set(info['bridgePolicy']['enabled'])==set(['Inspect','View','Cartography','Export','Editing','Automation','Python'])
    assert not call('has_edits')['hasEdits']
    count('MCP_Points',3)
    p=call('add_point_features',layer='MCP_Points',features=json.dumps([{'x':0.03,'y':0.03,'attributes':{'Label':'Delta_Test','Value':40}}]))
    count('MCP_Points',4)
    call('update_features',layer='MCP_Points',where="Label = 'Delta_Test'",attributes=json.dumps({'Value':45}))
    rows=call('read_layer_attributes',layer='MCP_Points',where="Label = 'Delta_Test'",fields='OBJECTID,Label,Value')
    assert '45' in json.dumps(rows), rows
    call('save_edits')
    call('delete_features',layer='MCP_Points',where="Label = 'Delta_Test'")
    count('MCP_Points',3)
    call('discard_edits')
    count('MCP_Points',4)
    call('delete_features',layer='MCP_Points',where="Label = 'Delta_Test'")
    call('save_edits'); count('MCP_Points',3)
    denied('delete_features',layer='MCP_Points')
    denied('update_features',layer='MCP_Points',where='1=1',oids='1',attributes='{"Value":999}')
    call('add_polyline_features',layer='MCP_Lines',features=json.dumps([{'vertices':[[0.0,0.0],[0.01,0.01]],'attributes':{'Label':'TestLine','Value':1}}]))
    call('add_polygon_features',layer='MCP_Polygons',features=json.dumps([{'vertices':[[-0.001,-0.001],[0.001,-0.001],[0.001,0.001],[-0.001,0.001],[-0.001,-0.001]],'attributes':{'Label':'TestPolygon','Value':1}}]))
    call('save_edits'); count('MCP_Lines',1); count('MCP_Polygons',1)
    assert not call('has_edits')['hasEdits']
    gp=call('run_gp_tool',tool='analysis.Buffer',parameters=json.dumps([str(HERE/'validation.gdb/MCP_Points'),str(HERE/'validation.gdb/GP_Buffer'),'100 Meters']))
    assert gp.get('success', not gp.get('isFailed',False)), gp
    call('describe_dataset',path=str(HERE/'validation.gdb/GP_Buffer'))
    py=call('execute_python',code="print('MCP Python acceptance')\nresult={'answer': 2+2, 'project': arcpy.mp.ArcGISProject('CURRENT').filePath}")
    assert py['ok'] and py['result']['answer']==4 and py['result']['project'].replace('\\','/').lower()==str(PROJECT).replace('\\','/').lower(), py
    bad=call('execute_python',code="raise ValueError('expected acceptance failure')")
    assert bad['ok'] is False and 'expected acceptance failure' in bad['error'], bad
    good=call('execute_python',code="result={'recovered':True, 'fresh': 'previous_marker' not in globals()}")
    assert good['ok'] and good['result']['recovered'], good
    call('save_project')
    (HERE/'advanced-passed.json').write_text(json.dumps({'editing':True,'geoprocessing':True,'python':True},indent=2))
finally:
    c.close()
