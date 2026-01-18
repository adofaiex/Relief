/// <reference path="../../Typings/unity-engine.d.ts" />
/// <reference path="../../Typings/react.d.ts" />
/// <reference path="../../Typings/react-jsx-runtime.d.ts" />
/// <reference path="../../Typings/builtin.d.ts" />

import { MonoBehaviour } from 'unity-engine'
import { LoaderA } from './umm.tsx'
import * as UI from 'unity-engine/ui'
import * as fs from 'fs'
import * as path from 'path'

export default function Loader(id: string, name: string): void {
  // 1. 统计启动次数
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

  // 2. 显示 UI
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

  canvas.AddComponentJ(new LoaderA());
  
  setTimeout(() => {
    canvas.Destroy();
    console.log(`Canvas for ${name} destroyed after 2 seconds.`);
  }, 2000);

  console.log(`Module ${name} (ID: ${id}) initialized. Total starts: ${stats.startCount}`);
}