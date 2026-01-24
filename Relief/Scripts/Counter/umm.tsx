/// <reference path="../../Typings/unity-engine.d.ts" />
/// <reference path="../../Typings/react.d.ts" />
/// <reference path="../../Typings/react-jsx-runtime.d.ts" />
/// <reference path="../../Typings/builtin.d.ts" />

import { MonoBehaviour } from 'unity-engine'
import * as UI from 'unity-engine/ui'


export class LoaderA extends MonoBehaviour {
    private timer: number = 0;

    Update(): void {
        this.timer += 0.016; // 粗略估算一帧的时间
        if (this.timer >= 1.0) {
            console.log("LoaderA is active and running...");
            this.timer = 0;
        }
    }

    OnDestroy(): void {
        console.log("LoaderA component destroyed.");
    }
}