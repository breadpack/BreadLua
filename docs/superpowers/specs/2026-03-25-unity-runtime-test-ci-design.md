# Unity Runtime Test + CI Pipeline Design

## Overview

BreadLua Unity 패키지의 전체 기능이 모바일 플랫폼(Android/iOS)에서 정상 동작하는지 검증하기 위한 Unity Play Mode 테스트 및 CI 파이프라인 설계.

## Goals

1. Unity Play Mode 테스트로 BreadLua 전체 기능 검증 (D 레벨)
2. GameCI를 통한 Editor Play Mode 테스트 (Windows/macOS/Linux)
3. Firebase Test Lab Game Loop을 통한 실기기 테스트 (Android/iOS)
4. IL2CPP 엣지 케이스 전용 테스트 포함
5. 기존 `ci.yml.disabled` 워크플로우를 활성화하여 통합

## Non-Goals

- UI/그래픽 렌더링 테스트
- 성능 벤치마크 (별도 파이프라인)
- WebGL 플랫폼 테스트

---

## 0. Implementation Prerequisites

테스트가 IL2CPP에서 정상 동작하려면 BreadLua 런타임에 다음 변경이 선행되어야 한다:

### 0.1 `[MonoPInvokeCallback]` 추가

`LuaTinker.OnGenericCallback`에 역방향 P/Invoke 콜백 속성 추가. 없으면 IL2CPP에서 크래시.

```csharp
// LuaTinker.cs
[AOT.MonoPInvokeCallback(typeof(GenericCallbackDelegate))]
private static int OnGenericCallback(IntPtr L, IntPtr namePtr) { ... }
```

### 0.2 `Marshal.PtrToStringAnsi` → UTF-8 마샬링 전환 검토

`PtrToStringAnsi`는 Windows에서는 시스템 코드페이지, 비-Windows에서는 시스템 로케일 인코딩을 사용한다. Lua 문자열은 UTF-8이므로 한글/이모지 등 비-ASCII 문자가 손실될 수 있다. 이는 IL2CPP 특유 문제가 아닌 **크로스 플랫폼 버그**이다.

영향 범위:
- `LuaTinker.OnGenericCallback` (line 72, 99, 108)
- `LuaState.ReadValue<T>` (line 145)
- `LuaState.GetTopString` (line 171)

수정 방안: `Marshal.PtrToStringUTF8` (.NET Standard 2.1+) 사용 또는 수동 UTF-8 디코딩.

### 0.3 `link.xml` 추가

Source Generator 생성 코드가 IL2CPP Managed Stripping에 의해 제거되지 않도록 보호.

위치: `src/BreadLua.Unity/Runtime/link.xml`

```xml
<linker>
  <assembly fullname="BreadPack.NativeLua.Unity">
    <type fullname="*" preserve="all"/>
  </assembly>
</linker>
```

---

## 1. Test Project Structure

### 1.1 테스트 코드 위치 — UPM 패키지 내부

```
src/BreadLua.Unity/Tests/
├── BreadPack.NativeLua.Unity.Tests.asmdef
├── LuaStateTests.cs
├── BufferTests.cs
├── TinkerTests.cs
├── HotReloadTests.cs
├── GeneratorTests.cs
├── UnityIntegrationTests.cs
└── IL2CPPEdgeCaseTests.cs
```

**asmdef 설정:**
- `includePlatforms`: [] (모든 플랫폼)
- `defineConstraints`: ["UNITY_INCLUDE_TESTS"]
- `references`: ["BreadPack.NativeLua.Unity"]
- `optionalUnityReferences`: ["TestAssemblies"]

### 1.2 테스트 프로젝트 — CI 빌드/실행용

```
tests/BreadLua.Unity.TestProject/
├── Assets/
│   └── (비어있음 — 패키지 테스트는 Packages에서 참조)
├── Packages/
│   └── manifest.json        (로컬 file: 경로로 BreadLua.Unity 참조)
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   └── EditorBuildSettings.asset
└── FirebaseGameLoop/
    ├── GameLoopManager.cs   (Firebase Game Loop 진입점)
    └── TestResultWriter.cs  (JSON + Logcat 결과 출력)
```

**manifest.json:**
```json
{
  "dependencies": {
    "dev.breadpack.nativelua": "file:../../src/BreadLua.Unity"
  }
}
```

---

## 2. Test Coverage

### 2.1 Core API Tests — `LuaStateTests.cs`

| Test | Description |
|------|------------|
| `CreateAndDispose` | LuaState 생성 및 정상 해제 |
| `DoString_SimpleExpression` | 기본 Lua 코드 실행 |
| `DoString_SyntaxError_Throws` | 문법 오류 시 LuaException |
| `Call_GlobalFunction` | Lua 전역 함수 호출 |
| `Eval_Int` | 정수 반환 |
| `Eval_Double` | 실수 반환 |
| `Eval_Bool` | 불리언 반환 |
| `Eval_String` | 문자열 반환 |
| `SetGlobal_AllTypes` | long, double, bool, string, IntPtr 글로벌 설정 |
| `CallWithArgs_MixedTypes` | `Call<T>(name, params)` 다양한 인자 조합 |
| `DisposedState_Throws` | Dispose 후 사용 시 ObjectDisposedException |

### 2.2 Buffer Tests — `BufferTests.cs`

| Test | Description |
|------|------------|
| `Create_And_Access` | Buffer<int> 생성 및 인덱서 접근 |
| `BindToLua_And_ReadFromLua` | Lua에서 공유 메모리 읽기 |
| `AsSpan_ReturnsCorrectSlice` | Span<T> 변환 검증 |
| `Dispose_FreesMemory` | 메모리 해제 확인 |
| `OutOfRange_Throws` | 범위 초과 시 IndexOutOfRangeException |

### 2.3 Tinker Tests — `TinkerTests.cs`

| Test | Description |
|------|------------|
| `Bind_IntFunc_And_Call` | `Func<int,int,int>` 바인딩 및 Lua에서 호출 |
| `Bind_FloatFunc` | `Func<float,float,float>` 바인딩 |
| `Bind_StringFunc` | `Func<string,string>` 바인딩 |
| `Bind_StringAction` | `Action<string>` 바인딩 |
| `Bind_Action` | `Action` 바인딩 |
| `Bind_DoubleFunc` | `Func<double>` 바인딩 |
| `Callback_Exception_Propagates` | C# 예외가 Lua로 전파 |
| `MultiInstance_BindingIsolation` | 두 LuaState에서 동일 이름 바인딩 시 격리 확인 (static `_bindings` 공유 문제 검출) |

### 2.4 HotReload Tests — `HotReloadTests.cs`

> **Note:** `HotReload`은 `FileSystemWatcher`를 사용하므로 모바일 빌드에서는 제외한다.
> `#if !UNITY_ANDROID && !UNITY_IOS` 가드 적용.

| Test | Description |
|------|------------|
| `Reload_UpdatesGlobalState` | 스크립트 리로드 후 전역 변수 변경 확인 |

### 2.5 Generator Tests — `GeneratorTests.cs`

| Test | Description |
|------|------------|
| `LuaBind_ClassRegistration` | [LuaBind] 클래스가 Lua에서 접근 가능 |
| `LuaBind_MethodCall` | 바인딩된 메서드 호출 |
| `LuaBind_PropertyAccess` | 프로퍼티 getter/setter |
| `LuaModule_FunctionCall` | [LuaModule] 정적 함수 호출 |
| `LuaBridge_StructBinding` | [LuaBridge] 구조체 공유 메모리 바인딩 |

### 2.6 Unity Integration Tests — `UnityIntegrationTests.cs`

| Test | Description |
|------|------------|
| `UnityLuaState_Lifecycle` | Awake에서 초기화, OnDestroy에서 해제 |
| `UnityLuaState_UpdateCallback` | Update에서 on_update() 호출 |
| `UnityLuaState_StartupScripts` | startupScripts 배열 실행 |
| `UnityModuleLoader_LoadFromResources` | Resources 폴더에서 Lua 스크립트 로드 |

### 2.7 IL2CPP Edge Case Tests — `IL2CPPEdgeCaseTests.cs`

| # | Test | 검증 대상 |
|---|------|----------|
| 1 | `ReversePInvokeCallback_SurvivesIL2CPP` | `Marshal.GetFunctionPointerForDelegate`로 생성한 콜백이 네이티브에서 호출될 때 크래시하지 않음. `[MonoPInvokeCallback]` 필요 여부 검증 |
| 2 | `GenericReadValue_AllTypes` | `Eval<int>`, `Eval<long>`, `Eval<float>`, `Eval<double>`, `Eval<bool>`, `Eval<string>` — 모든 타입 인스턴스의 박싱/언박싱이 IL2CPP AOT에서 정상 동작 |
| 3 | `GenericCallWithParams_BoxingRoundTrip` | `Call<int>("func", 1, 2.5, "str", true)` — params object[] 박싱 + 제네릭 반환 조합 |
| 4 | `BufferGenericPointer_MultipleTypes` | `Buffer<int>`, `Buffer<float>`, `Buffer<double>` — 제네릭 unsafe `T*` 포인터와 `sizeof(T)` |
| 5 | `GCHandleRoundTrip_ObjectSurvivesNativePass` | `ObjectHandle.Alloc` → 네이티브 전달 → `ObjectHandle.Get<T>` — GCHandle이 IL2CPP GC에서 살아남음 |
| 6 | `DelegateTypePatternMatching_AllBindOverloads` | `del is Func<int,int,int>` 등 6가지 델리게이트 타입 패턴 매칭이 IL2CPP에서 정상 동작 |
| 7 | `SourceGeneratedCode_NotStripped` | [LuaBind], [LuaModule], [LuaBridge] 생성 코드가 IL2CPP stripping에 살아남음. `link.xml` 필요 여부 검증 |
| 8 | `UnicodeStringMarshalling_KoreanAndEmoji` | `SetGlobal("name", "한글테스트🎮")` → `Eval<string>` 왕복 — `Marshal.PtrToStringAnsi`의 플랫폼별 인코딩 차이 |

---

## 3. Firebase Game Loop Integration

### 3.1 Android

**AndroidManifest.xml 추가:**
```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity">
    <intent-filter>
        <action android:name="com.google.intent.action.TEST_LOOP" />
        <category android:name="android.intent.category.DEFAULT" />
    </intent-filter>
</activity>
```

### 3.2 iOS

**Info.plist 추가:**
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>firebase-game-loop</string>
        </array>
    </dict>
</array>
```

### 3.3 GameLoopManager.cs

진입점 역할:
- 앱 시작 시 Game Loop intent/URL scheme 감지
- 감지되면 전체 테스트 스위트 실행
- 결과를 `TestResultWriter`로 기록
- 완료 시 Game Loop 종료 시그널 전송

### 3.4 TestResultWriter.cs

두 가지 결과 수집:

1. **Logcat/stdout 출력:**
   ```
   [BREADLUA_TEST] START: ReversePInvokeCallback_SurvivesIL2CPP
   [BREADLUA_TEST] PASS: ReversePInvokeCallback_SurvivesIL2CPP
   [BREADLUA_TEST] FAIL: UnicodeStringMarshalling — Expected "한글" but got "??"
   [BREADLUA_TEST] SUMMARY: 25/26 passed, 1 failed
   ```

2. **JSON 파일 저장:**
   - Android: `/sdcard/Android/data/<package>/files/test-results.json`
   - iOS: `Documents/test-results.json`
   - Firebase가 자동 수집

```json
{
  "timestamp": "2026-03-25T10:00:00Z",
  "platform": "Android",
  "backend": "IL2CPP",
  "total": 26,
  "passed": 25,
  "failed": 1,
  "results": [
    { "name": "ReversePInvokeCallback_SurvivesIL2CPP", "status": "pass", "duration_ms": 12 },
    { "name": "UnicodeStringMarshalling", "status": "fail", "message": "Expected '한글' but got '??'" }
  ]
}
```

---

## 4. CI Pipeline

### 4.1 Workflow Overview

기존 `ci.yml.disabled` → `ci.yml`로 활성화하고 Unity 단계 추가.

```
┌─────────────────────────────────────────────────────┐
│  Stage 1: Native Build (parallel)                   │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌─────────┐ ┌───┐     │
│  │ Win  │ │ Linux│ │ macOS│ │ Android │ │iOS│     │
│  └──────┘ └──────┘ └──────┘ └─────────┘ └───┘     │
└──────────────────────┬──────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────┐
│  Stage 2: .NET Build & TUnit Tests (matrix)         │
│  Windows / Linux / macOS                            │
└──────────────────────┬──────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────┐
│  Stage 3: Unity Play Mode Tests (GameCI)            │
│  Editor Play Mode on Ubuntu / macOS / Windows       │
└──────────────────────┬──────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────┐
│  Stage 4: Unity Build (GameCI Builder)              │
│  Android APK (IL2CPP) / iOS IPA (IL2CPP)            │
└──────────────────────┬──────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────┐
│  Stage 5: Firebase Test Lab                         │
│  Android Game Loop / iOS Game Loop (beta)           │
└─────────────────────────────────────────────────────┘
```

### 4.2 GitHub Secrets Required

| Secret | Description |
|--------|------------|
| `UNITY_LICENSE` | Unity 라이선스 (GameCI 활성화용) |
| `UNITY_EMAIL` | Unity 계정 이메일 |
| `UNITY_PASSWORD` | Unity 계정 비밀번호 |
| `GCP_SA_KEY` | GCP 서비스 계정 JSON 키 |
| `GCP_PROJECT_ID` | Firebase/GCP 프로젝트 ID |

### 4.3 Stage 3: Unity Play Mode Tests (GameCI)

```yaml
unity-test:
  needs: [build-windows, build-linux, build-macos, dotnet-test]  # 모든 네이티브 빌드 + .NET 테스트 완료 후
  strategy:
    matrix:
      os: [ubuntu-latest, macos-latest, windows-latest]
  runs-on: ${{ matrix.os }}
  steps:
    - uses: actions/checkout@v4
    - uses: actions/download-artifact@v4  # 해당 OS의 네이티브 바이너리
    - uses: game-ci/unity-test-runner@v4
      with:
        testMode: playmode
        projectPath: tests/BreadLua.Unity.TestProject
        unityVersion: auto  # ProjectSettings에서 자동 감지
    - uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ matrix.os }}
        path: artifacts/
```

### 4.4 Stage 4: Unity Build

```yaml
unity-build:
  needs: [unity-test, build-android, build-ios]  # 네이티브 모바일 빌드 포함
  strategy:
    matrix:
      targetPlatform: [Android, iOS]
  runs-on: ${{ matrix.targetPlatform == 'iOS' && 'macos-latest' || 'ubuntu-latest' }}
  steps:
    - uses: actions/checkout@v4
    - uses: actions/download-artifact@v4
    - uses: game-ci/unity-builder@v4
      with:
        targetPlatform: ${{ matrix.targetPlatform }}
        projectPath: tests/BreadLua.Unity.TestProject
        customParameters: -scripting-backend IL2CPP
    # iOS: GameCI builder는 Xcode 프로젝트 생성 → archive/export 필요
    - name: Archive and export iOS (iOS only)
      if: matrix.targetPlatform == 'iOS'
      run: |
        xcodebuild archive \
          -project build/iOS/Unity-iPhone.xcodeproj \
          -scheme Unity-iPhone \
          -archivePath build/iOS/app.xcarchive \
          -allowProvisioningUpdates \
          CODE_SIGN_IDENTITY="-" \
          AD_HOC_CODE_SIGNING_ALLOWED=YES
        xcodebuild -exportArchive \
          -archivePath build/iOS/app.xcarchive \
          -exportPath build/iOS/export \
          -exportOptionsPlist ExportOptions.plist
    - uses: actions/upload-artifact@v4
      with:
        name: build-${{ matrix.targetPlatform }}
        path: build/
```

### 4.5 Stage 5: Firebase Test Lab

> **Trigger 조건:** Firebase 단계는 `push to main` 시에만 실행 (무료 티어 5회/일 보호).
> PR에서는 Stage 4 (빌드 성공)까지만 검증.

```yaml
firebase-test:
  needs: [unity-build]
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  strategy:
    matrix:
      include:
        - platform: android
          artifact: build-Android
          device: model=Pixel6,version=33
        - platform: ios
          artifact: build-iOS
          device: model=iphone13pro,version=16.6
  steps:
    - uses: google-github-actions/auth@v2
      with:
        credentials_json: ${{ secrets.GCP_SA_KEY }}
    - uses: google-github-actions/setup-gcloud@v2
    - uses: actions/download-artifact@v4
      with:
        name: ${{ matrix.artifact }}
    - name: Run Firebase Test Lab (Android)
      if: matrix.platform == 'android'
      run: |
        gcloud firebase test android run \
          --type game-loop \
          --app build/*.apk \
          --device ${{ matrix.device }} \
          --timeout 5m \
          --results-dir=results \
          --results-bucket=${{ secrets.GCP_PROJECT_ID }}-test-results \
          --project ${{ secrets.GCP_PROJECT_ID }}
    - name: Run Firebase Test Lab (iOS)
      if: matrix.platform == 'ios'
      run: |
        gcloud beta firebase test ios run \
          --type game-loop \
          --app build/iOS/export/*.ipa \
          --device ${{ matrix.device }} \
          --timeout 5m \
          --project ${{ secrets.GCP_PROJECT_ID }}
    - name: Check results
      if: matrix.platform == 'android'
      run: |
        # Firebase 테스트 결과 다운로드 및 파싱
        RESULT=$(gcloud firebase test android results describe \
          --project ${{ secrets.GCP_PROJECT_ID }} \
          --format="value(testMatrixId)" 2>/dev/null | tail -1)
        gsutil cp "gs://${{ secrets.GCP_PROJECT_ID }}-test-results/$RESULT/*/logcat" ./logcat.txt || true
        if grep -q "\[BREADLUA_TEST\] FAIL:" ./logcat.txt 2>/dev/null; then
          echo "::error::Firebase Test Lab tests failed"
          grep "\[BREADLUA_TEST\]" ./logcat.txt
          exit 1
        fi
        grep "\[BREADLUA_TEST\] SUMMARY:" ./logcat.txt || echo "No test summary found"
```

---

## 5. Key Design Decisions

### 5.1 테스트 공유 구조

Play Mode 테스트와 Firebase Game Loop이 **동일한 테스트 로직을 공유**:
- 테스트 메서드는 `Tests/` asmdef에 NUnit 어트리뷰트로 작성
- `GameLoopManager`는 리플렉션 또는 직접 호출로 동일 테스트 실행
- Editor에서는 Test Runner UI로, 실기기에서는 Game Loop으로 실행

### 5.2 Implementation Prerequisites 참조

`link.xml` 추가, `[MonoPInvokeCallback]` 추가, UTF-8 마샬링 전환은 **Section 0. Implementation Prerequisites** 참조. 이 항목들은 테스트 작성 전에 선행되어야 한다.

---

## 6. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| GameCI Unity 라이선스 활성화 실패 | CI 전체 중단 | Unity Personal 라이선스 사용, 시크릿 검증 단계 추가 |
| Firebase iOS Game Loop Beta 불안정 | iOS 테스트 실패 | iOS는 빌드 성공만으로 1차 검증, Game Loop은 optional |
| IL2CPP 스트리핑으로 테스트 코드 제거 | 테스트 미실행 | link.xml로 보호, 빌드 로그에서 스트리핑 경고 확인 |
| 무료 티어 5회/일 초과 | Firebase 테스트 스킵 | PR 머지 시에만 Firebase 실행 (push to main) |
| `Marshal.PtrToStringAnsi` 유니코드 손실 | 문자열 테스트 실패 | UTF-8 마샬링으로 전환 검토 (네이티브 측 수정 필요) |
