"""Prepare the reviewed RC3 source ZIP without publishing or changing app binaries.
Python 3 standard library only. Run from any working directory.
"""
from pathlib import Path
import hashlib
import json
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
FROZEN = {'ScreenWisp-Setup.exe': '4ae31749a2b5fbb8f7d801e574efa66d8d7179079971368d0b842ecfb42f553e', 'ScreenWisp-Portable.zip': '5b24a3f736d7bc9c2fad87ee9256697edc328f7422e3a50d7f9c509eff57ad9f'}
APP_HASH = 'ff0960279b6100128e847bc483e52b240fd33a422fd3e30addd4f08885caa745'
TEXT = {'.resx', '.md', '.txt', '.cs', '.xaml', '.csproj', '.slnx', '.config', '.manifest', '.ps1', '.py', '.iss', '.svg'}
def digest(data): return hashlib.sha256(data).hexdigest()

def select_files():
    selected = {
        '.gitignore', '.gitattributes', 'LICENSE', 'LICENSE-SCREENWISP', 'COPYRIGHT.md', 'THIRD-PARTY-NOTICES.md',
        'README.md', 'README.ru.md', 'ScreenWisp.slnx', 'NuGet.Config', 'build.ps1',
        'installer/ScreenWisp.iss', 'installer/screenwisp.installed',
        'tools/BuildRelease.ps1', 'tools/GenerateIcon.ps1',
        'tools/GenerateSocialPreview.ps1', 'tools/PreparePublicSource.py',
        'docs/DEMO-RECORDING.md', 'docs/LOCALIZATION.md', 'docs/DEVELOPMENT.md',
        'docs/PUBLIC-ARCHITECTURE.md', 'docs/RELEASE-VALIDATION.md',
        'docs/screenshots/brush-popup.png', 'docs/screenshots/settings.png',
        'docs/screenshots/brush-popup.en-US.png', 'docs/screenshots/brush-popup.ru-RU.png',
        'docs/screenshots/settings.en-US.png', 'docs/screenshots/settings.ru-RU.png',
        'release/rc3-about/RELEASE-NOTES.md', 'publication/public-source-files.txt',
    }
    for folder in ('src', 'tests'):
        for path in (ROOT/folder).rglob('*'):
            rel = path.relative_to(ROOT)
            if path.is_file() and not {'bin','obj'}.intersection(rel.parts):
                selected.add(rel.as_posix())
    for path in (ROOT/'branding').rglob('*'):
        if path.is_file() and path.suffix.lower() in {'.svg','.png','.ico','.md'}:
            selected.add(path.relative_to(ROOT).as_posix())
    # Explicit publication document names prevent accidental private additions.
    for name in ('README.md','GITHUB-SETUP.md','PUBLISH-CHECKLIST.md'):
        selected.add('publication/'+name)
    return sorted(selected)

def scan(names):
    patterns = {
        'private-key': r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
        'github-token': r'\bgh[pousr]_[A-Za-z0-9]{30,}\b',
        'github-fine-token': r'\bgithub_pat_[A-Za-z0-9_]{40,}\b',
        'api-key': r'\bsk-(?:proj-)?[A-Za-z0-9_-]{35,}\b',
        'aws-access-key': r'\bAKIA[A-Z0-9]{16}\b',
        'concrete-user-path': r"(?i)[A-Z]:[\\/]+Users[\\/]+(?!Public[\\/])[^\\/\s\"'<>]+[\\/]",
        'machine-name': r'\bDESKTOP-[A-Z0-9]{5,}\b',
    }
    findings=[]
    count=0
    for name in names:
        path=ROOT/name
        if path.is_symlink() or not path.is_file():
            raise RuntimeError('Missing or symlink source file: '+name)
        if path.suffix.lower() in {'.exe','.dll','.pdb','.log','.evtx','.dmp','.pfx','.p12','.key'}:
            raise RuntimeError('Forbidden public source extension: '+name)
        if path.suffix.lower() in TEXT or path.name in {'LICENSE','.gitignore','.gitattributes'}:
            content=path.read_text(encoding='utf-8-sig')
            count+=1
            for kind,pattern in patterns.items():
                for match in re.finditer(pattern,content):
                    findings.append({'file':name,'line':content.count('\n',0,match.start())+1,'kind':kind})
    if findings:
        raise RuntimeError('Public source audit failed (values withheld): '+json.dumps(findings))
    return count

def main():
    release=ROOT/'release/rc3-about'
    for name,expected in FROZEN.items():
        if digest((release/name).read_bytes()) != expected:
            raise RuntimeError('Frozen RC3 package changed or missing: '+name)
    license_bytes=(ROOT/'LICENSE').read_bytes()
    if b'Copyright (c) 2026 atman-33' not in license_bytes:
        raise RuntimeError('Original MIT copyright notice missing')
    with zipfile.ZipFile(release/'ScreenWisp-Portable.zip') as package:
        if digest(package.read('ScreenWisp.exe')) != APP_HASH:
            raise RuntimeError('Portable EXE is not the accepted RC3')
        if package.read('LICENSE') != license_bytes:
            raise RuntimeError('Portable original LICENSE differs')
    names=select_files()
    (ROOT/'publication/public-source-files.txt').write_text('\n'.join(names)+'\n',encoding='utf-8')
    scanned=scan(names)
    target=release/'ScreenWisp-Source.zip'
    # Deterministic metadata: no username, machine path or file modification times.
    with zipfile.ZipFile(target,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
        for name in names:
            info=zipfile.ZipInfo(name,date_time=(2000,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            info.external_attr=0o100644 << 16
            archive.writestr(info,(ROOT/name).read_bytes(),compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    with zipfile.ZipFile(target) as archive:
        if archive.testzip() is not None or archive.namelist()!=names:
            raise RuntimeError('Source ZIP integrity/file-list mismatch')
        for name in names:
            if archive.read(name)!=(ROOT/name).read_bytes():
                raise RuntimeError('Source ZIP bytes mismatch: '+name)
    hashes={**FROZEN,'ScreenWisp-Source.zip':digest(target.read_bytes())}
    (release/'SHA256SUMS.txt').write_text(''.join(value+'  '+name+'\n' for name,value in hashes.items()),encoding='utf-8')
    report={'scope':'allowlisted public source files, not future Git history',
            'files':len(names),'textFilesScanned':scanned,'secretOrPersonalPathFindings':0,
            'sourceArchiveBytes':target.stat().st_size,'sha256':hashes,
            'frozenSetupPortableUnchanged':True,'portableAcceptedRC3Exe':True,
            'originalMitLicenseMatchesPortable':True,'sourceZipIntegrity':True,
            'limits':['Pattern scanning is not exhaustive secret detection.',
                      'Image contents require human review.',
                      'No runtime tests were performed by this packaging script.',
                      'This script does not create or publish Git history.']}
    destination=ROOT/'diagnostics/launch-readiness'
    destination.mkdir(parents=True,exist_ok=True)
    (destination/'source-audit.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':
    main()
