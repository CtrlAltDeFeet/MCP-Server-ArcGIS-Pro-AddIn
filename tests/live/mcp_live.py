"""Local stdio MCP acceptance harness. No HTTP or model API connections."""
import argparse, json, os, queue, subprocess, threading, time
from pathlib import Path

HERE = Path(os.environ['ARCGIS_TEST_ROOT']).resolve()
HERE.mkdir(parents=True, exist_ok=True)
SERVER = Path(os.environ['ARCGIS_MCP_SERVER']).resolve()
PROJECT = Path(os.environ['ARCGIS_PROJECT']).resolve()

class Client:
    def __init__(self, project=PROJECT):
        env = dict(os.environ, ARCGIS_PROJECT=str(project), MCP_TRANSPORT='stdio')
        self.p = subprocess.Popen([str(SERVER)], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.PIPE, text=True, encoding='utf-8', env=env,
            creationflags=subprocess.CREATE_NO_WINDOW)
        self.q, self.seq = queue.Queue(), 0
        threading.Thread(target=self.read, daemon=True).start()
        threading.Thread(target=lambda: [None for _ in self.p.stderr], daemon=True).start()
        self.rpc('initialize', dict(protocolVersion='2025-06-18', capabilities={},
            clientInfo=dict(name='local-live-acceptance',version='1.0')))
        self.send(dict(jsonrpc='2.0',method='notifications/initialized'))
    def read(self):
        for line in self.p.stdout:
            self.q.put(json.loads(line))
    def send(self, obj):
        self.p.stdin.write(json.dumps(obj)+'\n'); self.p.stdin.flush()
    def rpc(self, method, params):
        self.seq += 1
        self.send(dict(jsonrpc='2.0',id=self.seq,method=method,params=params))
        while True:
            reply = self.q.get(timeout=660)
            if reply.get('id') == self.seq:
                if 'error' in reply: raise RuntimeError(reply['error'])
                return reply['result']
    def call(self, name, arguments):
        start=time.time()
        result=self.rpc('tools/call',dict(name=name,arguments=arguments))
        # Preserve text and image metadata; never dump base64 to the log.
        for item in result.get('content',[]):
            if item.get('type') == 'image':
                item['base64Length']=len(item.pop('data',''))
        record=dict(tool=name,arguments=arguments,result=result,seconds=round(time.time()-start,2))
        with (HERE/'calls.jsonl').open('a',encoding='utf-8') as f: f.write(json.dumps(record)+'\n')
        return record
    def close(self):
        self.p.stdin.close()
        try: self.p.wait(timeout=5)
        except subprocess.TimeoutExpired: self.p.kill(); self.p.wait()

if __name__=='__main__':
    ap=argparse.ArgumentParser(); ap.add_argument('--catalog',action='store_true')
    ap.add_argument('--calls'); ap.add_argument('--project',default=str(PROJECT)); args=ap.parse_args()
    c=Client(args.project)
    try:
        if args.catalog:
            catalog=c.rpc('tools/list',{})['tools']
            (HERE/'tool-catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
            print(json.dumps([t['name'] for t in catalog]))
        if args.calls:
            for call in json.loads(Path(args.calls).read_text(encoding='utf-8-sig')):
                print(json.dumps(c.call(call['name'],call.get('arguments',{}))),flush=True)
    finally: c.close()
