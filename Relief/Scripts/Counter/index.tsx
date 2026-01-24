/// <reference path="../../Typings/unity-engine.d.ts" />
/// <reference path="../../Typings/react.d.ts" />
/// <reference path="../../Typings/react-jsx-runtime.d.ts" />
/// <reference path="../../Typings/react-unity.d.ts" />
/// <reference path="../../Typings/unity-components.d.ts" />
/// <reference path="../../Typings/builtin.d.ts" />

import { MonoBehaviour,Resources } from 'unity-engine'
import { LoaderA } from './umm.tsx'
import * as UI from 'unity-engine/ui'
import * as fs from 'fs'
import * as path from 'path'
import { loadFont, loadTMPFont,getOSFont } from 'resource-manager'
import { instance as uitext } from 'uitext'
import { createRoot } from 'react-unity'
import { Canvas, Image, TextMeshPro } from 'react/unityComponents'

export default function Loader(id: string, name: string): void {
  const baseDir = path.join(path.resolve(`./Mods/Relief/Scripts`),`${id}`);
  
  // 1. 统计启动次数
  const statsPath = path.join(baseDir, "stats.json");
  if (!fs.existsSync(baseDir)) {
    fs.mkdirSync(baseDir, { recursive: true });
  }

  const font = getOSFont("Microsoft YaHei UI Light");
  console.log("Loading font from OS ","Result:", font);

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


  // 3. 使用 ReactUnity 渲染 UI
  // 首先创建一个基础 Canvas
  const canvas = <Canvas
    name="ReliefCounterCanvas"
    referenceResolution={{ x: 1920, y: 1080 }}
    dontDestroyOnLoad={true}
  />;
  
  // 设置为激活状态
  canvas.SetActive(true);

  // 使用 createRoot 挂载 UI
  const root = createRoot(canvas);
  
  root.render(
    <TextMeshPro
        text={`这是你第 <color=#FFD700>${stats.startCount}</color> 次打开游戏\n<size=24>上次启动: ${new Date(stats.lastStart).toLocaleString()}</size>`}
        fontSize={36}
        font={font}
        color={{ r: 1, g: 1, b: 1, a: 1 }}
        alignment="Center"
      />
  );

  // 5秒后销毁整个 UI
  setTimeout(() => {
    root.unmount();
    canvas.Destroy();
    console.log(`UI for ${name} unmounted and destroyed.`);
  }, 5000);

  console.log(`Module ${name} (ID: ${id}) initialized. Total starts: ${stats.startCount}`);
}