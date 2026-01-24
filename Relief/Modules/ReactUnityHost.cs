using UnityEngine;
using Jint.Native;
using Jint;

namespace Relief.Modules
{
    public class ReactUnityHost : MonoBehaviour
    {
        private ReactUnity.Root _root;
        private JsValue _element;

        public void Initialize(ReactUnity.Root root, JsValue element)
        {
            _root = root;
            _element = element;
        }

        private void Update()
        {
            if (_root != null && !_element.IsUndefined())
            {
                _root.render(_element);
            }
        }
    }
}