using System;

namespace Zest {

    class DummyFile {

        public int Prop { get; private set; }

        //public int NoSetter { get { return 5; } }

        public int MyFunc(object a, byte b, char c, DummyFile d)
        {
            return 42;
        }

        public void foobar() {
            var d = DateTime.Now;
        }

        public DummyFile self() { return this; }

    }

}
