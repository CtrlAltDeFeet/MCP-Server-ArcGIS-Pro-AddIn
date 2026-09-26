from pathlib import Path
import urllib.request,json,datetime,re,hashlib,gzip
root=Path(__file__).resolve().parents[1]
def fetch(url):
    with urllib.request.urlopen(url,timeout=25) as r:
        data=r.read()
        if data[:2]==b'\x1f\x8b':data=gzip.decompress(data)
        return json.loads(data)
index=fetch('https://api.nuget.org/v3/index.json')
endpoint=next(x['@id'] for x in index['resources'] if x['@type'].startswith('VulnerabilityInfo/'))
pages=fetch(endpoint)
advisories={}
for page in pages:
    for name,items in fetch(page['@id']).items():advisories.setdefault(name,[]).extend(items)
packages={}
for lock in root.rglob('packages.lock.json'):
    for framework,deps in json.loads(lock.read_text())['dependencies'].items():
        for name,value in deps.items():
            if 'resolved' in value:packages[(name.lower(),value['resolved'])]=value.get('contentHash')
def version(s):
    # Resolved dependencies are stable releases except the old unchanged libraries;
    # compare NuGet semver release parts and prerelease precedence explicitly.
    main,_,pre=s.partition('-')
    nums=tuple(int(x) for x in main.split('.'))
    return nums+(0,)*(4-len(nums)), not bool(pre), tuple((0,int(x)) if x.isdigit() else (1,x.lower()) for x in pre.split('.')) if pre else ()
def includes(ver,span):
    v=version(ver)
    inner=span[1:-1]
    if ',' not in inner:return v==version(inner)
    lo,hi=[x.strip() for x in inner.split(',',1)]
    return (not lo or v>version(lo) or (span[0]=='[' and v==version(lo))) and (not hi or v<version(hi) or (span[-1]==']' and v==version(hi)))
findings=[]
for name,ver in packages:
    for advisory in advisories.get(name,[]):
        if includes(ver,advisory['versions']):findings.append({'package':name,'version':ver,**advisory})
report={'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source':endpoint,'packages':[{'name':k[0],'version':k[1],'contentHash':v} for k,v in sorted(packages.items())],'findings':findings,'limitations':'NuGet known-vulnerability feed only; not a full source audit.'}
(root/'review/dependency-audit-current.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'packages':len(packages),'findings':findings}))
if findings:raise SystemExit(1)
