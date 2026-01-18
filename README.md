# ScriptExecuter

> A Javascript/TypeScript for Games with UnityModManager (like ADOFAI)

## Features

* Support Javascript/TypeScript

We use tsc(typescript compiler) to compile ts to js file 
and use Jint to run js file in Unity.

It's safe, easy and fast.

* Internal Modules

```typescript
/// <reference path="../../Typings/unity-engine.d.ts" />
/// <reference path="../../Typings/react.d.ts" />
/// <reference path="../../Typings/react-jsx-runtime.d.ts" />
/// <reference path="../../Typings/builtin.d.ts" />

/* Type declarations will auto generate after the mod first start */

import { MonoBehaviour } from 'unity-engine'
import * as UI from 'unity-engine/ui'
import * as fs from 'fs'
import * as path from 'path'

export default function Loader(id: string, name: string): void {
  // 1. Get start stats
  const statsPath = path.join(`./${name}`, "stats.json");
  if (!fs.existsSync(`./${name}`)) {
    fs.mkdirSync(`./${name}`, { recursive: true });
  }

  let stats = {
    startCount: 0,
    firstStart: new Date().toISOString(),
    lastStart: new Date().toISOString()
  };

  try {
    if (fs.existsSync(statsPath)) {
      const existingData = fs.readFileSync(statsPath);
      stats = JSON.parse(existingData);
    }
    stats.startCount += 1;
    stats.lastStart = new Date().toISOString();
    fs.writeFileSync(statsPath, JSON.stringify(stats, null, 2), 'utf8');
  } catch (err) {
    console.error("Failed to update start stats:", err);
  }

  // 2. Display UI
  const canvas = <canvas
    components={[UI.CanvasScaler, UI.GraphicRaycaster]}
    referenceResolution={{ x: 1920, y: 1080 }}
    dontDestoryOnLoad={true}
  >
    <image
      backgroundGradient={{
        width: 10,
        height: 3,
        radius: 0.02,
        start: { r: 0.2, g: 0.2, b: 0.2, a: 0.8 },
        end: { r: 0.1, g: 0.1, b: 0.1, a: 0.9 },
        vertical: true
      }}
      anchoredPosition={{ x: 0, y: 400 }}
      sizeDelta={{ x: 600, y: 200 }}
    >
      <textMeshPro
        text={`这是你第 <color=#FFD700>${stats.startCount}</color> 次打开游戏\n<size=24>上次启动: ${new Date(stats.lastStart).toLocaleString()}</size>`}
        fontSize={36}
        color={{ r: 1, g: 1, b: 1, a: 1 }}
        alignment="Center"
        anchoredPosition={{ x: 0, y: 0 }}
      />
    </image>
  </canvas>;

  setTimeout(() => {
    canvas.Destroy();
    console.log(`Canvas for ${name} destroyed after 2 seconds.`);
  }, 2000);

  console.log(`Module ${name} (ID: ${id}) initialized. Total starts: ${stats.startCount}`);
}
```
(An easy template for a mod loader)

* Support Javascript JSX
```typescript
const MessageBoxCanvas =  <canvas
    components={[UI.CanvasScaler, UI.GraphicRaycaster]}
    referenceResolution={{ x: 1920, y: 1080 }}
    dontDestoryOnLoad={true}
  >
  <textMeshPro
    text="这是一条消息"
    fontSize={36}
    color={{ r: 1, g: 1, b: 1, a: 1 }}
    alignment="Center"
    anchoredPosition={{ x: 0, y: 0 }}
  />
</canvas>;
```
JSX will be compiled to `React.createElement`,but `React.createElement` will generate `UnityEngine.GameObject` instead of `JSX.Element`

Of course,You can create GameObject directly:

```typescript
import {GameObject} from 'unity-engine'
const go = GameObject.FindWithTag('t1');
```

