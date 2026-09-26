"""Create only synthetic data in this new, isolated acceptance workspace."""
from pathlib import Path
import arcpy, json, os
root=Path(os.environ['ARCGIS_TEST_ROOT']).resolve()
root.mkdir(parents=True, exist_ok=True)
gdb=root/'validation.gdb'
if gdb.exists(): raise RuntimeError('Scratch database already exists; refusing to replace it')
arcpy.management.CreateFileGDB(str(root),gdb.name)
for name,kind in [('MCP_Points','POINT'),('MCP_Lines','POLYLINE'),('MCP_Polygons','POLYGON')]:
    fc=str(gdb/name)
    arcpy.management.CreateFeatureclass(str(gdb),name,kind,spatial_reference=arcpy.SpatialReference(4326))
    arcpy.management.AddField(fc,'Label','TEXT',field_length=80)
    arcpy.management.AddField(fc,'Value','LONG')
with arcpy.da.InsertCursor(str(gdb/'MCP_Points'),['SHAPE@XY','Label','Value']) as cur:
    for xy,label,value in [((0.0,0.0),'Alpha',10),((0.01,0.01),'Beta',20),((0.02,0.02),'Gamma',30)]:
        cur.insertRow([xy,label,value])
print(json.dumps({'gdb':str(gdb),'points':3,'lines':0,'polygons':0}))
