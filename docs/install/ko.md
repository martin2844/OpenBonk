# OpenBonk 베타 설치 가이드

> 영어에서 기계 번역. 불분명한 지침을 신고해 주세요.

> OpenBonk는 테스트 중인 비공식 커뮤니티 포트입니다. 각 테스터는 호환 가능한 Megabonk Steam 사본이 필요합니다.

## 필요한 것

- Android 8 이상을 실행하는 64비트 Android 기기. 베타 버전은 Android 15가 설치된 Retroid Pocket 5 및 iPlay 70 mini Pro에서 테스트되었습니다. 다른 장치와의 호환성은 아직 확립되지 않았습니다.
- 컴퓨터에 Megabonk의 호환 가능한 Steam 설치. 런처는 지원되는 특정 파일을 확인합니다. 다른 게임 업데이트는 호환되지 않을 수 있습니다.
- ZIP, 준비 및 게임 데이터를 위한 추가 여유 공간. 요구 사항은 장치에 따라 다릅니다. 준비에는 몇 분이 소요될 수 있습니다(iPlay 70 mini Pro 테스트 1회에 약 4분 소요).
- 베타 패키지의 `OpenBonk.apk`입니다. 설치를 교체하기 전에 기존 테스트 진행 상황을 백업하십시오. 다르게 서명된 APK에는 별도의 설치가 필요할 수 있습니다.

## 베타 패키지 받기

[베타 릴리스](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta)에서 `OpenBonk-beta.zip`를 다운로드하고 컴퓨터에 추출하세요. 여기에는 `OpenBonk.apk`, 설치 가이드 및 `tools` 폴더가 포함되어 있습니다. ZIP 도우미가 필요한 경우 전체 `tools` 폴더를 유지하세요. APK는 설치 프로그램입니다. 자신의 Steam 사본에서 별도로 비공개 ZIP을 생성해야 합니다.

## 게임 데이터 ZIP 만들기

1. Steam에서 Megabonk의 속성을 열고 **설치된 파일 → 찾아보기**를 선택하세요.
2. `Megabonk_Data` 폴더를 찾으세요. Windows에서는 마우스 오른쪽 버튼을 클릭하고 **보내기 → 압축(zip) 폴더**를 선택합니다. Windows 11에서는 **추가 옵션 표시** 아래에 있을 수 있습니다. Linux에서는 파일 관리자의 압축 옵션을 사용하고 ZIP을 선택하세요. 내용뿐만 아니라 전체 폴더를 압축합니다. ZIP에는 하위 폴더와 함께 `Megabonk_Data` 폴더가 포함되어야 합니다. [Microsoft: 파일 압축 및 압축 풀기](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. ZIP이 **1GiB**보다 크지 않은지 확인하세요. Android 기기를 연결하고 잠금을 해제하고 USB 알림에서 **파일 전송**을 선택한 다음 Steam ZIP과 베타의 `OpenBonk.apk`를 모두 다운로드에 복사하세요. Steam ZIP을 비공개로 유지하세요. 게임 복사본의 파일이 포함되어 있습니다. [안드로이드: USB로 파일 전송](https://support.google.com/android/answer/9064445?hl=en).

전체 Linux 설치 ZIP은 Retroid 및 iPlay에서 테스트되었습니다. 39개의 지원되는 Windows 파일이 포함된 더 작은 ZIP도 iPlay로 가져와 Forest에 도달했습니다. 실제 Windows PC에서 ZIP을 준비하고 전체 Windows 설치 폴더를 가져오는 것은 아직 검증되지 않았습니다.

전체 ZIP이 1GiB를 초과하는 경우 패키지의 `tools` 폴더에는 39개의 필수 파일을 확인하고 더 작은 ZIP을 생성하는 도우미가 포함되어 있습니다. Windows에서는 `tools`가 포함된 폴더에서 PowerShell을 열고 이 명령을 실행하여 첫 번째 경로를 게임 폴더로 바꿉니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Linux의 경우 동일한 폴더에서 Python 3.9 이상을 사용하는 경우:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Windows에서 Python 3.9 이상이 설치된 경우 PowerShell에서 Python 도우미를 실행할 수도 있습니다.

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

도우미의 ZIP을 Android 다운로드에 복사하고 전체 ZIP 대신 선택하세요. 도우미는 기존 ZIP을 덮어쓰지 않으며 인식할 수 없는 파일 버전을 거부합니다. Python 도우미는 Wine에서 Windows Python을 사용하여 실행되었으며 해당 ZIP은 iPlay로 가져왔습니다. PowerShell 도우미는 Linux에서 PowerShell 7을 사용하여 테스트되었습니다. 두 도우미 모두 기본 Windows PC에서 테스트되지 않았습니다.

## 설치 및 플레이

1. Android 파일 관리자에서 `OpenBonk.apk`를 엽니다. 파일 세부정보가 표시되면 **다음으로 열기 → 패키지 설치 프로그램**을 찾아보세요. Android에서 해당 앱 설치 권한을 요청하는 경우 메시지를 읽고 지침을 따르세요. 나중에 해당 권한을 끌 수 있습니다.
2. OpenBonk를 엽니다. 런처에서 **Controller** 또는 **Touch**를 선택합니다.
3. **Import Steam game**를 선택하고 다운로드에서 ZIP을 찾아 선택하세요. **Import prepared Android data**는 Steam ZIP이 아닌 이전에 준비된 Android 번들용입니다. 앱을 열어두고 **Data ready**를 기다립니다. 메시지는 단계마다 변경되지만 백분율은 표시되지 않습니다.
4. **Play**를 선택합니다. 처음 실행하면 게임 메뉴가 나타나기 전에 텍스트와 텍스처를 가져옵니다.

## 가져오기 및 백업 진행 상황

Steam 프로필 가져오기는 선택 사항입니다. 먼저 컴퓨터에서 게임을 닫고 백업을 보관하세요. Linux에서는 `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/`에서 계정 폴더를 찾거나 Windows에서는 `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\`를 사용해 보세요(Windows 경로는 확인되지 않았습니다). 동일한 계정에서 ZIP `progression.json` 및 `stats.json`를 함께 사용하려면 ZIP을 Android에 복사하고 **Import profile ZIP**를 선택하세요. 게임을 시작하면 현재 Android 계정의 진행 상황이 대체되므로 원본 백업을 보관하세요. 이는 PC에서 Android로의 동기화가 아닌 일회성 가져오기입니다. 프로필 ZIP은 비공개입니다. 보고서에 첨부하지 마세요.

Android 진행 상황을 백업하려면 게임을 완전히 닫고 런처를 열고 **Export progress**를 선택하세요. 프로필을 선택하고 ZIP 파일을 비공개로 저장하세요. **Import profile ZIP**를 사용하여 복원할 수 있습니다. ZIP에는 진행 상황과 통계가 포함되어 있지만 컨트롤이나 모드는 포함되어 있지 않습니다. 내보내기는 테스트 프로필을 사용하여 iPlay에서 테스트되었습니다. 원본 Steam 백업도 유지하세요.

**Android 백업:** 이 버전은 자동 백업을 비활성화하지 않습니다. 기기 설정에 따라 Android는 계정 백업 및 기기 전송에 저장 또는 보류 중인 프로필 가져오기와 같은 앱 데이터를 포함할 수 있습니다. APK는 인터넷 권한을 요청하지 않지만 Android는 자체 백업을 관리할 수 있습니다. 이를 원하지 않으면 Android의 백업 설정을 검토하세요. 앱을 제거하면 로컬 데이터가 삭제될 수 있습니다. 먼저 진행 상황을 백업하세요. [안드로이드 자동 백업](https://developer.android.com/identity/data/autobackup).

## 문제 해결

- **호환되지 않는 ZIP:** 전체 설치 및 지원되는 게임 버전에서 `Megabonk_Data`를 선택하세요. Steam 업데이트는 런처에서 확인한 파일을 변경할 수 있습니다.
- **공간이 부족합니다.** 여유 공간을 확보하고 다시 시도하세요. 런처는 아직 전체 공간 요구 사항을 미리 계산하지 않습니다.
- **실행 시 텍스트 가져오기 실패:** 보류 중인 ZIP이 캐시된 상태에서 게임을 다시 열어 재시도하세요. Android에서 캐시를 지운 경우 ZIP을 다시 선택하세요.
- **APK는 다른 버전 위에 설치되지 않습니다.** 서명이나 애플리케이션 ID가 다를 수 있습니다. 기존 앱을 제거하기 전에 유지하려는 진행 상황을 백업하세요.
- **Play Protect는 APK 검사를 요청합니다.** 하나의 iPlay에서 Android는 설치 전에 분석을 위해 파일을 Google에 보낼 것을 제안했습니다. 프롬프트를 읽고 보낼지 여부를 결정하십시오. 해당 스캔 결과와 시스템 설치 프로그램을 통한 전체 설치는 아직 확인되지 않았습니다.

## 문제 신고

기기 모델, Android 버전, 컨트롤러 또는 터치 사용 여부, 수행한 단계, 예상한 결과 및 발생한 상황을 포함하세요. 유용한 경우 오류 스크린샷을 첨부하세요. 일반 보고서에는 Steam ZIP, 프로필 또는 계정 정보를 포함하지 마세요.
