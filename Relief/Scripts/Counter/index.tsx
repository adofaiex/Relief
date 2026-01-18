import * as UnityEngine from 'unity-engine'
import * as UI from 'unity-engine/ui'

export default function Loader(id: string, name: string) {
  const canvas = <canvas
    components={[UI.CanvasScaler, UI.GraphicRaycaster]}
    referenceResolution={{ x: 1920, y: 1080 }}
    dontDestoryOnLoad={true}
  >
    <image
      backgroundGradient={{
        width: 6,
        height: 1,
        radius: 0.02,
        start: { r: 1, g: 0.6, b: 0.8, a: 1 },
        end: { r: 0.8, g: 0.4, b: 0.6, a: 1 },
        vertical: true
      }}
      anchoredPosition={{ x: 0, y: 400 }}
    >
      <textMeshPro 
        text="SRP 风格按钮示例" 
        fontSize={36} 
        color={{ r: 1, g: 1, b: 1, a: 1 }}
        alignment="Center"
      />
    </image>
  </canvas>;

  // 使用扩展函数添加组件
  canvas.AddComponentJ(UI.CanvasScaler);
  
  console.log(`Module ${name} (ID: ${id}) initialized with direct JSX-to-GameObject mapping.`);
  console.log(canvas);
}