// Search direct x86 rel32 CALLs to the original BossSummoner constructor.
// A byte-level search can include false positives inside instructions/data and
// cannot rule out indirect calls; annotate owners via nearest known IL2CPP RVA.
const fs=require('fs');
const path='/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const buf=fs.readFileSync(path);
const methods=JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod.sort((a,b)=>a.Address-b.Address);
const target=methods.find(x=>x.Name==='Assets.Scripts.Game.Spawning.New.Summoners.BossSummoner$$.ctor').Address;
for(let i=0;i+4<buf.length;i++) if(buf[i]===0xe8 && i+5+buf.readInt32LE(i+1)===target){
 let lo=0,hi=methods.length;while(lo<hi){let mid=(lo+hi)>>>1;if(methods[mid].Address<=i)lo=mid+1;else hi=mid;}
 const owner=methods[lo-1];console.log(`possible call at 0x${i.toString(16)} nearest=${owner?owner.Name:'unknown'}+0x${owner?(i-owner.Address).toString(16):'?'}`);
}
console.log(`target=0x${target.toString(16)}; search covers literal bytes, not indirect calls or instruction validity`);
