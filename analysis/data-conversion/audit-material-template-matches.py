#!/usr/bin/env python3
"""Compare material byte positions across a private template and Steam copies.

Names, sizes and byte similarity are matching heuristics. The report contains
metadata and counts, not asset bytes; it does not establish provenance or rights.
"""
import argparse
from collections import Counter,defaultdict
import hashlib,importlib.util,json,re,zipfile
from pathlib import Path
import UnityPy
spec=importlib.util.spec_from_file_location('particle_audit',Path(__file__).with_name('audit-particle-template-matches.py'))
audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)

def material_objects(data,reference=None):
    items=list(UnityPy.load(data).objects)
    refs=items if reference is None else list(UnityPy.load(reference).objects)
    refs_by_id={o.path_id:o for o in refs}
    if len(refs_by_id)!=len(refs):raise ValueError('duplicate reference ID')
    out=defaultdict(list);skipped=Counter()
    for o in items:
        if o.type.name!='Material':continue
        r=refs_by_id.get(o.path_id)
        if r is None or r.type.name!='Material' or r.byte_size!=o.byte_size:
            skipped['layout']+=1;continue
        try:name=r.read_typetree()['m_Name']
        except (ValueError,KeyError,TypeError):skipped['name']+=1;continue
        raw=o.get_raw_data()
        if len(raw)!=o.byte_size:raise ValueError('unexpected material size')
        out[(name,len(raw))].append((o.path_id,o.byte_start,raw))
    return out,skipped

def digest(data):return hashlib.sha256(data).hexdigest()

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('reference',type=Path);p.add_argument('linux_data',type=Path);p.add_argument('windows_zip',type=Path);p.add_argument('report',type=Path);p.add_argument('--asset',choices=('sharedassets1.assets','sharedassets2.assets'),required=True);a=p.parse_args()
    if a.report.exists() or a.report.is_symlink():p.error('report must be new')
    base=a.base.read_bytes();ref_raw=a.reference.read_bytes()
    if digest(base)!='d1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431' or digest(ref_raw)!='c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5':raise ValueError('unexpected template or reference')
    target=audit.template(a.base,a.asset)
    with zipfile.ZipFile(a.reference) as z:
        parts=[]
        for i in z.infolist():
            m=re.fullmatch(r'assets/bin/Data/'+re.escape(a.asset)+r'\.split(\d+)',i.filename)
            if m:parts.append((int(m.group(1)),i))
        parts.sort(key=lambda x:x[0])
        if not parts or [n for n,_ in parts]!=list(range(len(parts))):raise ValueError('missing reference splits')
        reference=b''.join(z.read(i) for _,i in parts)
    linux=(a.linux_data/a.asset).read_bytes()
    with zipfile.ZipFile(a.windows_zip) as z:
        matches=[i for i in z.infolist() if i.filename=='Megabonk_Data/'+a.asset]
        if len(matches)!=1:raise ValueError('missing Windows asset')
        windows=z.read(matches[0])
    target_objects,ts=material_objects(target,reference);left,ls=material_objects(linux);right,rs=material_objects(windows)
    counts=Counter();rows=[]
    for (name,size),candidates in sorted(target_objects.items()):
        for ident,pos,before in candidates:
            counts['target']+=1; l=left[(name,size)];w=right[(name,size)]
            if not l or not w:counts['missing']+=1;continue
            def choose(options):return max(options,key=lambda o:(sum(x!=0 and x==y for x,y in zip(before,o[2])),-o[0]))
            lc,wc=choose(l),choose(w)
            n=sum(x!=0 and x==y==z for x,y,z in zip(before,lc[2],wc[2]))
            counts['matched']+=1;counts['equal_nonzero']+=n
            rows.append({'name':name,'size':size,'target_id':ident,'target_offset':pos,'linux_id':lc[0],'windows_id':wc[0], 'linux_candidates':len(l),'windows_candidates':len(w),'equal_nonzero':n})
    report={'asset':a.asset,'base_sha256':digest(base),'reference_sha256':digest(ref_raw),'linux_sha256':digest(linux),'windows_sha256':digest(windows),'counts':dict(counts),'skipped':{'target':dict(ts),'linux':dict(ls),'windows':dict(rs)},'objects':rows,'limitations':'Candidate matches by name, length and similarity are heuristic; only exact three-way byte equality is counted and this does not establish provenance, rights or semantic identity.'}
    with a.report.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    print(a.asset,dict(counts),'skipped',dict(ts),dict(ls),dict(rs))
if __name__=='__main__':main()
