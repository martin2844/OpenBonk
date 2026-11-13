// Bounded heuristic: empty void method with a non-immediate native entry and
// a same-named call somewhere in the exported C# tree. Overloads and unrelated
// types can produce false positives, while virtual and reflective calls can be missed.
const fs=require('fs'), cp=require('child_process');
const root='unity/OpenBonk/Assets/Scripts/Assembly-CSharp';
const paths=cp.execSync(`rg --files ${root} | rg '\\.cs$'`,{encoding:'utf8'}).trim().split('\n');
const src=paths.map(p=>({p,s:fs.readFileSync(p,'utf8')}));
const classes={}; for(const f of src){const cls=f.s.match(/\bclass\s+(\w+)/);if(cls) (classes[cls[1]]??=[]).push(f);}
const methods=JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod;
const fd=fs.openSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so','r');
for(const m of methods){const k=m.Name.match(/(?:^|\.)(\w+)\$\$(\w+)$/);if(!k||!classes[k[1]]||/Test|Gizmos|Validate/.test(k[2]))continue;
 for(const f of classes[k[1]]){if(!new RegExp('\\bvoid\\s+'+k[2]+'\\s*\\([^)]*\\)\\s*\\{\\s*\\}').test(f.s))continue;
 const b=Buffer.alloc(1);fs.readSync(fd,b,0,1,m.Address);if(b[0]===0xc3)continue;
 const re=new RegExp('\\.\\s*'+k[2]+'\\s*\\(');const callsites=src.filter(x=>re.test(x.s));if(callsites.length) console.log(`${k[1]}.${k[2]} 0x${m.Address.toString(16)} ${callsites.length} calls: ${callsites.slice(0,5).map(x=>x.p.split('/').pop()).join(', ')}`);
 }
}
