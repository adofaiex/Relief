/// <reference path="../../Typings/unity-engine.d.ts" />
/// <reference path="../../Typings/react.d.ts" />
/// <reference path="../../Typings/react-jsx-runtime.d.ts" />
/// <reference path="../../Typings/builtin.d.ts" />

import { MonoBehaviour } from 'unity-engine'
import * as UI from 'unity-engine/ui'


export class LoaderA extends MonoBehaviour {
    Update(): void {
        console.log("Hi")
    }

}