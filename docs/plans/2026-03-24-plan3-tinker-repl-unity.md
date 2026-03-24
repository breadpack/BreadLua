# BreadLua Plan 3: LuaTinker + REPL + Unity Integration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development

**Goal:** LuaTinker(간편 바인딩 + 호출), REPL, 핫 리로드, Unity 패키지를 구현하여 풀 패키지를 완성한다.

**Architecture:** LuaTinker는 LuaState 위에 편의 API를 추가한다. Bind()는 델리게이트를 함수 포인터로 변환하여 등록하고, Call<T>()는 pcall 후 결과를 가져온다. REPL은 콘솔 입력을 DoString으로 실행한다.

**Tech Stack:** .NET 9, TUnit

---

## Task 1: LuaState 확장 — Call<T>, Eval<T>, SetGlobal 오버로드

LuaState에 타입 반환 함수 호출과 다양한 타입 SetGlobal 추가.

**Files:**
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs`
- Test: `tests/BreadLua.Tests/Core/LuaStateExtendedTests.cs`

기능:
- `T Call<T>(string funcName, params object[] args)` — Lua 함수 호출 + 결과 반환
- `T Eval<T>(string expression)` — Lua 표현식 평가 + 결과 반환
- `SetGlobal(string, double)`, `SetGlobal(string, bool)`, `SetGlobal(string, string)` 오버로드

---

## Task 2: LuaTinker — Bind() 런타임 바인딩

LuaState에 Bind() 메서드 추가. 런타임에 C# 델리게이트/람다를 Lua 함수로 등록.

**Files:**
- Create: `src/BreadLua.Runtime/Core/LuaTinker.cs`
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs` (Tinker 프로퍼티)
- Test: `tests/BreadLua.Tests/Core/TinkerTests.cs`

기능:
- `lua.Bind("add", (int a, int b) => a + b)`
- `lua.Bind("greet", (string name) => "Hello " + name)`
- `lua.Bind("log", (string msg) => Console.WriteLine(msg))`
- 내부: 델리게이트 → C 콜백 래퍼 등록 (breadlua_core.c의 lua_pushcfunction 활용)

---

## Task 3: Hot Reload

파일 변경 시 Lua 스크립트를 다시 로드하는 기능.

**Files:**
- Create: `src/BreadLua.Runtime/Core/HotReload.cs`
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs`
- Test: `tests/BreadLua.Tests/Core/HotReloadTests.cs`

기능:
- `lua.Reload(string path)` — 파일 다시 실행
- `lua.WatchAndReload(string directory)` — FileSystemWatcher 기반 자동 리로드 (optional)

---

## Task 4: REPL

콘솔에서 Lua 코드를 대화형으로 실행.

**Files:**
- Create: `src/BreadLua.Runtime/Core/Repl.cs`
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs`

기능:
- `lua.StartRepl()` — 콘솔 루프: 입력 → DoString → 결과 출력
- 멀티라인 지원 (불완전한 문장 감지)
- `exit` 또는 `quit`으로 종료

---

## Task 5: Unity 패키지 스캐폴딩

**Files:**
- Create: `src/BreadLua.Unity/package.json`
- Create: `src/BreadLua.Unity/Runtime/UnityModuleLoader.cs`
- Create: `src/BreadLua.Unity/Runtime/UnityLuaState.cs`
- Create: `src/BreadLua.Unity/Runtime/BreadPack.NativeLua.Unity.asmdef`

기능:
- Unity Package Manager 호환 구조
- UnityModuleLoader — Resources 폴더에서 .lua.txt 로드
- UnityLuaState — MonoBehaviour 라이프사이클 통합

---

## Task 6: 최종 통합 테스트 + README 업데이트

**Files:**
- Create: `tests/BreadLua.Tests/Integration/FullIntegrationTests.cs`
- Modify: `README.md`
- Modify: `samples/BreadLua.Sample/Program.cs`

전체 API를 사용하는 엔드투엔드 테스트 + 샘플 업데이트 + README 완성.
